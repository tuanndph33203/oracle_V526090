import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, OnDestroy, signal, DestroyRef, HostListener } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MultiSelect } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { Checkbox } from 'primeng/checkbox';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import BranchFilterAction from '@/store/branch-filter/branch-filter.actions';
import { SalesService } from '../../../dining-table/services/sales';
import { PreparationAreaService } from '../../../preparation-area/services/preparation-area';
import { MessageService, ConfirmationService } from 'primeng/api';
import { environment } from 'src/environments/environment';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel, HttpTransportType } from '@microsoft/signalr';

@Component({
    selector: 'app-kitchen-dashboard',
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ButtonModule,
        SelectModule,
        DialogModule,
        TableModule,
        InputTextModule,
        InputNumberModule,
        ToastModule,
        ConfirmDialogModule,
        MultiSelect,
        TooltipModule,
        Checkbox
    ],
    providers: [ConfirmationService, MessageService],
    templateUrl: './kitchen.html',
    styleUrl: './kitchen.scss'
})
export class KitchenDashboard implements OnInit, OnDestroy {
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    private salesService = inject(SalesService);
    private prepAreaService = inject(PreparationAreaService);
    private messageService = inject(MessageService);
    private confirmationService = inject(ConfirmationService);

    // Filter states
    branchUser = signal<any[]>([]);
    selectedBranchCode = '';
    preparationAreas = signal<any[]>([]);
    selectedPrepAreaIds = signal<string[]>([]);
    isFullscreen = signal<boolean>(false);

    // Connection states
    private connection: HubConnection | null = null;
    connectionState = signal<string>('Disconnected');

    toggleFullscreen() {
        const isFull = !this.isFullscreen();
        this.isFullscreen.set(isFull);

        if (isFull) {
            const elem = document.documentElement;
            if (elem.requestFullscreen) {
                elem.requestFullscreen().catch(() => { });
            } else if ((elem as any).webkitRequestFullscreen) {
                (elem as any).webkitRequestFullscreen();
            } else if ((elem as any).msRequestFullscreen) {
                (elem as any).msRequestFullscreen();
            }
        } else {
            if (document.fullscreenElement || (document as any).webkitFullscreenElement) {
                if (document.exitFullscreen) {
                    document.exitFullscreen().catch(() => { });
                } else if ((document as any).webkitExitFullscreen) {
                    (document as any).webkitExitFullscreen();
                }
            }
        }
    }

    @HostListener('document:fullscreenchange')
    @HostListener('document:webkitfullscreenchange')
    onFullscreenChange() {
        const isNativeFull = !!(document.fullscreenElement || (document as any).webkitFullscreenElement);
        this.isFullscreen.set(isNativeFull);
    }

    getSelectedBranchName(): string {
        const branch = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        return branch ? branch.Name : '';
    }

    getSelectedPrepAreaNames(): string {
        const selectedIds = this.selectedPrepAreaIds();
        const areas = this.preparationAreas()
            .filter(x => selectedIds.includes(x.Id))
            .map(x => x.Name);
        return areas.join(', ');
    }

    hasValidTable(req: any): boolean {
        if (!req) return false;
        const name = (req.TableCode || req.TableName || '').trim();
        if (!name) return false;
        const invalidNames = ['không bàn', 'tại bàn', 'mang về', 'giao hàng', 'n/a', 'none', 'null', 'undefined', 'bàn'];
        return !invalidNames.includes(name.toLowerCase());
    }

    getValidTableText(req: any): string {
        if (!this.hasValidTable(req)) return '';
        return (req.TableCode || req.TableName || '').trim();
    }

    // Data lists
    requests = signal<any[]>([]);
    summaryItems = signal<any[]>([]);

    // Search query for summary
    summarySearchQuery = signal<string>('');

    // Checkbox selections: maps RequestDetailId to boolean or a set
    selectedDetailIds = new Set<string>();

    // Serve / Sold out dialogs
    actionDialogVisible = false;
    actionType: 'serve' | 'soldOut' = 'serve';
    actionItems = signal<any[]>([]); // Array of { RequestDetailId, MenuItemName, MaxQty, Quantity }

    // Periodic elapsed timer
    private elapsedTimer: any;

    constructor() {
        // Automatically start the elapsed timer to update request waiting times in real-time
        this.elapsedTimer = setInterval(() => {
            this.updateElapsedTimes();
        }, 1000);
    }

    ngOnInit() {
        // Load user branches
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            this.branchUser.set(branches || []);
            if (this.selectedBranchCode && this.preparationAreas().length === 0) {
                this.loadPrepAreas();
            }
        });

        // Sync with global branch selection
        this.store.pipe(
            select(selectSelectedBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            if (codes && codes.length > 0) {
                const newCode = codes[0].Code || codes[0];
                if (this.selectedBranchCode !== newCode) {
                    this.selectedBranchCode = newCode;
                    this.onBranchChange();
                } else if (this.preparationAreas().length === 0) {
                    this.loadPrepAreas();
                }
            }
        });
    }

    ngOnDestroy() {
        if (this.elapsedTimer) {
            clearInterval(this.elapsedTimer);
        }
        void this.stopSignalR();
    }

    onBranchChange() {
        this.selectedDetailIds.clear();
        this.loadPrepAreas();
        void this.restartSignalR();
    }

    loadPrepAreas() {
        this.prepAreaService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const list = (res.Data.Content || []).filter((x: any) => x.Status === 1);
                this.preparationAreas.set(list);
                // Default select all prep areas
                this.selectedPrepAreaIds.set(list.map((x: any) => x.Id));
                this.loadData();
            }
        });
    }

    onPrepAreaChange() {
        this.selectedDetailIds.clear();
        this.loadData();
    }

    loadData() {
        if (!this.selectedBranchCode) return;

        const areaIds = this.selectedPrepAreaIds();

        // Fetch chronological requests
        this.salesService.getKitchenRequests(this.selectedBranchCode, areaIds).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.requests.set(res.Data);
                this.updateElapsedTimes();
            } else {
                this.requests.set([]);
            }
        });

        // Fetch grouped summary items
        this.salesService.getKitchenSummary(this.selectedBranchCode, areaIds).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.summaryItems.set(res.Data);
            } else {
                this.summaryItems.set([]);
            }
        });
    }

    private updateElapsedTimes() {
        const list = this.requests();
        if (!list.length) return;

        const now = new Date();
        const updated = list.map(req => {
            const requestedTime = new Date(req.RequestedAt);
            const seconds = Math.floor((now.getTime() - requestedTime.getTime()) / 1000);
            return {
                ...req,
                ElapsedSeconds: seconds >= 0 ? seconds : 0
            };
        });
        this.requests.set(updated);
    }

    getWaitingTimeText(seconds: number): string {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;

        const mm = minutes.toString().padStart(2, '0');
        const ss = secs.toString().padStart(2, '0');

        if (hours > 0) {
            const hh = hours.toString().padStart(2, '0');
            return `${hh}:${mm}:${ss}`;
        }
        return `${mm}:${ss}`;
    }

    getWaitingTimeClass(seconds: number): string {
        if (seconds >= 900) { // 15 mins
            return 'bg-red-500 text-white animate-pulse';
        }
        if (seconds >= 450) { // 7.5 mins
            return 'bg-amber-500 text-white';
        }
        return 'bg-primary text-primary-contrast';
    }

    // Checkbox selections helper
    toggleItemSelection(detailId: string) {
        if (this.selectedDetailIds.has(detailId)) {
            this.selectedDetailIds.delete(detailId);
        } else {
            this.selectedDetailIds.add(detailId);
        }
    }

    isItemSelected(detailId: string): boolean {
        return this.selectedDetailIds.has(detailId);
    }

    toggleSummaryItemSelection(summaryItem: any) {
        // Find all request details that match this MenuItemId
        const matchingDetailIds: string[] = [];
        this.requests().forEach((req: any) => {
            (req.Details || []).forEach((d: any) => {
                if (d.MenuItemId === summaryItem.MenuItemId) {
                    matchingDetailIds.push(d.RequestDetailId);
                }
            });
        });

        if (matchingDetailIds.length === 0) return;

        // Check if all of them are already selected
        const allSelected = matchingDetailIds.every(id => this.selectedDetailIds.has(id));

        if (allSelected) {
            // Uncheck all matching items
            matchingDetailIds.forEach(id => this.selectedDetailIds.delete(id));
        } else {
            // Check all matching items
            matchingDetailIds.forEach(id => this.selectedDetailIds.add(id));
        }
    }

    isSummaryItemSelected(summaryItem: any): boolean {
        // Find all matching details in requests
        const matchingDetailIds: string[] = [];
        this.requests().forEach((req: any) => {
            (req.Details || []).forEach((d: any) => {
                if (d.MenuItemId === summaryItem.MenuItemId) {
                    matchingDetailIds.push(d.RequestDetailId);
                }
            });
        });

        if (matchingDetailIds.length === 0) return false;
        return matchingDetailIds.every(id => this.selectedDetailIds.has(id));
    }

    toggleSelectAllInRequest(req: any) {
        const details = req.Details || [];
        const allSelected = details.every((d: any) => this.selectedDetailIds.has(d.RequestDetailId));

        if (allSelected) {
            // Uncheck all
            details.forEach((d: any) => this.selectedDetailIds.delete(d.RequestDetailId));
        } else {
            // Check all
            details.forEach((d: any) => this.selectedDetailIds.add(d.RequestDetailId));
        }
    }

    isRequestAllSelected(req: any): boolean {
        const details = req.Details || [];
        return details.length > 0 && details.every((d: any) => this.selectedDetailIds.has(d.RequestDetailId));
    }

    hasSelection(): boolean {
        return this.selectedDetailIds.size > 0;
    }

    // Action execution (Serve / SoldOut)
    openActionDialog(type: 'serve' | 'soldOut') {
        if (!this.hasSelection()) return;
        this.actionType = type;

        // Gather all selected details from requests lists
        const itemsToProcess: any[] = [];
        this.requests().forEach((req: any) => {
            (req.Details || []).forEach((d: any) => {
                if (this.selectedDetailIds.has(d.RequestDetailId)) {
                    itemsToProcess.push({
                        RequestDetailId: d.RequestDetailId,
                        MenuItemName: d.MenuItemName,
                        RequestCode: req.RequestCode,
                        TableCode: req.TableCode,
                        TableName: req.TableName,
                        FloorName: req.FloorName,
                        MaxQty: d.RemainingQty,
                        Quantity: d.RemainingQty // default to max
                    });
                }
            });
        });

        this.actionItems.set(itemsToProcess);
        this.actionDialogVisible = true;
    }

    submitAction() {
        const items = this.actionItems();
        if (items.some(x => x.Quantity < 0 || x.Quantity > x.MaxQty)) {
            this.messageService.add({
                severity: 'error',
                summary: 'Lỗi số lượng',
                detail: 'Số lượng xử lý không hợp lệ hoặc lớn hơn số lượng còn lại!'
            });
            return;
        }

        const validItems = items.filter(x => x.Quantity > 0);
        if (validItems.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cảnh báo',
                detail: 'Vui lòng chọn ít nhất 1 món có số lượng lớn hơn 0!'
            });
            return;
        }

        if (this.actionType === 'serve') {
            const payload = {
                Items: validItems.map(x => ({
                    RequestDetailId: x.RequestDetailId,
                    ServeQty: x.Quantity
                }))
            };

            this.salesService.serveItems(payload).subscribe((res: any) => {
                if (res?.Code === 200) {
                    const printResults = res.Data?.PrintDispatchResults ?? [];
                    const unassignedSlips = res.Data?.PrintSlips ?? [];
                    this.messageService.add({
                        severity: printResults.some((x: any) => x.Status === 'Failed') || unassignedSlips.length > 0 ? 'warn' : 'success',
                        summary: 'Đã trả món',
                        detail: printResults.some((x: any) => x.Status === 'Failed') || unassignedSlips.length > 0
                            ? 'Đã trả món, nhưng có phiếu chưa gửi được đến máy in. Vui lòng kiểm tra cấu hình in theo khu vực.'
                            : 'Đã gửi phiếu trả món đến máy in theo khu vực.'
                    });
                    this.actionDialogVisible = false;
                    this.selectedDetailIds.clear();
                    this.loadData();
                } else {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: res.Message || 'Có lỗi xảy ra!'
                    });
                }
            });
        } else {
            const payload = {
                Items: validItems.map(x => ({
                    RequestDetailId: x.RequestDetailId,
                    SoldOutQty: x.Quantity
                }))
            };

            this.salesService.soldOutItems(payload).subscribe((res: any) => {
                if (res?.Code === 200) {
                    const printResults = res.Data?.PrintDispatchResults ?? [];
                    const unassignedSlips = res.Data?.PrintSlips ?? [];
                    this.messageService.add({
                        severity: printResults.some((x: any) => x.Status === 'Failed') || unassignedSlips.length > 0 ? 'warn' : 'success',
                        summary: 'Đã báo hết món',
                        detail: printResults.some((x: any) => x.Status === 'Failed') || unassignedSlips.length > 0
                            ? 'Đã báo hết món, nhưng có phiếu chưa gửi được đến máy in. Vui lòng kiểm tra cấu hình in theo khu vực.'
                            : 'Đã gửi phiếu báo hết món đến máy in theo khu vực.'
                    });
                    this.actionDialogVisible = false;
                    this.selectedDetailIds.clear();
                    this.loadData();
                } else {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: res.Message || 'Có lỗi xảy ra!'
                    });
                }
            });
        }
    }

    // Group summary list filtered search queries
    getFilteredSummaryItems(): any[] {
        const q = this.summarySearchQuery().toLowerCase().trim();
        const list = this.summaryItems();
        if (!q) return list;
        return list.filter(item => item.MenuItemName.toLowerCase().includes(q));
    }

    playNotificationSound() {
        try {
            const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
            const now = ctx.currentTime;
            const frequencies = [830, 1245, 1660, 2075];
            const gains = [0.12, 0.08, 0.05, 0.03];
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
            console.warn('Web Audio not supported or blocked', e);
        }
    }

    // ────────────────────────────────────────────────────────────
    // SIGNALR HUB CONNECTION
    // ────────────────────────────────────────────────────────────
    private async restartSignalR() {
        await this.stopSignalR();
        await this.startSignalR();
    }

    private async startSignalR() {
        if (!this.selectedBranchCode) return;
        const token = localStorage.getItem('authToken');
        if (!token) return;

        // Build the KitchenHub SignalR connection URL
        const apiBaseUrl = environment.apiUrl.replace('/api/', '');
        const hubUrl = `${apiBaseUrl}/kitchenHub`;

        this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token,
                withCredentials: true,
                skipNegotiation: false,
                transport: HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([1000, 3000, 5000, 10000])
            .configureLogging(LogLevel.Warning)
            .build();

        // Register events
        this.connection.on('NewKitchenRequest', (request: any) => {
            console.log('[KitchenHub] NewKitchenRequest received:', request);
            this.playNotificationSound();
            this.loadData();
        });

        this.connection.on('KitchenRequestsUpdated', () => {
            console.log('[KitchenHub] KitchenRequestsUpdated received');
            this.loadData();
        });

        this.connection.onclose(() => {
            this.connectionState.set('Disconnected');
        });

        this.connection.onreconnecting(() => {
            this.connectionState.set('Reconnecting');
        });

        this.connection.onreconnected(() => {
            this.connectionState.set('Connected');
            void this.joinBranchGroup();
        });

        try {
            this.connectionState.set('Connecting');
            await this.connection.start();
            this.connectionState.set('Connected');
            await this.joinBranchGroup();
            console.log('[KitchenHub] SignalR connected and joined branch group successfully');
        } catch (err) {
            console.error('[KitchenHub] SignalR connection failed:', err);
            this.connectionState.set('Disconnected');
        }
    }

    private async joinBranchGroup() {
        if (this.connection && this.connection.state === HubConnectionState.Connected && this.selectedBranchCode) {
            await this.connection.invoke('JoinBranch', this.selectedBranchCode);
        }
    }

    private async stopSignalR() {
        if (this.connection) {
            try {
                if (this.connection.state === HubConnectionState.Connected && this.selectedBranchCode) {
                    await this.connection.invoke('LeaveBranch', this.selectedBranchCode);
                }
                await this.connection.stop();
            } catch (err) {
                console.warn('[KitchenHub] Error while stopping hub connection:', err);
            } finally {
                this.connection = null;
                this.connectionState.set('Disconnected');
            }
        }
    }

    printHtmlViaHiddenIframe(htmlContent: string) {
        let iframe = document.getElementById('pos-silent-print-frame') as HTMLIFrameElement;
        if (!iframe) {
            iframe = document.createElement('iframe');
            iframe.id = 'pos-silent-print-frame';
            iframe.style.position = 'fixed';
            iframe.style.right = '0';
            iframe.style.bottom = '0';
            iframe.style.width = '0';
            iframe.style.height = '0';
            iframe.style.border = '0';
            iframe.style.visibility = 'hidden';
            iframe.style.zIndex = '-9999';
            document.body.appendChild(iframe);
        }

        const iframeWin = iframe.contentWindow;
        const iframeDoc = iframeWin?.document || iframe.contentDocument;
        if (!iframeDoc || !iframeWin) return;

        iframeDoc.open();
        iframeDoc.write(htmlContent);
        iframeDoc.close();

        let printed = false;
        const triggerPrint = () => {
            if (printed) return;
            printed = true;
            try {
                iframeWin.focus();
                iframeWin.print();
            } catch (err) {
                console.error('Lỗi khi in qua iframe:', err);
            }
        };

        iframeWin.onload = triggerPrint;
        setTimeout(triggerPrint, 350);
    }

    printKitchenActionReceipt(items: any[], type: 'serve' | 'soldOut') {
        const dateStr = new Date().toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });
        const title = type === 'serve' ? 'PHIẾU TRẢ MÓN' : 'PHIẾU BÁO HẾT MÓN';

        let itemsHtml = '';
        items.forEach((item: any) => {
            itemsHtml += `
                <tr style="border-bottom: 1px dashed #000; font-size: 13px;">
                    <td style="padding: 6px 0; font-weight: 700; width: 50%; color: #000;">${item.MenuItemName}</td>
                    <td style="padding: 6px 0; font-weight: 700; text-align: center; width: 30%; color: #000;">${this.hasValidTable(item) ? (this.getValidTableText(item) + (item.FloorName ? ' - Sàn: ' + item.FloorName : '')) : '-'}</td>
                    <td style="padding: 6px 0; font-size: 18px; font-weight: 900; text-align: center; width: 20%; color: #000;">${item.Quantity}</td>
                </tr>
            `;
        });

        const contentHtml = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>${title}</title>
                <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet">
                <style>
                    @page {
                        size: auto;
                        margin: 0mm;
                    }
                    html, body {
                        margin: 0;
                        padding: 0;
                        width: 100%;
                    }
                    body {
                        width: 80mm;
                        font-family: 'Inter', Arial, sans-serif;
                        font-size: 13px;
                        line-height: 1.35;
                        padding: 0 4px 4px 4px;
                        box-sizing: border-box;
                        color: #000;
                    }
                    .text-center { text-align: center; }
                    .title { font-size: 18px; font-weight: 900; text-transform: uppercase; margin-top: 0; margin-bottom: 4px; color: #000; }
                    .divider { border-top: 2px solid #000; margin: 8px 0; }
                    .info-row { display: flex; justify-content: space-between; font-size: 12px; font-weight: 700; margin-bottom: 4px; color: #000; }
                    @media print {
                        @page {
                            size: auto;
                            margin: 0mm;
                        }
                        html, body {
                            margin: 0;
                            padding: 0;
                            width: 100%;
                        }
                        body {
                            width: 80mm;
                            padding: 0 4px 4px 4px;
                        }
                    }
                </style>
            </head>
            <body>
                <div class="text-center">
                    <div class="title">${title}</div>
                </div>
                <div class="divider"></div>
                <div class="info-row">
                    <span>Thời gian: ${dateStr}</span>
                </div>
                <div class="divider"></div>
                <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="border-bottom: 2px solid #000; font-weight: 900; font-size: 14px; color: #000;">
                            <td style="text-align: left; padding-bottom: 6px; width: 50%;">Tên món</td>
                            <td style="text-align: center; padding-bottom: 6px; width: 30%;">Bàn</td>
                            <td style="text-align: center; padding-bottom: 6px; width: 20%;">SL</td>
                        </tr>
                    </thead>
                    <tbody>
                        ${itemsHtml}
                    </tbody>
                </table>
                <div class="divider"></div>
                <div class="text-center" style="margin-top: 10px; font-size: 14px; font-weight: 900; color: #000;">
                    --- HẾT ---
                </div>

                <!-- Paper Feed for Thermal Printer Auto-Cutter Blade (35mm feed pushes paper past cutter) -->
                <div style="height: 35mm; min-height: 35mm; clear: both; line-height: 35mm;">&nbsp;</div>
            </body>
            </html>
        `;

        this.printHtmlViaHiddenIframe(contentHtml);
    }
}
