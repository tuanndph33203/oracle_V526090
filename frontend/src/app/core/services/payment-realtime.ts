import { Injectable, computed, inject, signal } from '@angular/core';
import { Subject, firstValueFrom } from 'rxjs';
import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { IPaymentStatusEvent, ISalesRealtimeEvent, PaymentLifecycleStatus } from '../models/payment-realtime';
import { PaymentService } from './payment';
import { PaymentNotificationService } from './payment-notification';

@Injectable({ providedIn: 'root' })
export class PaymentRealtimeService {
    private readonly paymentService = inject(PaymentService);
    private readonly paymentNotificationService = inject(PaymentNotificationService);
    private connection: HubConnection | null = null;
    private connectionPromise: Promise<void> | null = null;
    private retryTimer: ReturnType<typeof setTimeout> | null = null;
    private retryCount = 0;
    private qrSyncPromise: Promise<void> | null = null;
    private qrSyncRequested = false;
    private readonly seenQrCallIds = new Set<string>();
    private readonly hubUrl = this.buildHubUrl();

    private activeBranchCodes: string[] = [];
    private readonly _notifications = signal<any[]>([]);
    private readonly _unreadCount = signal<number>(0);
    private readonly _connectionState = signal<HubConnectionState>(HubConnectionState.Disconnected);
    private readonly _branchReady = signal(false);
    private readonly _activeTransactionId = signal<string | null>(null);
    private readonly _statusByPaymentId = signal<Record<string, IPaymentStatusEvent>>({});
    private readonly _lastStatusEvent = signal<IPaymentStatusEvent | null>(null);
    private readonly _qrOrderNotification = signal<any | null>(null);
    private audioCtx: AudioContext | null = null;
    private readonly _audioReady = signal(false);
    /** Emits delta branch sales events for all screens to react to without polling */
    private readonly _salesEvent$ = new Subject<ISalesRealtimeEvent>();
    /** Public read-only observable for components to subscribe */
    readonly salesEvent$ = this._salesEvent$.asObservable();

    readonly notifications = this._notifications.asReadonly();
    readonly unreadCount = this._unreadCount.asReadonly();
    readonly connectionState = this._connectionState.asReadonly();
    readonly branchReady = this._branchReady.asReadonly();
    readonly activeTransactionId = this._activeTransactionId.asReadonly();
    readonly paymentStatusByPaymentId = this._statusByPaymentId.asReadonly();
    readonly lastStatusEvent = this._lastStatusEvent.asReadonly();
    readonly qrOrderNotification = this._qrOrderNotification.asReadonly();
    readonly audioReady = this._audioReady.asReadonly();

    constructor() {
        if (typeof window !== 'undefined') {
            try {
                const saved = JSON.parse(localStorage.getItem('qrStaffCallSeenIds') || '[]');
                if (Array.isArray(saved)) saved.forEach(id => this.seenQrCallIds.add(String(id)));
            } catch { /* Ignore invalid browser storage. */ }
            const unlockAudio = () => {
                void this.unlockAudio();
            };
            window.addEventListener('click', unlockAudio, { passive: true });
            window.addEventListener('touchstart', unlockAudio, { passive: true });
            window.addEventListener('online', () => void this.ensureConnection());
        }
    }

    async unlockAudio(): Promise<boolean> {
        try {
            const ctx = this.getAudioContext();
            if (!ctx) return false;
            if (ctx.state === 'suspended') await ctx.resume();
            this._audioReady.set(ctx.state === 'running');
            return this._audioReady();
        } catch {
            this._audioReady.set(false);
            return false;
        }
    }

    async testNotificationSound(): Promise<boolean> {
        if (!(await this.unlockAudio())) return false;
        this.playNotificationSound();
        return true;
    }

    private getAudioContext(): AudioContext | null {
        if (typeof window === 'undefined') return null;
        if (!this.audioCtx) {
            const AudioCtxClass = window.AudioContext || (window as any).webkitAudioContext;
            if (AudioCtxClass) {
                this.audioCtx = new AudioCtxClass();
                this.audioCtx.onstatechange = () => this._audioReady.set(this.audioCtx?.state === 'running');
            }
        }
        return this.audioCtx;
    }

    public playNotificationSound(): void {
        try {
            const ctx = this.getAudioContext();
            if (!ctx || ctx.state !== 'running') {
                this._audioReady.set(false);
                return;
            }

            const now = ctx.currentTime;
            const frequencies = [830, 1245, 1660, 2075];
            const gains = [0.15, 0.10, 0.06, 0.04];
            const decays = [1.5, 1.0, 0.7, 0.5];

            for (let i = 0; i < frequencies.length; i++) {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.value = frequencies[i];
                gain.gain.setValueAtTime(gains[i], now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + decays[i]);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(now);
                osc.stop(now + decays[i]);
            }

            setTimeout(() => {
                if (!ctx) return;
                const now2 = ctx.currentTime;
                for (let i = 0; i < frequencies.length; i++) {
                    const osc = ctx.createOscillator();
                    const gain = ctx.createGain();
                    osc.type = 'sine';
                    osc.frequency.value = frequencies[i] * 0.85;
                    gain.gain.setValueAtTime(gains[i] * 0.8, now2);
                    gain.gain.exponentialRampToValueAtTime(0.001, now2 + decays[i]);
                    osc.connect(gain);
                    gain.connect(ctx.destination);
                    osc.start(now2);
                    osc.stop(now2 + decays[i]);
                }
            }, 400);
        } catch (e) {
            console.warn('[PaymentRealtime] Web Audio error:', e);
        }
    }

    private buildHubUrl(): string {
        const baseUrl = environment.apiUrl.replace('/api/', '');
        return baseUrl + environment.paymentHubUrl;
    }

    /**
     * Get JWT token from localStorage (for SignalR authorization)
     */
    private getAccessToken(): string {
        const token = localStorage.getItem('authToken');
        if (!token) {
            console.warn('[PaymentRealtime] No auth token found in localStorage');
            return '';
        }
        return token;
    }

    /**
     * Initialize SignalR connection (call once at app startup)
     */
    async initializeConnection(): Promise<void> {
        if (this.connection && this.connection.state === HubConnectionState.Connected) {
            return;
        }

        if (this.connectionPromise) {
            return this.connectionPromise;
        }

        if (!this.getAccessToken()) {
            throw new Error('No authentication token available');
        }

        this._connectionState.set(HubConnectionState.Connecting);

        this.connectionPromise = (async () => {
            try {
                if (!this.connection) {
                    this.connection = new HubConnectionBuilder()
                        .withUrl(this.hubUrl, {
                            accessTokenFactory: () => this.getAccessToken(),
                            withCredentials: true,
                            skipNegotiation: false,
                            transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling
                        })
                        .withAutomaticReconnect([1000, 3000, 5000, 10000, 30000])
                        .configureLogging(LogLevel.Information)
                        .build();

                    // Setup event listener BEFORE connecting
                    this.setupTransactionUpdatedListener();
                    this.setupConnectionHandler();
                }

                if (this.connection.state === HubConnectionState.Disconnected) {
                    await this.connection.start();
                    this.clearRetry();
                    this.retryCount = 0;
                    this._connectionState.set(HubConnectionState.Connected);
                    console.info('[PaymentRealtime] SignalR connected successfully');
                    if (this.activeBranchCodes.length > 0) {
                        await this.connection.invoke('JoinBranches', this.activeBranchCodes);
                        this._branchReady.set(true);
                        await this.syncQrStaffCalls();
                    }
                }
            } catch (error) {
                console.error('[PaymentRealtime] Failed to connect', error);
                this._connectionState.set(HubConnectionState.Disconnected);
                this._branchReady.set(false);
                this.scheduleReconnect();
                throw error;
            } finally {
                this.connectionPromise = null;
            }
        })();

        return this.connectionPromise;
    }

    /**
     * Setup listener cho event TransactionUpdated từ backend
     * MUST called BEFORE invoking JoinTransaction
     */
    private setupTransactionUpdatedListener(): void {
        if (!this.connection) return;

        this.connection.on('TransactionUpdated', (status: IPaymentStatusEvent) => {
            console.info('[PaymentRealtime] Received TransactionUpdated event', status);

            const normalizedPayload = this.normalizeIncomingPayload(status);
            if (normalizedPayload) {
                this.upsertStatus(normalizedPayload);
            }
        });

        this.connection.on('ReceivePaymentNotification', (notification: any) => {
            console.info('[PaymentRealtime] Received ReceivePaymentNotification event', notification);
            const normalized = this.normalizeNotification(notification);
            if (normalized) {
                this._notifications.update(prev => [normalized, ...prev]);
                this._unreadCount.update(count => count + 1);
            }
        });

        this.connection.on('ReceiveQrOrderNotification', (notification: any) => {
            console.info('[PaymentRealtime] Received ReceiveQrOrderNotification event', notification);
            this.acceptQrNotification(notification, true);
        });

        this.connection.on('ReceiveBranchSalesUpdate', (event: ISalesRealtimeEvent) => {
            console.info('[PaymentRealtime] ReceiveBranchSalesUpdate:', event.eventType, event.orderId);
            this._salesEvent$.next(event);
        });
    }

    /**
     * Setup connection event handlers (reconnect, disconnect)
     */
    private setupConnectionHandler(): void {
        if (!this.connection) return;

        this.connection.onreconnected(async (connectionId) => {
            console.info('[PaymentRealtime] Reconnected, new connection ID:', connectionId);
            this._connectionState.set(HubConnectionState.Connected);

            // Resubscribe to branches
            try {
                if (this.connection && this.activeBranchCodes.length > 0) {
                    await this.connection.invoke('JoinBranches', this.activeBranchCodes);
                    this._branchReady.set(true);
                    await this.syncQrStaffCalls();
                }
            } catch (error) {
                this._branchReady.set(false);
                this.scheduleReconnect();
                console.warn('[PaymentRealtime] Failed to rejoin branches', error);
            }

            // Fallback: Sync status once to catch missed updates
            const transactionId = this._activeTransactionId();
            if (transactionId) {
                await this.joinTransaction(transactionId); //
                await this.syncStatusOnce(transactionId);
            }
        });

        // Reconcile state when user returns to tab (handles missed events during sleep/background)
        if (typeof document !== 'undefined') {
            document.addEventListener('visibilitychange', async () => {
                if (document.visibilityState === 'visible') {
                    void this.ensureConnection();
                    if (this.connection?.state === HubConnectionState.Connected) void this.syncQrStaffCalls();
                }
                if (document.visibilityState === 'visible' && this.connection?.state === HubConnectionState.Connected) {
                    console.info('[PaymentRealtime] Tab visible again - emitting reconcile signal');
                    this._salesEvent$.next({ eventType: 'Reconcile', timestamp: new Date().toISOString() });
                }
            });
        }

        this.connection.onreconnecting((error) => {
            console.warn('[PaymentRealtime] Reconnecting...', error?.message);
            this._connectionState.set(HubConnectionState.Reconnecting);
            this._branchReady.set(false);
        });

        this.connection.onclose((error) => {
            console.warn('[PaymentRealtime] Disconnected', error?.message);
            this._connectionState.set(HubConnectionState.Disconnected);
            this._branchReady.set(false);
            this.scheduleReconnect();
        });
    }

    private clearRetry(): void {
        if (this.retryTimer) clearTimeout(this.retryTimer);
        this.retryTimer = null;
    }

    private scheduleReconnect(): void {
        if (this.retryTimer || !this.activeBranchCodes.length || !this.getAccessToken()) return;
        const delay = Math.min(30000, 1000 * 2 ** Math.min(this.retryCount++, 5));
        this.retryTimer = setTimeout(() => {
            this.retryTimer = null;
            void this.ensureConnection();
        }, delay);
    }

    private async ensureConnection(): Promise<void> {
        if (!this.activeBranchCodes.length || !this.getAccessToken()) return;
        if (this.connection?.state === HubConnectionState.Connected) {
            if (!this._branchReady()) {
                try {
                    await this.connection.invoke('JoinBranches', this.activeBranchCodes);
                    this._branchReady.set(true);
                    this.clearRetry();
                    this.retryCount = 0;
                    await this.syncQrStaffCalls();
                } catch { this.scheduleReconnect(); }
            }
            return;
        }
        if (this.connection?.state === HubConnectionState.Reconnecting) return;
        try { await this.initializeConnection(); } catch { /* initializeConnection schedules retry. */ }
    }

    async disconnect(): Promise<void> {
        this.clearRetry();
        this.activeBranchCodes = [];
        if (this.connection) {
            try {
                await this.connection.stop();
                this.connection = null;
            } catch (error) {
                console.error('[PaymentRealtime] Disconnect failed', error);
            }
        }
        this._connectionState.set(HubConnectionState.Disconnected);
        this._branchReady.set(false);
        this._activeTransactionId.set(null);
    }

    async switchTransaction(transactionId: string | null): Promise<void> {
        const normalized = this.normalizeTransactionId(transactionId);
        const previous = this._activeTransactionId();
        if (previous === normalized) return;

        if (previous) {
            await this.leaveTransaction(previous);
        }

        this._activeTransactionId.set(normalized);
        if (normalized) {
            await this.joinTransaction(normalized);
        }
    }

    async joinTransaction(transactionId: string): Promise<void> {
        const normalized = this.normalizeTransactionId(transactionId);
        if (!normalized) {
            throw new Error('Invalid transaction ID');
        }

        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            throw new Error('SignalR not connected. Call initializeConnection() first');
        }

        try {
            this._activeTransactionId.set(normalized);
            await this.connection.invoke('JoinTransaction', normalized);
            console.info('[PaymentRealtime] Joined transaction via SignalR', normalized);
        } catch (error) {
            console.error('[PaymentRealtime] Failed to join transaction', normalized, error);
            this._activeTransactionId.set(null);
            throw error;
        }
    }

    async leaveTransaction(transactionId: string): Promise<void> {
        const normalized = this.normalizeTransactionId(transactionId);
        if (!normalized) return;

        if (this._activeTransactionId() === normalized) {
            this._activeTransactionId.set(null);
        }
    }

    async syncStatusOnce(transactionId: string): Promise<void> {
        const normalized = this.normalizeTransactionId(transactionId);
        if (!normalized) return;

        try {
            const response = await firstValueFrom(this.paymentService.getStatus(normalized));
            const entity = response?.Data ?? response;
            const normalizedPayload = this.normalizeIncomingPayload({
                ...entity,
                transactionId: entity?.transactionId ?? normalized,
                orderId: entity?.orderId ?? normalized
            });
            if (!normalizedPayload) return;

            this.upsertStatus(normalizedPayload);
        } catch (error) {
            console.warn('[PaymentRealtime] Sync status failed', normalized, error);
        }
    }

    getStatusByPaymentId(paymentId: string | null | undefined): IPaymentStatusEvent | null {
        if (!paymentId) return null;
        return this._statusByPaymentId()[paymentId] ?? null;
    }

    getLatestStatusByTransactionId(transactionId: string | null | undefined): IPaymentStatusEvent | null {
        const normalized = this.normalizeTransactionId(transactionId);
        if (!normalized) return null;

        return (
            Object.values(this._statusByPaymentId())
                .filter((item) => item.orderId === normalized || item.transactionId === normalized)
                .sort((a, b) => new Date(b.updatedAt ?? 0).getTime() - new Date(a.updatedAt ?? 0).getTime())[0] ?? null
        );
    }

    async updateBranchSubscription(branchCodes: string[]): Promise<void> {
        const previous = this.activeBranchCodes;
        this.activeBranchCodes = [...new Set(branchCodes.filter(Boolean))];
        if (!this.activeBranchCodes.length) {
            this.clearRetry();
            this._branchReady.set(false);
            if (this.connection?.state === HubConnectionState.Connected) {
                for (const code of previous) await this.connection.invoke('LeaveBranch', code);
            }
            return;
        }
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            try {
                await this.initializeConnection();
            } catch (error) {
                console.warn('[PaymentRealtime] Connection not ready yet, subscription queued:', branchCodes);
                return;
            }
        }
        if (this.connection && this.connection.state === HubConnectionState.Connected && this.activeBranchCodes.length > 0) {
            try {
                for (const code of previous.filter(code => !this.activeBranchCodes.includes(code))) {
                    await this.connection.invoke('LeaveBranch', code);
                }
                await this.connection.invoke('JoinBranches', this.activeBranchCodes);
                this._branchReady.set(true);
                await this.syncQrStaffCalls();
                console.info('[PaymentRealtime] Subscribed to branches:', this.activeBranchCodes);
            } catch (error) {
                this._branchReady.set(false);
                this.scheduleReconnect();
                console.error('[PaymentRealtime] Failed to subscribe to branches', branchCodes, error);
            }
        }
    }

    private rememberQrCall(id: string): void {
        this.seenQrCallIds.add(id);
        if (this.seenQrCallIds.size > 1000) this.seenQrCallIds.delete(this.seenQrCallIds.values().next().value!);
        try { localStorage.setItem('qrStaffCallSeenIds', JSON.stringify([...this.seenQrCallIds])); } catch { /* Storage unavailable. */ }
    }

    private acceptQrNotification(notification: any, live: boolean): boolean {
        const id = String(notification.id ?? notification.Id ?? '');
        if (id && this.seenQrCallIds.has(id)) return false;
        if (id) this.rememberQrCall(id);

        const type = notification.type ?? notification.Type;
        const tableName = notification.tableName ?? notification.TableName ?? '';
        const branchCode = notification.branchCode ?? notification.BranchCode ?? '';
        const orderCode = notification.orderCode ?? notification.OrderCode ?? '';
        const message = type === 'CallWaiter'
            ? `Bàn ${tableName} đang gọi nhân viên hỗ trợ.`
            : type === 'CallPayment'
                ? `Bàn ${tableName} yêu cầu thanh toán hóa đơn.`
                : `Đơn QR mới tại bàn ${tableName} (${orderCode}).`;
        const item = {
            id: id || `qr_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
            branchCode, transactionId: orderCode || tableName || 'QR',
            documentNumber: tableName ? `Bàn ${tableName}` : (orderCode || 'QR Order'),
            message: live ? message : `[Nhận bù] ${message}`,
            isRead: false, isQrCall: true,
            createdAt: notification.createdAt ?? notification.CreatedAt ?? new Date().toISOString()
        };
        this._notifications.update(prev => [item, ...prev]);
        this._unreadCount.update(count => count + 1);
        if (live) {
            this.playNotificationSound();
            this._qrOrderNotification.set({ ...notification, type, tableName });
        }
        return true;
    }

    private syncQrStaffCalls(): Promise<void> {
        if (this.qrSyncPromise) {
            this.qrSyncRequested = true;
            return this.qrSyncPromise;
        }
        this.qrSyncPromise = (async () => {
            do {
                this.qrSyncRequested = false;
                await this.fetchMissedQrStaffCalls();
            } while (this.qrSyncRequested);
        })().finally(() => { this.qrSyncPromise = null; });
        return this.qrSyncPromise;
    }

    private async fetchMissedQrStaffCalls(): Promise<void> {
        if (!this.activeBranchCodes.length) return;
        let recovered = 0;
        for (const branchCode of this.activeBranchCodes) {
            const key = `qrStaffCallsSyncedAt:${branchCode}`;
            const startedAt = new Date();
            const previous = localStorage.getItem(key);
            const previousTime = previous ? Date.parse(previous) : NaN;
            const since = Number.isFinite(previousTime)
                ? new Date(previousTime - 5000).toISOString()
                : new Date(startedAt.getTime() - 5 * 60 * 1000).toISOString();
            try {
                let skip = 0;
                while (true) {
                    const res = await firstValueFrom(this.paymentNotificationService.getQrStaffCalls([branchCode], since, skip));
                    if (res?.Code !== 200 || !Array.isArray(res.Data)) throw new Error('QR call sync failed');
                    for (const call of res.Data) if (this.acceptQrNotification(call, false)) recovered++;
                    if (res.Data.length < 200) break;
                    skip += res.Data.length;
                }
                localStorage.setItem(key, startedAt.toISOString());
            } catch (error) {
                console.warn('[PaymentRealtime] Failed to sync QR staff calls', branchCode, error);
            }
        }
        if (recovered > 0) this.playNotificationSound();
    }

    private normalizeNotification(n: any): any {
        if (!n) return null;
        return {
            id: n.id ?? n.Id,
            branchCode: n.branchCode ?? n.BranchCode,
            transactionId: n.transactionId ?? n.TransactionId,
            documentNumber: n.documentNumber ?? n.DocumentNumber,
            message: n.message ?? n.Message,
            amount: n.amount ?? n.Amount,
            isRead: n.isRead ?? n.IsRead,
            createdAt: n.createdAt ?? n.CreatedAt
        };
    }

    async loadInitialNotifications(branchCodes: string[]): Promise<void> {
        if (!branchCodes || branchCodes.length === 0) {
            this._notifications.set([]);
            this._unreadCount.set(0);
            return;
        }
        try {
            const notificationsRes = await firstValueFrom(this.paymentNotificationService.getNotifications(branchCodes));
            if (notificationsRes && notificationsRes.Code === 200) {
                const normalized = (notificationsRes.Data || []).map((n: any) => this.normalizeNotification(n));
                this._notifications.update(prev => [
                    ...prev.filter(n => n.isQrCall), ...normalized
                ].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
            }

            const unreadRes = await firstValueFrom(this.paymentNotificationService.getUnreadCount(branchCodes));
            if (unreadRes && unreadRes.Code === 200) {
                this._unreadCount.set((unreadRes.Data || 0) + this._notifications().filter(n => n.isQrCall && !n.isRead).length);
            }
        } catch (error) {
            console.error('[PaymentRealtime] Failed to load initial notifications', error);
        }
    }

    async markNotificationAsRead(id: string): Promise<void> {
        if (this._notifications().some(n => n.id === id && n.isQrCall)) {
            this._notifications.update(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
            this._unreadCount.update(count => Math.max(0, count - 1));
            return;
        }
        try {
            const res = await firstValueFrom(this.paymentNotificationService.markAsRead(id));
            if (res && res.Code === 200) {
                this._notifications.update(prev => 
                    prev.map(n => n.id === id ? { ...n, isRead: true } : n)
                );
                this._unreadCount.update(count => Math.max(0, count - 1));
            }
        } catch (error) {
            console.error('[PaymentRealtime] Failed to mark notification as read', error);
        }
    }

    async markAllNotificationsAsRead(branchCodes: string[]): Promise<void> {
        try {
            const res = await firstValueFrom(this.paymentNotificationService.markAllAsRead(branchCodes));
            if (res && res.Code === 200) {
                this._notifications.update(prev => 
                    prev.map(n => branchCodes.includes(n.branchCode) ? { ...n, isRead: true } : n)
                );
                this._unreadCount.set(this._notifications().filter(n => !n.isRead).length);
            }
        } catch (error) {
            console.error('[PaymentRealtime] Failed to mark all notifications as read', error);
        }
    }

    private normalizeTransactionId(transactionId: string | null | undefined): string | null {
        const value = transactionId?.trim();
        return value ? value : null;
    }

    private upsertStatus(payload: IPaymentStatusEvent): void {
        this._statusByPaymentId.update((prev) => ({
            ...prev,
            [payload.paymentId]: payload
        }));
        this._lastStatusEvent.set(payload);
    }

    private normalizeIncomingPayload(payload: any): IPaymentStatusEvent | null {
        const transactionId = this.normalizeTransactionId(payload?.transactionId ?? payload?.orderId ?? payload?.merchant_transaction_id);
        if (!transactionId) return null;
        const orderId = this.normalizeTransactionId(payload?.orderId) ?? transactionId;
        const paymentId = this.normalizeTransactionId(payload?.paymentId) ?? transactionId;
        const status = this.normalizeStatus(payload?.status);
        const updatedAt = this.normalizeTransactionId(payload?.updatedAt) ?? new Date().toISOString();
        const message = this.normalizeTransactionId(payload?.message) ?? '';
        const amountDue = this.normalizeAmount(payload?.amountDue);
        const amountPaid = this.normalizeAmount(payload?.amountPaid);

        return {
            orderId,
            paymentId,
            transactionId,
            status,
            updatedAt,
            message,
            amountDue,
            amountPaid
        };
    }

    private normalizeStatus(status: unknown): PaymentLifecycleStatus | null {
        if (status === 1 || status === '1') return 1;
        if (status === 2 || status === '2') return 2;

        return null;
    }

    private normalizeAmount(value: unknown): number | null {
        if (value === null || value === undefined || value === '') return null;
        const amount = Number(value);
        return Number.isFinite(amount) ? amount : null;
    }

    clearQrOrderNotification() {
        this._qrOrderNotification.set(null);
    }
}
