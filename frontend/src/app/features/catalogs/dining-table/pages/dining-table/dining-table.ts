import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, DestroyRef, HostListener, effect, untracked } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DrawerModule } from 'primeng/drawer';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { timeout } from 'rxjs';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { selectPermissions, selectUser, selectEmployee } from '@/store/auth/auth.selectors';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import SystemSettingsAction from '@/store/system-settings/system-settings.action';
import BranchFilterAction from '@/store/branch-filter/branch-filter.actions';
import { FloorPlanService } from '../../../floor-plan/services/floor-plan';
import { forkJoin, Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { SalesService } from '../../services/sales';
import { DiningTableService } from '../../services/dining-table';
import { EmployeeService } from '../../../../employee/services/empoyee';
import { MenuItemService } from '../../../menu-item/services/menu-item';
import { ShiftSessionService } from '@/features/catalogs/shift-session/services/shift-session';
import { Router } from '@angular/router';
import { MenuGroupService } from '../../../menu-group/services/menu-group';
import { MessageService, ConfirmationService } from 'primeng/api';
import { environment } from 'src/environments/environment';
import { DialogService } from 'primeng/dynamicdialog';
import { QrCode } from '@/core/layout/qr-code/qr-code';
import { PaymentService } from '@/core/services/payment';
import { PaymentRealtimeService } from '@/core/services/payment-realtime';
import { ISalesRealtimeEvent } from '@/core/models/payment-realtime';
import { TooltipModule } from 'primeng/tooltip';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { ReasonService } from '../../../reason/services/reason';
import { PromotionService } from '../../../promotions/services/promotion';
import { BranchService } from '@/features/branch/services/branch';
import { MultiSelect } from 'primeng/multiselect';
import { Checkbox } from 'primeng/checkbox';
import { Badge } from 'primeng/badge';
import { Popover } from 'primeng/popover';
import { ProgressSpinner } from 'primeng/progressspinner';
import { ClockService } from '@/core/services/clock';
import { SelectedOrderItemsComponent } from '../../components/selected-order-items/selected-order-items';
import { OrderMetadataFormComponent } from '../../components/order-metadata-form/order-metadata-form';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { OrderCalculator } from '@/core/utils/order-calculator';
import { LayoutService } from '@/core/services/layout';
import { QRCodeComponent } from 'angularx-qrcode';

@Component({
    selector: 'app-dining-table',
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
        DrawerModule,
        TooltipModule,
        MultiSelect,
        Checkbox,
        Badge,
        Popover,
        ProgressSpinner,
        SelectedOrderItemsComponent,
        OrderMetadataFormComponent,
        HasPermissionDirective,
        QRCodeComponent
    ],
    providers: [ConfirmationService],
    templateUrl: './dining-table.html',
    styleUrl: './dining-table.scss'
})
export class DiningTable implements OnInit {
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    remoteQrVisible = false;
    remoteQrInfo: any = null;

    showRemoteOrderQr() {
        this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: 'Chức năng đặt từ xa đã được tắt' });
    }

    async copyRemoteOrderLink(url: string, label: string) {
        if (!url) return;
        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(url);
            } else {
                const input = document.createElement('textarea');
                input.value = url;
                input.style.position = 'fixed';
                input.style.opacity = '0';
                document.body.appendChild(input);
                try {
                    input.select();
                    if (!document.execCommand('copy')) throw new Error('Clipboard unavailable');
                } finally {
                    input.remove();
                }
            }
            this.messageService.add({ severity: 'success', summary: 'Đã sao chép', detail: label });
        } catch {
            this.messageService.add({ severity: 'error', summary: 'Không sao chép được link', detail: 'Vui lòng chọn và sao chép link thủ công.' });
        }
    }

    downloadRemoteQr(type: 'takeaway' | 'delivery') {
        const containerId = type === 'takeaway' ? '#remote-qr-takeaway' : '#remote-qr-delivery';
        const qrCanvas = document.querySelector(`${containerId} canvas`) as HTMLCanvasElement;
        if (!qrCanvas) {
            this.messageService.add({ severity: 'warn', summary: 'Chưa tải xong mã QR', detail: 'Vui lòng đợi vài giây và thử lại.' });
            return;
        }

        try {
            const label = type === 'takeaway' ? 'ĐƠN MANG VỀ' : 'ĐƠN GIAO TẬN NƠI';
            const branchName = this.branchUser()?.find((b: any) => b.Code === this.selectedBranchCode)?.Name || this.selectedBranchCode || '';

            const exportCanvas = document.createElement('canvas');
            const padding = 20;
            const textHeight = 52;
            exportCanvas.width = qrCanvas.width + padding * 2;
            exportCanvas.height = qrCanvas.height + padding * 2 + textHeight;
            const ctx = exportCanvas.getContext('2d');
            if (ctx) {
                ctx.fillStyle = '#ffffff';
                ctx.fillRect(0, 0, exportCanvas.width, exportCanvas.height);
                ctx.drawImage(qrCanvas, padding, padding);

                ctx.fillStyle = type === 'takeaway' ? '#047857' : '#1d4ed8';
                ctx.font = 'bold 15px sans-serif';
                ctx.textAlign = 'center';
                ctx.fillText(label, exportCanvas.width / 2, qrCanvas.height + padding + 22);

                if (branchName) {
                    ctx.fillStyle = '#475569';
                    ctx.font = '12px sans-serif';
                    ctx.fillText(branchName, exportCanvas.width / 2, qrCanvas.height + padding + 40);
                }
            }

            const url = (ctx ? exportCanvas : qrCanvas).toDataURL('image/png');
            const a = document.createElement('a');
            a.href = url;
            a.download = `QR_${type === 'takeaway' ? 'MangVe' : 'GiaoHang'}_${this.selectedBranchCode || ''}.png`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            this.messageService.add({ severity: 'success', summary: 'Tải thành công', detail: `Đã tải mã QR ${type === 'takeaway' ? 'Mang về' : 'Giao hàng'}` });
        } catch {
            this.messageService.add({ severity: 'error', summary: 'Lỗi tải QR', detail: 'Không thể tải mã QR' });
        }
    }

    /** True khi viewport <= 991px — reactive signal, cập nhật khi resize */
    isMobile = signal<boolean>(typeof window !== 'undefined' && window.innerWidth <= 991);

    @HostListener('window:resize')
    onWindowResize() {
        this.isMobile.set(typeof window !== 'undefined' && window.innerWidth <= 991);
    }
    private floorPlanService = inject(FloorPlanService);
    public salesService = inject(SalesService);
    private messageService = inject(MessageService);
    private confirmationService = inject(ConfirmationService);
    private employeeService = inject(EmployeeService);
    private menuItemService = inject(MenuItemService);
    private menuGroupService = inject(MenuGroupService);
    private dialogService = inject(DialogService);
    private paymentService = inject(PaymentService);
    public paymentRealtimeService = inject(PaymentRealtimeService);
    /** Debounce trigger for table map reload - prevents API storm when multiple events fire quickly */
    private readonly _tableReloadTrigger$ = new Subject<void>();
    private reasonService = inject(ReasonService);
    public clockService = inject(ClockService);
    private promotionService = inject(PromotionService);
    private shiftService = inject(ShiftSessionService);
    private branchService = inject(BranchService);
    private diningTableService = inject(DiningTableService);
    public layoutService = inject(LayoutService);
    private router = inject(Router);
    private kitchenConnection: HubConnection | null = null;

    allBranchTables: any[] = [];

    eInvoiceBranchConfigs = signal<any[]>([]);
    eInvoiceBranchConfigsLoaded = false;

    selectedPromotionCode: string | null = null;
    promoDialogVisible = signal<boolean>(false);
    promotionsList = signal<any[]>([]);
    loadingPromotions = signal<boolean>(false);
    requireKitchenSend = signal<boolean>(true);

    paymentQrUrl: string | null = null;
    paymentTransactionId = '';
    isPaying = signal<boolean>(false);
    private readonly localPaymentOrderIds = new Map<string, number>();
    private readonly localCancellationOrderIds = new Map<string, number>();
    private _locallyHandledPaidOrderId = '';
    private _locallyHandledCancelledOrderId = '';
    cancelReasons = signal<any[]>([]);
    selectedCancelReasons: string[] = [];

    branchUser = signal<any[]>([]);
    currentUser = signal<any>(null);
    currentEmployee = signal<any>(null);
    userPermissions = signal<string[]>([]);
    selectedBranchCode = '';
    private menuVisibilityState: string | null = null;
    private menuVisibilityPollInFlight = false;
    private lastPeriodicMenuRefreshAt = Date.now();
    floors: any[] = [];
    tables: any[] = [];

    /** Trên mobile: sắp xếp bàn theo tên (TableCode / TableName) để dễ tìm kiếm */
    tablesSorted(): any[] {
        if (!this.isMobile()) return this.tables;
        return [...this.tables].sort((a, b) => {
            const nameA = (a.TableCode || a.TableName || '').toLowerCase();
            const nameB = (b.TableCode || b.TableName || '').toLowerCase();
            return nameA.localeCompare(nameB, 'vi', { numeric: true, sensitivity: 'base' });
        });
    }
    activeFloor: any = null;
    isLoadingTables = signal<boolean>(false);
    isSendingKitchen = signal<boolean>(false);
    multiSelectMode = signal<boolean>(true);
    isOpeningTable = false;
    openMenuPending = false;
    processedQrNotificationIds = new Set<string>();
    lastNotificationTime = 0;

    // Status Filter selection
    statusFilter = signal<string>('All');
    statusFilters = [
        { label: 'Tất cả', value: 'All' },
        { label: 'Trống', value: 'Empty' },
        { label: 'Đang phục vụ', value: 'Serving' },
        { label: 'Chờ phục vụ', value: 'WaitingService' },
        { label: 'Đã kiểm đồ', value: 'Checked' },
        { label: 'Đặt trước', value: 'Reserved' },
        { label: 'Chờ thanh toán', value: 'WaitingPayment' },
        { label: 'Chờ xác nhận', value: 'WaitingConfirm' },
        { label: 'Mang về', value: 'TakeAway' },
        { label: 'Giao hàng', value: 'Delivery' }
    ];
    takeAwayCount = signal<number>(0);
    deliveryCount = signal<number>(0);
    waitingConfirmCount = signal<number>(0);
    allTablesOfFloor: any[] = [];
    emptyCount = signal<number>(0);
    servingCount = signal<number>(0);
    reservedCount = signal<number>(0);
    waitingPaymentCount = signal<number>(0);
    joinedTransactionIds = new Set<string>();
    allActiveOrders: any[] = [];
    availableEmptyTables: any[] = [];

    // Multi-Selection State
    selectedTableIds = signal<string[]>([]);

    // View modes
    viewMode = this.layoutService.posViewMode;
    displayMode = signal<'services' | 'summary'>('summary');

    // Order List View Filters & Search
    orderListSearchQuery = signal<string>('');
    orderListStatusFilter = signal<string>('All');
    orderListFloorFilter = signal<string>('');
    orderListTypeFilter = signal<string>('All');
    isLoadingOrderList = signal<boolean>(false);

    // Dialog Visibility states
    mergeDialogVisible = false;
    splitDialogVisible = false;
    transferDialogVisible = false;
    checkDialogVisible = false;
    editBillDialogVisible = false;
    cancelDialogVisible = false;
    cancelItemDialogVisible = false;
    cancellingItem: any = null;
    cancellingItemBaseQty = 0;
    cancelItemQty = 1;
    cancelItemReason = '';
    isCancellingItem = false;
    quickCancelDishReasons: string[] = [
        'Khách đổi món',
        'Khách chờ lâu',
        'Hết nguyên liệu / Món hỏng',
        'Nhân viên nhập nhầm',
        'Khách hủy không ăn'
    ];
    eInvoiceDialogVisible = false;
    pendingPrintContext: { type: 'order' | 'check' | 'payment' | 'card', data?: any } | null = null;
    skipCloseDrawerFromPopup = false;

    // Order selection states
    selectedOrderId: string | null = null;
    activeOrdersOfSelectedTable: any[] = [];
    isOrderSelectPopupOpen = false;
    pendingActionAfterOrderSelect: (() => void) | null = null;

    // Sub-view Working Data Models
    mergePreview: any = null;
    mergeTargetTableId: string = '';
    mergeSelectedTableIds = new Map<string, boolean>();
    cachedQrs = new Map<string, { qrUrl: string, transactionId: string }>();
    tableActiveItemsMap = new Map<string, any[]>();
    lastProcessedNotificationId: string | null = null;

    splitPreview: any = null;
    splitTargetTableId: string = '';
    splitIsNewBill = true;
    splitCustomerCount = 1;
    splitItems: any[] = []; // Array of { ItemId, ItemName, MaxQty, Quantity }

    transferTargetTableIds: string[] = [];
    transferNote: string = '';
    availableTargetTables: any[] = [];

    checkBillDetail: any = null;
    editBillDetail: any = null;
    editBillItems: any[] = []; // Current editing bill items
    editCustomerCount = 1;

    cancelReason = '';
    cancelTableId = '';
    serviceTypeOrders: any[] = [];

    // Order Editing Panel States
    isEditingOrder = false;
    currentOrder: any = null;
    menuItems: any[] = [];
    filteredMenuItems: any[] = [];
    waiters: any[] = [];
    searchQuery = '';
    orderWaiterId: string | null = null;
    orderCustomerCount = 1;
    orderItems: any[] = [];

    // Customer & Delivery States
    customers = signal<any[]>([]);
    orderCustomerId: string | null = null;
    phoneNumber = '';
    deliveryAddress = '';
    deliveryNote = '';
    shippingFeeNote = '';
    therapyVoucherCode = '';
    therapyServices: any[] = [];
    selectedTherapyServiceCode = '';
    orderNote = '';
    openTime: Date | null = new Date();
    enableInvoiceDate = signal<boolean>(true);
    enableKitchenPrint = signal<boolean>(true);
    kitchenPrintByArea = signal<boolean>(true);
    kitchenDefaultAreaId = signal<string>('');
    invoiceLockEnabled = signal<boolean>(false);
    invoiceLockDate = signal<string>('');
    invoiceLockAuto = signal<boolean>(true);
    invoiceLockAutoDays = signal<number>(0);
    isDeliveryInfoCollapsed = false;

    // Menu category / group filters
    selectedMenuType = '';
    selectedGroupId = '';
    menuGroups: any[] = [];
    topUsedMenuItems: any[] = [];

    // Food selection popup states
    foodDialogVisible = false;
    mobileFoodTab: 'menu' | 'order' = 'menu';
    mergeQrDialogVisible = false;
    currentQrMergeOrder: any = null;
    selectedTargetOrderId: string | null = null;
    foodSearchQuery = '';
    foodFilteredMenuItems: any[] = [];
    showSoldOutDialog = false;
    soldOutMenuItems = signal<any[]>([]);
    apiUrl = environment.apiUrl;

    private waiterSearchTimeout: any = null;
    private customerSearchTimeout: any = null;
    private foodSearchDebounceTimer: any = null;
    isBillEditUnlocked = false;
    appliedOrderLevelPromoDiscount = 0;
    appliedOrderLevelManualDiscount = 0;

    // Zoom state
    zoom = signal<number>(100);

    private getZoomStorageKey(branchCode?: string, floorId?: string): string | null {
        const branch = branchCode || this.selectedBranchCode;
        const floor = floorId || this.activeFloor?.Id || this.activeFloor?.Code;
        if (!branch || !floor) return null;
        return `floor_zoom_${branch}_${floor}`;
    }

    restoreZoomForCurrentFloor() {
        const key = this.getZoomStorageKey();
        if (!key || typeof localStorage === 'undefined') {
            this.zoom.set(100);
            return;
        }
        const saved = localStorage.getItem(key);
        if (saved) {
            const val = parseInt(saved, 10);
            if (!isNaN(val) && val >= 30 && val <= 300) {
                this.zoom.set(val);
                return;
            }
        }
        this.zoom.set(100);
    }

    saveZoomForCurrentFloor(zoomValue?: number) {
        const key = this.getZoomStorageKey();
        if (!key || typeof localStorage === 'undefined') return;
        const val = zoomValue ?? this.zoom();
        localStorage.setItem(key, val.toString());
    }

    zoomIn() {
        this.zoom.update(z => {
            const newZ = Math.min(200, z + 10);
            this.saveZoomForCurrentFloor(newZ);
            return newZ;
        });
    }

    zoomOut() {
        this.zoom.update(z => {
            const newZ = Math.max(50, z - 10);
            this.saveZoomForCurrentFloor(newZ);
            return newZ;
        });
    }

    zoomReset() {
        this.zoom.set(100);
        this.saveZoomForCurrentFloor(100);
    }

    /** Auto-fit: tính zoom để toàn bộ bàn vừa vào viewport canvas */
    zoomFit() {
        if (!this.tables || this.tables.length === 0) {
            this.zoom.set(100);
            this.saveZoomForCurrentFloor(100);
            return;
        }
        const maxX = Math.max(...this.tables.map(t => (t.X || 0) + (t.Width || 150)));
        const maxY = Math.max(...this.tables.map(t => (t.Y || 0) + (t.Height || 150)));
        const canvasEl = document.querySelector('.canvas-workspace') as HTMLElement;
        if (!canvasEl) {
            this.zoom.set(100);
            this.saveZoomForCurrentFloor(100);
            return;
        }
        const vw = canvasEl.clientWidth - 32;
        const vh = canvasEl.clientHeight - 32;
        const scaleX = vw / maxX;
        const scaleY = vh / maxY;
        const fitted = Math.floor(Math.min(scaleX, scaleY) * 100);
        const finalZoom = Math.max(30, Math.min(150, fitted));
        this.zoom.set(finalZoom);
        this.saveZoomForCurrentFloor(finalZoom);
    }

    /** Tính số phút bàn đã ngồi từ OpenTime */
    getElapsedMinutes(openTime: string | null): number {
        if (!openTime) return 0;
        const open = new Date(openTime).getTime();
        return Math.floor((Date.now() - open) / 60000);
    }

    /** Format thời gian ngồi dạng HH:mm hoặc XXp */
    getElapsedLabel(openTime: string | null): string {
        const mins = this.getElapsedMinutes(openTime);
        if (mins < 60) return `${mins}p`;
        const h = Math.floor(mins / 60);
        const m = mins % 60;
        return `${h}h${m > 0 ? m + 'p' : ''}`;
    }

    /** Màu cảnh báo theo thời gian ngồi: xanh < 30p, vàng 30-60p, đỏ > 60p */
    getElapsedUrgencyClass(openTime: string | null): string {
        const mins = this.getElapsedMinutes(openTime);
        if (mins >= 90) return 'text-red-600 font-black';
        if (mins >= 45) return 'text-amber-600 font-bold';
        return 'text-slate-500 font-semibold';
    }

    // 2-level Food Filters
    level1Filter: string = 'Popular';
    level2Filter = '';

    getFilteredMenuGroups(): any[] {
        const typeMap: Record<string, number> = {
            'Food': 1,
            'Beverage': 2,
            'Combo': 3,
            'Others': 4
        };
        const targetType = typeMap[this.level1Filter];
        if (!targetType) return [];
        return this.menuGroups.filter(g => g.Type === targetType);
    }

    // Transfer dialog helper filters
    transferFloorOptions: any[] = [];
    transferSelectedFloorId = '';
    transferSearchText = '';

    getFilteredTargetTables(): any[] {
        let list = this.availableTargetTables || [];
        if (this.transferSelectedFloorId) {
            list = list.filter(t => t.FloorPlanId === this.transferSelectedFloorId);
        }
        if (this.transferSearchText && this.transferSearchText.trim()) {
            const query = this.transferSearchText.toLowerCase().trim();
            list = list.filter(t =>
                (t.TableName && t.TableName.toLowerCase().includes(query)) ||
                (t.TableCode && t.TableCode.toLowerCase().includes(query))
            );
        }
        return list;
    }
    // Payment Dialog States
    paymentDialogVisible = false;
    paymentType = 'Cash';
    paymentAmount = 0;

    constructor() {
        effect(() => {
            const mode = this.layoutService.posViewMode();
            if (mode === 'order-list') {
                untracked(() => {
                    this.loadOrderList();
                });
            }
        });

        effect(() => {
            const lastEvent = this.paymentRealtimeService.lastStatusEvent();
            if (!lastEvent) return;

            // Check if status is 2 (Paid)
            if (lastEvent.status === 2) {
                const txId = lastEvent.transactionId;

                // Case 1: If the payment dialog is currently open for THIS transaction
                if (this.paymentDialogVisible && this.paymentTransactionId === txId) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Thông báo',
                        detail: 'Nhận tiền chuyển khoản thành công!'
                    });

                    // Immediately update UI states and reload tables without calling backend APIs again
                    this.paymentDialogVisible = false;
                    this.isEditingOrder = false;
                    this.clearSelection();
                    this._tableReloadTrigger$.next();

                    this.paymentQrUrl = null;
                    this.paymentTransactionId = '';
                    return;
                }

                // Case 2: Background payment update (payment dialog not open or open for another transaction)
                const matchedOrder = this.allActiveOrders.find(x => x.OrderCode === txId || x.BillCode === txId);
                if (matchedOrder) {
                    // Show a toast message to the user that payment was received in the background
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Thanh toán thành công',
                        detail: `Đơn hàng ${txId} (${matchedOrder.TableName || 'Mang về/Giao hàng'}) đã được thanh toán thành công tự động!`,
                        life: 5000
                    });

                    // Reload the table map to reflect the green/paid state immediately
                    this._tableReloadTrigger$.next();
                }
            }
        });

        effect(() => {
            const notifications = this.paymentRealtimeService.notifications();
            if (notifications.length > 0) {
                const latestNotification = notifications[0];
                if (latestNotification && latestNotification.id !== this.lastProcessedNotificationId) {
                    this.lastProcessedNotificationId = latestNotification.id;

                    // Reload the tables to reflect updated statuses/cleared tables
                    this._tableReloadTrigger$.next();

                    // If currently editing the paid order, clear selection
                    const paidOrderCode = latestNotification.transactionId;
                    if (paidOrderCode && this.isEditingOrder && this.currentOrder && this.currentOrder.OrderCode === paidOrderCode) {
                        this.isEditingOrder = false;
                        this.clearSelection();
                    }
                }
            }
        });

        effect(() => {
            const qrNoti = this.paymentRealtimeService.qrOrderNotification();
            if (qrNoti) {
                const notiKey = qrNoti.id ||
                    (qrNoti.orderId ? `${qrNoti.orderId}_${qrNoti.type || 'Order'}` : null) ||
                    `${qrNoti.tableName || 'Table'}_${qrNoti.type || 'Noti'}_${Date.now()}`;

                const isProcessed = untracked(() => this.processedQrNotificationIds.has(notiKey));
                if (isProcessed) {
                    untracked(() => {
                        this.paymentRealtimeService.clearQrOrderNotification();
                    });
                    return;
                }

                const now = Date.now();
                const isTooSoon = untracked(() => now - this.lastNotificationTime < 300);
                if (isTooSoon) {
                    untracked(() => {
                        this.paymentRealtimeService.clearQrOrderNotification();
                    });
                    return;
                }

                untracked(() => {
                    this.processedQrNotificationIds.add(notiKey);
                    this.lastNotificationTime = now;
                });

                if (qrNoti.type === "CallWaiter") {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Khách gọi nhân viên!',
                        detail: `Bàn ${qrNoti.tableName} đang gọi nhân viên hỗ trợ.`,
                        life: 10000
                    });
                } else if (qrNoti.type === "CallPayment") {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Khách gọi thanh toán!',
                        detail: `Bàn ${qrNoti.tableName} yêu cầu thanh toán hóa đơn.`,
                        life: 10000
                    });
                } else {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Có QR Order mới!',
                        detail: `Có đơn ${qrNoti.serviceType === 'TakeAway' ? 'mang về' : qrNoti.serviceType === 'Delivery' ? 'giao hàng' : 'tại bàn'} mới qua QR (${qrNoti.orderCode})`,
                        life: 10000
                    });

                    // Reload tables to show the table in Reserved state and update counts
                    this.loadTables();
                    this.updateServiceTypeCounts();
                }

                // Clear the notification to prevent re-triggering (FE-01)
                untracked(() => {
                    this.paymentRealtimeService.clearQrOrderNotification();
                });
            }
        });

        // Register clean up hook on component destruction
        this.destroyRef.onDestroy(() => {
            for (const txId of this.joinedTransactionIds) {
                void this.paymentRealtimeService.leaveTransaction(txId);
            }
            this.joinedTransactionIds.clear();
            void this.stopSignalR();
        });
    }

    private setRealVh() {
        document.documentElement.style.setProperty('--real-vh', `${window.innerHeight}px`);
    }

    loadEInvoiceBranchConfigs() {
        this.eInvoiceBranchConfigsLoaded = true;
    }

    isCurrentBranchEInvoiceApplied(branchCode?: string): boolean {
        return false;
    }

    shouldUseKitchenFlow(): boolean {
        return !this.isOrderInLockedPeriod() && (this.requireKitchenSend() || this.enableKitchenPrint());
    }

    isOrderInLockedPeriod(): boolean {
        if (!this.invoiceLockEnabled() || !this.currentOrder) return false;
        if (this.currentOrder.BillId) return this.isBillLockedForEditing();
        const lockDate = this.getEffectiveLockDate();
        const orderDate = this.parseDateOnly(this.openTime);
        return !!lockDate && !!orderDate && orderDate.getTime() <= lockDate.getTime();
    }

    onKitchenOrSaveClick(): void {
        if (this.isOrderInLockedPeriod()) {
            void this.orderSave();
        } else {
            void this.orderSendKitchen();
        }
    }

    getKitchenSendButtonLabel(): string {
        if (this.isOrderInLockedPeriod()) return 'Lưu';
        if (this.requireKitchenSend() || this.enableKitchenPrint()) {
            return 'Gửi Bếp/Bar';
        }
        if (!this.currentOrder?.BillId) {
            return 'Mở bàn';
        }
        if (this.currentOrder?.ServiceType === 'TakeAway' || this.currentOrder?.ServiceType === 'Delivery' || this.currentOrder?.Status === 'WaitingConfirm') {
            return 'Lưu';
        }
        return 'Lưu';
    }

    isKitchenSendButtonVisible(): boolean {
        if (this.requireKitchenSend() || this.enableKitchenPrint()) {
            return true;
        }
        if (this.currentOrder?.Status === 'WaitingConfirm') {
            return true;
        }
        if (this.currentOrder?.BillId && this.currentOrder?.ServiceType === 'DineIn') {
            return false;
        }
        return true;
    }

    getKitchenSendLoadingTitle(): string {
        if (this.isOrderInLockedPeriod()) return 'Đang lưu đơn hàng...';
        if (this.requireKitchenSend() || this.enableKitchenPrint()) {
            return 'Đang gửi Bếp/Bar...';
        }
        if (!this.currentOrder?.BillId) {
            return 'Đang mở bàn & lưu đơn...';
        }
        return 'Đang lưu đơn hàng...';
    }

    getKitchenSendLoadingMessage(): string {
        if (this.isOrderInLockedPeriod()) return 'Hệ thống đang lưu thông tin đơn hàng, vui lòng chờ trong giây lát...';
        if (this.requireKitchenSend() || this.enableKitchenPrint()) {
            return 'Hệ thống đang lưu đơn và chuyển thông tin món đến Bếp/Bar, vui lòng chờ trong giây lát...';
        }
        return 'Hệ thống đang lưu thông tin đơn hàng, vui lòng chờ trong giây lát...';
    }

    actionSendOrderToKitchenBar(order: any) {
        if (this.isSendingKitchen()) return;
        this.isSendingKitchen.set(true);
        this.salesService.sendOrderToKitchenBar(order.OrderId, this.selectedBranchCode).subscribe({
            next: (res: any) => {
                this.isSendingKitchen.set(false);
                if (res?.Code === 200) {
                    this.showPrintDispatchResults(res);
                    if (this.enableKitchenPrint() && res.Data?.PrintSlips && res.Data.PrintSlips.length > 0) {
                        this.printKitchenSlips(res.Data.PrintSlips, order);
                    }
                    this.messageService.add({ severity: 'success', summary: 'Gửi bếp/bar', detail: 'Đã gửi bếp/bar thành công!' });
                    this.loadTables();
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Có lỗi xảy ra' });
                }
            },
            error: (err: any) => {
                this.isSendingKitchen.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi gửi bếp/bar' });
            }
        });
    }

    ngOnInit() {
        const visibilityRefresh = setInterval(() => {
            if (document.visibilityState === 'visible' && this.selectedBranchCode) {
                this.pollMenuVisibility();
            }
        }, 30000);
        this.destroyRef.onDestroy(() => clearInterval(visibilityRefresh));
        this.store.dispatch(SystemSettingsAction.loadAll());
        this.layoutService.setPosViewMode('floor-plan');
        this.layoutService.showPosSwitcher.set(true);
        this.destroyRef.onDestroy(() => {
            this.layoutService.showPosSwitcher.set(false);
        });
        this.loadEInvoiceBranchConfigs();
        this.promotionService.getByFilter({ Page: 1, Size: 1000 }).subscribe({
            next: (res: any) => {
                if (res?.Code === 200 && Array.isArray(res?.Data?.Content)) {
                    this.promotionsList.set(res.Data.Content);
                }
            }
        });
        // Set real viewport height for mobile browsers (avoids dvh inconsistencies)
        this.setRealVh();
        window.addEventListener('resize', () => this.setRealVh());
        window.addEventListener('orientationchange', () => {
            setTimeout(() => this.setRealVh(), 100);
        });
        // Load user permissions from store
        this.store.pipe(
            select(selectPermissions),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((perms) => {
            this.userPermissions.set(perms || []);
        });

        // Load current user and employee
        this.store.pipe(
            select(selectUser),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((user) => {
            this.currentUser.set(user);
        });

        this.store.pipe(
            select(selectEmployee),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((emp) => {
            this.currentEmployee.set(emp);
        });

        // Load system settings from store
        this.store.pipe(
            select(selectSystemSettings),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((configs) => {
            const setting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'sale-require-kitchen');
            if (setting) {
                const val = `${setting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.requireKitchenSend.set(isTrue && setting.IsActive);
            } else {
                this.requireKitchenSend.set(true);
            }

            const invoiceDateSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'sale-enable-invoice-date');
            if (invoiceDateSetting) {
                const val = `${invoiceDateSetting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.enableInvoiceDate.set(isTrue && invoiceDateSetting.IsActive);
            } else {
                this.enableInvoiceDate.set(true);
            }

            const kitchenPrintSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'sale-enable-kitchen-print');
            if (kitchenPrintSetting) {
                const val = `${kitchenPrintSetting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.enableKitchenPrint.set(isTrue && kitchenPrintSetting.IsActive);
            } else {
                this.enableKitchenPrint.set(true);
            }

            const printByAreaSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'kitchen-print-by-area');
            if (printByAreaSetting) {
                const val = `${printByAreaSetting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.kitchenPrintByArea.set(isTrue && printByAreaSetting.IsActive);
            } else {
                this.kitchenPrintByArea.set(true);
            }

            const defaultAreaSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'kitchen-default-area-id');
            if (defaultAreaSetting) {
                this.kitchenDefaultAreaId.set(`${defaultAreaSetting.Value ?? ''}`.trim());
            }

            const lockEnabledSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'invoice-lock-enabled');
            if (lockEnabledSetting) {
                const val = `${lockEnabledSetting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.invoiceLockEnabled.set(isTrue);
            } else {
                this.invoiceLockEnabled.set(false);
            }

            const lockDateSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'invoice-lock-date');
            if (lockDateSetting) {
                this.invoiceLockDate.set(`${lockDateSetting.Value ?? ''}`.trim());
            }

            const lockAutoSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'invoice-lock-auto');
            if (lockAutoSetting) {
                const val = `${lockAutoSetting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.invoiceLockAuto.set(isTrue);
            } else {
                this.invoiceLockAuto.set(true);
            }

            const lockDaysSetting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'invoice-lock-auto-days');
            if (lockDaysSetting) {
                const num = Number(lockDaysSetting.Value);
                this.invoiceLockAutoDays.set(!isNaN(num) ? Math.max(0, num) : 0);
            }
        });

        // Load user branches from store
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            this.branchUser.set(branches || []);
            // Sync initial loading once available branches are populated
            if (this.selectedBranchCode && (!this.activeFloor || this.floors.length === 0)) {
                this.loadFloors();
                this.loadCatalogData();
                this.updateServiceTypeCounts();
                this.loadOrderList();
                this.loadAvailableEmptyTables();
            }
        });

        // Sync with global branch selection
        this.store.pipe(
            select(selectSelectedBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            if (codes && codes.length > 0) {
                const item = codes[0];
                const newCode = typeof item === 'string' ? item : (item?.Code || '');
                if (newCode && this.selectedBranchCode !== newCode) {
                    this.selectedBranchCode = newCode;
                    this.onBranchChange();
                } else if (newCode) {
                    this.loadCatalogData();
                    this.loadOrderList();
                    this.loadAvailableEmptyTables();
                }
            }
        });

        // Load cancel reasons (Type 2 - Hủy Order)
        this.reasonService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            if (res?.Code === 200) {
                const raw = res?.Data?.Content || [];
                // Filter only Type 2 (Hủy Order) and active ones (Status === 1)
                const filtered = raw.filter((x: any) => x.Type === 2 && x.Status === 1);
                this.cancelReasons.set(filtered);
            }
        });

        // Debounced table reload to prevent API storm from rapid successive realtime events
        this._tableReloadTrigger$.pipe(
            debounceTime(300),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe(() => this.loadTables());

        // Subscribe to branch-wide sales events for realtime multi-device sync
        this.paymentRealtimeService.salesEvent$.pipe(
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((event: ISalesRealtimeEvent) => {
            if (!this.selectedBranchCode) return;
            this._handleBranchSalesEvent(event);
        });

        // Initial load of order list and tables if branch is set
        if (this.selectedBranchCode) {
            this.loadOrderList();
            this.loadAvailableEmptyTables();
            void this.paymentRealtimeService.updateBranchSubscription([this.selectedBranchCode]);
        }
    }

    onBranchChange() {
        this.menuVisibilityState = null;
        this.lastPeriodicMenuRefreshAt = Date.now();
        this.activeFloor = null;
        this.clearSelection();
        this.tables = [];
        this.loadFloors();
        this.loadCatalogData();
        this.updateServiceTypeCounts();
        this.loadOrderList();
        this.loadAvailableEmptyTables();

        // Kiểm tra ca làm việc khi đổi chi nhánh — nếu chi nhánh mới chưa có ca → redirect
        this.checkActiveShift(this.selectedBranchCode);

        // Dispatch selection to store to synchronize globally
        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (branchOpt) {
            this.store.dispatch(BranchFilterAction.setSelectedBranches({ codes: [branchOpt] }));
        }

        // Restart SignalR connection for the new branch
        void this.stopSignalR();
        void this.startSignalR();
        if (this.selectedBranchCode) {
            void this.paymentRealtimeService.updateBranchSubscription([this.selectedBranchCode]);
        }
    }

    checkActiveShift(branchCode: string) {
        if (!branchCode || this.userPermissions().includes('SALEROOM_CASHIER_ADMIN')) return;

        this.shiftService.getCurrentShift(branchCode).subscribe({
            next: (res: any) => {
                if (res.Code === 404 || !res.Data || res.Data.Status !== 1) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Chưa mở ca',
                        detail: 'Vui lòng mở ca làm việc trước khi thực hiện bán hàng!'
                    });
                    this.router.navigateByUrl('/catalogs/shift-session');
                } else if (res.Data.IsExpired) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hết thời gian ca',
                        detail: 'Ca làm việc đã hết hạn. Vui lòng gia hạn hoặc thực hiện kiểm kê đóng ca!'
                    });
                    this.router.navigateByUrl('/catalogs/shift-session');
                }
            },
            error: () => {
                this.router.navigateByUrl('/catalogs/shift-session');
            }
        });
    }

    loadCatalogData() {
        if (!this.selectedBranchCode) return;

        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (branchOpt && branchOpt.Id && this.allBranchTables.length === 0) {
            this.loadAllBranchTables(branchOpt.Id);
        }

        this.loadCatalogMenu();

        this.refreshEmployeesAndCustomers();

        // Load menu groups
        this.menuGroupService.getByFilter({ Page: 1, Size: 100 }).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.menuGroups = res.Data.Content || [];
            }
        });

        this.loadMenuItems();
    }

    private pollMenuVisibility() {
        if (this.menuVisibilityPollInFlight) return;
        const branchCode = this.selectedBranchCode;
        this.menuVisibilityPollInFlight = true;
        this.salesService.getMenuVisibilityState(branchCode).pipe(timeout(10000)).subscribe({
            next: (res: any) => {
                if (branchCode !== this.selectedBranchCode || res?.Code !== 200 || !res.Data) return;
                const changed = this.menuVisibilityState !== res.Data;
                this.menuVisibilityState = res.Data;
                if (changed || Date.now() - this.lastPeriodicMenuRefreshAt >= 300000) {
                    this.lastPeriodicMenuRefreshAt = Date.now();
                    this.loadCatalogMenu();
                    this.loadMenuItems();
                }
            },
            complete: () => { this.menuVisibilityPollInFlight = false; },
            error: () => { this.menuVisibilityPollInFlight = false; }
        });
    }

    private loadCatalogMenu() {
        if (!this.selectedBranchCode) return;
        const branchCode = this.selectedBranchCode;
        // Refresh only the menu, preserving order edits and search text.
        this.salesService.getMenuItemsFilter(this.selectedBranchCode).subscribe((res: any) => {
            if (branchCode !== this.selectedBranchCode) return;
            if (res?.Code === 200 && res.Data) {
                const content = (res.Data || []).filter((mi: any) => mi.IsAvailable !== false);
                this.menuItems = content.map((mi: any) => ({
                    ItemId: mi.Id || mi.id,
                    ItemCode: mi.Code || mi.code,
                    ItemName: mi.Name || mi.name,
                    UnitName: mi.UnitName || mi.unitName || '',
                    Quantity: 0,
                    Price: mi.SalePrice || mi.FixedTaxAmount || 0,
                    FixedTaxAmount: mi.FixedTaxAmount || 0,
                    VatRate: mi.TaxRatePercent || 0,
                    Amount: 0,
                    ServedQuantity: 0,
                    RemainingQuantity: 0,
                    Note: null,
                    Description: mi.Description,
                    ImageUrl: mi.ImageUrl,
                    OptionGroupsJson: mi.OptionGroupsJson,
                    IsAvailable: true
                }));
                this.filterMenuItems();
            }
        });

        // Load top used menu items
        this.salesService.getTopUsedMenuItems(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const content = (res.Data || []).filter((mi: any) => mi.IsAvailable !== false);
                this.topUsedMenuItems = content.map((mi: any) => ({
                    ItemId: mi.Id,
                    ItemCode: mi.Code,
                    ItemName: mi.Name,
                    UnitName: mi.UnitName || mi.unitName || '',
                    Quantity: 0,
                    DisplayPrice: mi.Amount || mi.SalePrice || 0,
                    Price: mi.SalePrice || 0,
                    FixedTaxAmount: mi.FixedTaxAmount || 0,
                    VatRate: mi.TaxRatePercent || 0,
                    Amount: 0,
                    ServedQuantity: 0,
                    RemainingQuantity: 0,
                    Note: null,
                    Description: mi.Description,
                    ImageUrl: mi.ImageUrl,
                    OptionGroupsJson: mi.OptionGroupsJson,
                    IsAvailable: true
                }));
            }
        });
    }

    filterMenuItems() {
        if (!this.searchQuery.trim()) {
            this.filteredMenuItems = [...this.menuItems];
        } else {
            const query = this.searchQuery.toLowerCase();
            this.filteredMenuItems = this.menuItems.filter(item =>
                item.ItemName.toLowerCase().includes(query) ||
                item.ItemCode.toLowerCase().includes(query)
            );
        }
    }

    loadFloors() {
        if (!this.selectedBranchCode) return;

        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (branchOpt && branchOpt.Id) {
            this.fetchFloorsForBranch(branchOpt.Id);
        } else {
            this.branchService.getByFilter({ Page: 1, Size: 100 }).subscribe((res: any) => {
                const content = res?.Data?.Content || [];
                const matched = content.find((b: any) => b.Code === this.selectedBranchCode);
                if (matched && matched.Id) {
                    this.fetchFloorsForBranch(matched.Id);
                }
            });
        }
    }

    loadAllBranchTables(branchId: string) {
        if (!branchId) return;
        this.diningTableService.getByFilter({ BranchId: branchId, Size: 1000, Page: 1, IsInactive: false }).subscribe({
            next: (res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const raw = Array.isArray(res.Data) ? res.Data : (res.Data?.Content || res.Data?.datas || []);
                    this.allBranchTables = raw.map((t: any) => ({
                        TableId: t.Id || t.TableId,
                        Id: t.Id || t.TableId,
                        TableCode: t.Code || t.TableCode || t.Name,
                        TableName: t.Name || t.TableName || t.Code,
                        FloorId: t.FloorPlanId || t.FloorId,
                        FloorName: t.FloorPlanName || t.FloorName || ''
                    }));
                }
            }
        });
    }

    fetchFloorsForBranch(branchId: string) {
        this.isLoadingTables.set(true);
        this.loadAllBranchTables(branchId);
        this.floorPlanService.getByFilter({ BranchId: branchId, Size: 1000, Page: 1 }).subscribe({
            next: (res: any) => {
                if (res?.Code === 200) {
                    this.floors = res.Data?.Content || [];
                    if (this.floors.length > 0) {
                        this.selectFloor(this.floors[0]);
                    } else {
                        this.activeFloor = null;
                        this.isLoadingTables.set(false);
                    }
                } else {
                    this.isLoadingTables.set(false);
                }
            },
            error: () => {
                this.isLoadingTables.set(false);
            }
        });
    }

    validateTherapyVoucherIfNeeded(): Promise<boolean> {
        if (!this.therapyVoucherCode || !this.therapyVoucherCode.trim()) {
            return Promise.resolve(true);
        }

        return new Promise((resolve) => {
            const code = this.therapyVoucherCode.trim();
            this.salesService.validateTherapyVoucher(code).subscribe({
                next: (res: any) => {
                    if (res?.Code === 200) {
                        resolve(true);
                    } else {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Lỗi mã phiếu',
                            detail: res?.Message || 'Mã phiếu không có trên AnPOS'
                        });
                        resolve(false);
                    }
                },
                error: () => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi mã phiếu',
                        detail: 'Mã phiếu không có trên AnPOS'
                    });
                    resolve(false);
                }
            });
        });
    }

    onValidateTherapyVoucher(code: string) {
        if (!code) {
            this.therapyVoucherCode = '';
            this.therapyServices = [];
            this.selectedTherapyServiceCode = '';
            this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: 'Đã xóa mã phiếu trị liệu' });
            return;
        }

        this.therapyVoucherCode = code.trim();
        // Gọi API kiểm tra tính hợp lệ của mã phiếu trị liệu
        this.salesService.validateTherapyVoucher(this.therapyVoucherCode).subscribe({
            next: (res: any) => {
                if (res?.Code === 200) {
                    this.therapyServices = res?.Data?.Services || [];
                    if (this.therapyServices.length > 0 && !this.selectedTherapyServiceCode) {
                        this.selectedTherapyServiceCode = this.therapyServices[0].ServiceCode;
                    }
                    const count = this.therapyServices.length;
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Thành công',
                        detail: count > 0 ? `Mã phiếu hợp lệ! Có ${count} dịch vụ.` : 'Mã phiếu trị liệu hợp lệ!'
                    });
                } else {
                    this.therapyServices = [];
                    this.selectedTherapyServiceCode = '';
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Mã phiếu không có trên AnPOS' });
                }
            },
            error: () => {
                this.therapyServices = [];
                this.selectedTherapyServiceCode = '';
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Mã phiếu không có trên AnPOS' });
            }
        });
    }

    selectFloor(floor: any) {
        if (this.activeFloor?.Id !== floor?.Id) {
            this.allTablesOfFloor = [];
            this.tables = [];
        }
        this.activeFloor = floor;
        this.restoreZoomForCurrentFloor();
        if (this.statusFilter() === 'TakeAway' || this.statusFilter() === 'Delivery') {
            this.statusFilter.set('All');
        }
        this.clearSelection();
        this.loadTables();
    }

    updateServiceTypeCounts() {
        if (!this.selectedBranchCode) return;
        this.salesService.getOrdersByServiceType(this.selectedBranchCode, 'TakeAway', 'WaitingService,Serving,WaitingPayment').subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.takeAwayCount.set(res.Data.Orders?.length || 0);
            } else {
                this.takeAwayCount.set(0);
            }
        });
        this.salesService.getOrdersByServiceType(this.selectedBranchCode, 'Delivery', 'WaitingService,Serving,WaitingPayment').subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.deliveryCount.set(res.Data.Orders?.length || 0);
            } else {
                this.deliveryCount.set(0);
            }
        });
        this.salesService.getOrdersByServiceType(this.selectedBranchCode, 'All', 'WaitingConfirm').subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.waitingConfirmCount.set(res.Data.Orders?.length || 0);
            } else {
                this.waitingConfirmCount.set(0);
            }
        });
    }

    setViewMode(mode: 'floor-plan' | 'order-list') {
        this.viewMode.set(mode);
        if (mode === 'order-list') {
            this.loadOrderList();
        }
    }

    loadOrderList() {
        if (!this.selectedBranchCode) return;
        this.isLoadingOrderList.set(true);
        this.salesService.getOrdersByServiceType(this.selectedBranchCode, 'All', 'WaitingService,Serving,WaitingPayment,Reserved,Checked,WaitingConfirm').subscribe({
            next: (res: any) => {
                this.isLoadingOrderList.set(false);
                if (res?.Code === 200 && res.Data) {
                    const rawOrders = res.Data.Orders || [];
                    rawOrders.forEach((o: any) => {
                        const tName = this.getOrderTableDisplayName(o);
                        if (tName) {
                            o.TableName = tName;
                        }
                    });
                    this.allActiveOrders = rawOrders;
                }
            },
            error: () => {
                this.isLoadingOrderList.set(false);
            }
        });
    }

    loadAvailableEmptyTables() {
        if (!this.selectedBranchCode) return;
        this.salesService.getAvailableTargetTables(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.availableEmptyTables = (res.Data || []).map((t: any) => ({
                    TableId: t.TableId || t.Id,
                    TableCode: t.TableCode || t.Code || t.Name,
                    TableName: t.TableName || t.Name || t.TableCode,
                    FloorId: t.FloorId || t.FloorPlanId,
                    FloorName: t.FloorName || '',
                    DisplayLabel: `${t.TableCode || t.TableName || t.Name}`
                }));
            }
        });
    }

    getAvailableTablesForCurrentOrder(): any[] {
        const tableMap = new Map<string, any>();

        // 1. Add all tables from allBranchTables
        (this.allBranchTables || []).forEach(t => {
            const id = String(t.TableId || t.Id || '').toLowerCase();
            if (id) {
                tableMap.set(id, {
                    TableId: t.TableId || t.Id,
                    Id: t.TableId || t.Id,
                    TableCode: t.TableCode || t.Code,
                    TableName: t.TableName || t.Name,
                    FloorId: t.FloorId || t.FloorPlanId,
                    FloorName: t.FloorName || t.FloorPlanName || ''
                });
            }
        });

        // 2. Add from allTablesOfFloor if not present
        (this.allTablesOfFloor || []).forEach(t => {
            const id = String(t.TableId || t.Id || '').toLowerCase();
            if (id && !tableMap.has(id)) {
                tableMap.set(id, {
                    TableId: t.TableId || t.Id,
                    Id: t.TableId || t.Id,
                    TableCode: t.TableCode || t.Code,
                    TableName: t.TableName || t.Name,
                    FloorId: t.FloorId || t.FloorPlanId,
                    FloorName: t.FloorName || ''
                });
            }
        });

        // 3. Add from availableEmptyTables if not present
        (this.availableEmptyTables || []).forEach(t => {
            const id = String(t.TableId || t.Id || '').toLowerCase();
            if (id && !tableMap.has(id)) {
                tableMap.set(id, {
                    TableId: t.TableId || t.Id,
                    Id: t.TableId || t.Id,
                    TableCode: t.TableCode || t.Code,
                    TableName: t.TableName || t.Name,
                    FloorId: t.FloorId || t.FloorPlanId,
                    FloorName: t.FloorName || ''
                });
            }
        });

        // 4. Ensure currentOrder's table is present
        const curTableId = this.currentOrder?.TableId;
        if (curTableId) {
            const curStr = String(curTableId).toLowerCase();
            if (!tableMap.has(curStr)) {
                const tCode = this.currentOrder.TableCode || '';
                const tName = this.currentOrder.TableName || tCode || 'Bàn hiện tại';
                const fName = this.currentOrder.FloorName || '';
                tableMap.set(curStr, {
                    TableId: curTableId,
                    Id: curTableId,
                    TableCode: tCode || tName,
                    TableName: tName,
                    FloorId: this.currentOrder.FloorId || null,
                    FloorName: fName
                });
            }
        }

        // 5. Determine occupied status for each table and build DisplayLabel
        const result: any[] = [];
        const currentBillId = this.currentOrder?.BillId || this.currentOrder?.OrderId || this.currentOrder?.Id;
        const currentOrderTableId = this.currentOrder?.TableId ? String(this.currentOrder.TableId).toLowerCase() : null;

        tableMap.forEach((table, key) => {
            const isCurrentTable = !!(currentOrderTableId && (key === currentOrderTableId || String(table.TableId).toLowerCase() === currentOrderTableId || String(table.Id).toLowerCase() === currentOrderTableId));

            // If it is the current order's table, do NOT show '(đã có người)'
            let isOccupied = false;
            if (!isCurrentTable) {
                isOccupied = (this.allActiveOrders || []).some((o: any) => {
                    if (o.Status === 'Cancelled' || o.Status === 'ApprovedCancelled' || o.Status === 'Paid' || o.Status === 'Completed' || o.Status === 'Merged') return false;
                    const oBillId = o.BillId || o.OrderId || o.Id;
                    if (currentBillId && oBillId && String(oBillId).toLowerCase() === String(currentBillId).toLowerCase()) {
                        return false;
                    }
                    const tId = o.TableId || (o.Tables && o.Tables[0]?.TableId);
                    return tId && String(tId).toLowerCase() === key;
                }) || (this.allTablesOfFloor || []).some(t => {
                    const tId = t.TableId || t.Id;
                    return tId && String(tId).toLowerCase() === key && t.Status && t.Status !== 'Empty';
                });
            }

            const tName = table.TableName || table.TableCode || '';
            const statusTag = isOccupied ? ' (đã có người)' : '';

            result.push({
                ...table,
                IsOccupied: isOccupied,
                DisplayLabel: `${tName}${statusTag}`
            });
        });

        return result.sort((a, b) => {
            return (a.TableName || a.TableCode || '').localeCompare(b.TableName || b.TableCode || '', 'vi', { numeric: true });
        });
    }

    onOrderTableIdChange(tableId: string | null) {
        if (!this.currentOrder) return;
        this.currentOrder.TableId = tableId;
        if (tableId) {
            const table = this.getAvailableTablesForCurrentOrder().find(t => (t.TableId === tableId || t.Id === tableId))
                || this.availableEmptyTables.find(t => (t.TableId === tableId || t.Id === tableId));
            if (table) {
                this.currentOrder.TableCode = table.TableCode || table.Code || '';
                this.currentOrder.TableName = table.TableName || table.Name || table.TableCode || '';
                this.currentOrder.FloorId = table.FloorId || table.FloorPlanId || null;
                this.currentOrder.FloorName = table.FloorName || null;
            }
        } else {
            this.currentOrder.TableCode = '';
            this.currentOrder.TableName = '';
            this.currentOrder.FloorId = null;
            this.currentOrder.FloorName = null;
        }
    }

    getOrderTableDisplayName(order: any): string {
        if (!order) return '';

        // 1. Direct TableName if valid
        if (order.TableName && order.TableName !== 'Không bàn' && order.TableName !== 'Tại bàn' && order.TableName !== 'Mang về' && order.TableName !== 'Giao hàng') {
            return order.TableName;
        }

        // 2. TableNames array
        if (order.TableNames && Array.isArray(order.TableNames) && order.TableNames.length > 0) {
            const validNames = order.TableNames.filter((n: any) => n && n !== 'Không bàn' && n !== 'Tại bàn');
            if (validNames.length > 0) return validNames.join(', ');
        }

        // 3. Tables array (from API detail or order list)
        if (order.Tables && Array.isArray(order.Tables) && order.Tables.length > 0) {
            const names = order.Tables
                .map((t: any) => t.TableName || t.Name || t.TableCode || t.Code)
                .filter((n: any) => n && n !== 'Không bàn' && n !== 'Tại bàn');
            if (names.length > 0) return names.join(', ');
        }

        // 4. Match TableId / TableIds against all known branch tables
        const tableId = order.TableId || (order.TableIds && order.TableIds[0]) || (order.Tables && order.Tables[0]?.TableId);
        if (tableId && tableId !== '00000000-0000-0000-0000-000000000000') {
            const found = (this.allBranchTables || []).find((t: any) => (t.TableId || t.Id) === tableId) ||
                (this.allTablesOfFloor || []).find((t: any) => (t.TableId || t.Id) === tableId) ||
                (this.tables || []).find((t: any) => (t.TableId || t.Id) === tableId);
            if (found) {
                return found.TableName || found.Name || found.TableCode || '';
            }
        }

        // 5. Direct TableCode
        if (order.TableCode && order.TableCode !== 'DRAFT') {
            return order.TableCode;
        }

        return '';
    }

    getOrderListCount(status: string): number {
        if (!this.allActiveOrders) return 0;
        switch (status) {
            case 'All':
                return this.allActiveOrders.length;
            case 'WaitingPayment':
                return this.allActiveOrders.filter(o => o.Status === 'WaitingPayment').length;
            case 'TakeAway':
                return this.allActiveOrders.filter(o => o.ServiceType === 'TakeAway').length;
            case 'Delivery':
                return this.allActiveOrders.filter(o => o.ServiceType === 'Delivery').length;
            case 'Reserved':
                return this.allActiveOrders.filter(o => o.Status === 'Reserved').length;
            default:
                return 0;
        }
    }

    getFilteredOrderCards(): any[] {
        let list = this.allActiveOrders || [];

        // Status filter (Dùng chung với màn sơ đồ bàn)
        const sf = this.statusFilter();
        if (sf && sf !== 'All') {
            if (sf === 'TakeAway') {
                list = list.filter(o => o.ServiceType === 'TakeAway');
            } else if (sf === 'Delivery') {
                list = list.filter(o => o.ServiceType === 'Delivery');
            } else if (sf === 'WaitingConfirm') {
                list = list.filter(o => o.Status === 'WaitingConfirm');
            } else if (sf === 'WaitingPayment') {
                list = list.filter(o => o.Status === 'WaitingPayment');
            } else if (sf === 'Serving') {
                list = list.filter(o => o.Status === 'Serving');
            } else if (sf === 'WaitingService') {
                list = list.filter(o => o.Status === 'WaitingService');
            } else if (sf === 'Checked') {
                list = list.filter(o => o.Status === 'Checked');
            } else if (sf === 'Reserved') {
                list = list.filter(o => o.Status === 'Reserved');
            } else if (sf === 'Empty') {
                list = [];
            } else {
                list = list.filter(o => o.Status === sf || o.ServiceType === sf);
            }
        }

        // Order Type dropdown filter
        const typeFilter = this.orderListTypeFilter();
        if (typeFilter && typeFilter !== 'All') {
            if (typeFilter === 'Reserved') {
                list = list.filter(o => o.Status === 'Reserved');
            } else if (typeFilter === 'HasTable') {
                list = list.filter(o => (o.TableName && o.TableName.trim().length > 0) || (o.TableId && o.TableId !== '00000000-0000-0000-0000-000000000000'));
            } else if (typeFilter === 'NoTable') {
                list = list.filter(o => (!o.TableName || o.TableName.trim().length === 0) && (!o.TableId || o.TableId === '00000000-0000-0000-0000-000000000000'));
            } else {
                list = list.filter(o => o.ServiceType === typeFilter);
            }
        }

        // Search Query
        const q = this.orderListSearchQuery().trim().toLowerCase();
        if (q) {
            list = list.filter(o => {
                const code = (o.OrderCode || o.BillCode || '').toLowerCase();
                const table = (o.TableName || '').toLowerCase();
                const cust = (o.CustomerName || '').toLowerCase();
                const phone = (o.PhoneNumber || '').toLowerCase();
                return code.includes(q) || table.includes(q) || cust.includes(q) || phone.includes(q);
            });
        }

        return list;
    }

    parseDateTimeMs(timeInput: any): number | null {
        if (!timeInput) return null;
        if (typeof timeInput === 'number') return isNaN(timeInput) ? null : timeInput;
        if (timeInput instanceof Date) return isNaN(timeInput.getTime()) ? null : timeInput.getTime();

        const str = String(timeInput).trim();
        if (!str || str.startsWith('0001-01-01')) return null;

        const now = this.clockService.time();

        // 1. Explicit timezone offset (+07:00, -05:00, Z)
        if (str.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(str)) {
            const parsed = new Date(str).getTime();
            if (!isNaN(parsed)) return parsed;
        }

        // 2. ISO format or string without timezone (e.g., "2026-09-03T16:50:00", "2026-09-03 16:50:00")
        // Treat as local wall-clock time
        const match = str.match(/^(\d{4})-(\d{2})-(\d{2})[T\s](\d{2}):(\d{2})(?::(\d{2}))?(?:\.(\d+))?/);
        if (match) {
            const year = parseInt(match[1], 10);
            const month = parseInt(match[2], 10) - 1;
            const day = parseInt(match[3], 10);
            const hours = parseInt(match[4], 10);
            const minutes = parseInt(match[5], 10);
            const seconds = match[6] ? parseInt(match[6], 10) : 0;
            const millis = match[7] ? parseInt(match[7].substring(0, 3).padEnd(3, '0'), 10) : 0;

            const localDate = new Date(year, month, day, hours, minutes, seconds, millis);
            const localMs = localDate.getTime();

            // Discrepancy check: If localMs is ~7h in the future (-25200s +- 1h), it was UTC stored without offset
            const diff = now - localMs;
            if (diff < -5 * 3600 * 1000 && diff > -9 * 3600 * 1000) {
                return Date.UTC(year, month, day, hours, minutes, seconds, millis);
            }
            return localMs;
        }

        const fallback = new Date(str).getTime();
        return isNaN(fallback) ? null : fallback;
    }

    getOrderDuration(orderOrTime: any): string {
        if (!orderOrTime) return "00:00:00";

        let startTimeStr: any = null;
        if (typeof orderOrTime === 'object') {
            startTimeStr = orderOrTime.OpenTime || orderOrTime.CreatedDate || orderOrTime.CreatedTime || orderOrTime.OrderTime || orderOrTime.CreatedAt || orderOrTime.InTime || orderOrTime.ServingTime;
        } else {
            startTimeStr = orderOrTime;
        }

        if (!startTimeStr) return "00:00:00";

        const startMs = this.parseDateTimeMs(startTimeStr);
        if (startMs === null) return "00:00:00";

        const now = this.clockService.time();
        const diffMs = Math.max(0, now - startMs);
        const totalSeconds = Math.floor(diffMs / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;

        const pad = (n: number) => n.toString().padStart(2, '0');
        return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
    }

    getOrderOpenTimeStr(order: any): string {
        if (!order) return '--:--';
        let startTimeStr: any = null;
        if (typeof order === 'object') {
            startTimeStr = order.OpenTime || order.CreatedDate || order.CreatedTime || order.OrderTime || order.CreatedAt || order.InTime || order.ServingTime;
        } else {
            startTimeStr = order;
        }
        if (!startTimeStr) return '--:--';
        const ms = this.parseDateTimeMs(startTimeStr);
        if (ms !== null) {
            const date = new Date(ms);
            const pad = (n: number) => n.toString().padStart(2, '0');
            return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
        }
        return '--:--';
    }

    actionAddNewOrder() {
        this.selectedTableIds.set([]);
        this.startDraftOrderEditing(null);
        this.currentOrder.TableId = null;
        this.currentOrder.TableName = '';
        this.currentOrder.TableCode = '';
        this.currentOrder.ServiceType = 'DineIn';
        this.isEditingOrder = false;
        this.openFoodDialog();
    }

    actionCheckItemsForOrder(order: any) {
        const orderId = order.OrderId || order.BillId || order.Id;
        if (!orderId) return;
        this.salesService.getCheckItems(orderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.checkBillDetail = res.Data;
                this.checkBillItems = (res.Data.Items || []).map((item: any) => {
                    const served = item.ServedQty !== undefined ? item.ServedQty : (item.ServedQuantity !== undefined ? item.ServedQuantity : item.Quantity);
                    const actual = item.IsChecked === 1
                        ? (item.ActualQuantity !== undefined && item.ActualQuantity !== null ? item.ActualQuantity : served)
                        : served;
                    const returned = item.IsChecked === 1
                        ? (item.ReturnedQuantity || 0)
                        : (actual < served ? (served - actual) : 0);
                    return {
                        ...item,
                        Quantity: item.Quantity,
                        SentQty: item.SentQty || 0,
                        ServedQty: served,
                        SoldOutQty: item.SoldOutQty || 0,
                        OriginalServedQty: served,
                        ServedQuantity: actual,
                        ReturnedQuantity: returned
                    };
                });
                this.selectedOrderId = orderId;
                this.currentOrder = {
                    ...order,
                    BillId: orderId,
                    Id: orderId,
                    BillCode: res.Data.BillCode || order.OrderCode || order.BillCode,
                    TableName: res.Data.TableName || order.TableName || '',
                    CustomerCount: res.Data.CustomerCount || order.CustomerCount || 1,
                    ServiceType: res.Data.ServiceType || order.ServiceType || 'DineIn'
                };
                this.checkDialogVisible = true;
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không tải được danh sách kiểm đồ' });
            }
        });
    }

    actionPrintTemporaryForOrder(order: any) {
        this.openEInvoicePrompt('card', order);
    }

    actionTransferTableForOrder(order: any) {
        const orderId = order.OrderId || order.BillId || order.Id;
        this.salesService.getAvailableTargetTables(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.availableTargetTables = res.Data || [];
                this.transferTargetTableIds = [];
                const tableCode = order.TableName || 'Không bàn';
                const orderCode = order.OrderCode || order.BillCode || '';
                this.transferNote = `Chuyển từ ${tableCode} ${orderCode}`.trim();
                this.transferSelectedFloorId = '';
                this.transferSearchText = '';
                this.transferFloorOptions = [{ Id: '', Name: 'Tất cả các tầng' }, ...this.floors];
                this.selectedOrderId = orderId;
                this.currentOrder = { ...order, BillId: orderId, Id: orderId };
                this.transferDialogVisible = true;
            }
        });
    }

    // ==========================================
    // Merge Order Flow (Danh sách order)
    // ==========================================
    mergeOrderDialogVisible: boolean = false;
    mergePrimaryOrder: any = null;
    mergeSelectedOrderIds = signal<Set<string>>(new Set<string>());
    mergeOrderTargetId: string = '';
    mergeOrderSearchText: string = '';
    mergeOrderKeptTableIds = signal<Set<string>>(new Set<string>());
    mergeOrderLoadedDetails = new Map<string, any>();
    mergeOrderPreviewData: {
        TotalCustomerCount: number;
        TotalAmount: number;
        MergedItems: any[];
        ParticipatingOrders: any[];
        ParticipatingTables: any[];
    } = {
            TotalCustomerCount: 0,
            TotalAmount: 0,
            MergedItems: [],
            ParticipatingOrders: [],
            ParticipatingTables: []
        };

    actionMergeOrderForOrder(order: any) {
        const orderId = order.OrderId || order.BillId || order.Id;
        this.salesService.getOrderById(orderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const fullOrder = res.Data;
                const tName = this.getOrderTableDisplayName(fullOrder) || this.getOrderTableDisplayName(order) || order.TableName;
                if (tName) {
                    fullOrder.TableName = tName;
                }
                this.mergePrimaryOrder = fullOrder;
                this.mergeOrderLoadedDetails.clear();
                this.mergeOrderLoadedDetails.set(orderId, fullOrder);
                this.mergeSelectedOrderIds.set(new Set<string>());
                this.mergeOrderTargetId = orderId;
                this.mergeOrderSearchText = '';
                this.mergeOrderKeptTableIds.set(new Set<string>());
                if (fullOrder.TableIds && fullOrder.TableIds.length > 0) {
                    this.mergeOrderKeptTableIds.set(new Set<string>(fullOrder.TableIds));
                } else if (fullOrder.TableId && fullOrder.TableId !== '00000000-0000-0000-0000-000000000000') {
                    this.mergeOrderKeptTableIds.set(new Set<string>([fullOrder.TableId]));
                }
                this.recalculateMergeOrderPreview();
                this.mergeOrderDialogVisible = true;
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không tải được thông tin đơn hàng' });
            }
        });
    }

    getAvailableOrdersToMerge(): any[] {
        if (!this.allActiveOrders || !this.mergePrimaryOrder) return [];
        const primaryId = this.mergePrimaryOrder.OrderId || this.mergePrimaryOrder.BillId || this.mergePrimaryOrder.Id;
        const q = this.mergeOrderSearchText.trim().toLowerCase();

        return this.allActiveOrders.filter(o => {
            const id = o.OrderId || o.BillId || o.Id;
            if (id === primaryId) return false;
            if (!['Serving', 'WaitingService', 'WaitingPayment', 'Reserved', 'WaitingConfirm'].includes(o.Status)) return false;

            if (q) {
                const code = (o.OrderCode || o.BillCode || '').toLowerCase();
                const table = (this.getOrderTableDisplayName(o) || o.TableName || '').toLowerCase();
                const cust = (o.CustomerName || '').toLowerCase();
                return code.includes(q) || table.includes(q) || cust.includes(q);
            }
            return true;
        });
    }

    toggleMergeOrderSelection(order: any) {
        const id = order.OrderId || order.BillId || order.Id;
        const current = new Set(this.mergeSelectedOrderIds());
        if (current.has(id)) {
            current.delete(id);
            this.mergeSelectedOrderIds.set(current);
            this.recalculateMergeOrderPreview();
        } else {
            current.add(id);
            this.mergeSelectedOrderIds.set(current);
            if (!this.mergeOrderLoadedDetails.has(id)) {
                this.salesService.getOrderById(id, this.selectedBranchCode).subscribe((res: any) => {
                    if (res?.Code === 200 && res.Data) {
                        const loaded = res.Data;
                        const tName = this.getOrderTableDisplayName(loaded) || this.getOrderTableDisplayName(order) || order.TableName;
                        if (tName) {
                            loaded.TableName = tName;
                        }
                        this.mergeOrderLoadedDetails.set(id, loaded);
                        this.recalculateMergeOrderPreview();
                    }
                });
            } else {
                this.recalculateMergeOrderPreview();
            }
        }
    }

    toggleMergeOrderKeptTable(tableId: string, event: any) {
        const checked = event.target.checked;
        const current = new Set(this.mergeOrderKeptTableIds());
        if (checked) {
            current.add(tableId);
        } else {
            current.delete(tableId);
        }
        this.mergeOrderKeptTableIds.set(current);
    }

    recalculateMergeOrderPreview() {
        if (!this.mergePrimaryOrder) return;
        const primaryId = this.mergePrimaryOrder.OrderId || this.mergePrimaryOrder.BillId || this.mergePrimaryOrder.Id;
        this.mergeOrderLoadedDetails.set(primaryId, this.mergePrimaryOrder);

        const participatingIds = [primaryId, ...Array.from(this.mergeSelectedOrderIds())];
        const participatingOrders = participatingIds
            .map(id => this.mergeOrderLoadedDetails.get(id))
            .filter(Boolean);

        let totalCustomerCount = 0;
        let totalAmount = 0;
        const allItemsMap = new Map<string, any>();
        const tablesMap = new Map<string, any>();

        participatingOrders.forEach(o => {
            totalCustomerCount += (o.CustomerCount || 1);
            totalAmount += (o.TotalAmount || 0);

            // Collect tables
            if (o.Tables && Array.isArray(o.Tables) && o.Tables.length > 0) {
                o.Tables.forEach((t: any) => {
                    const tId = t.TableId || t.Id;
                    const tName = t.TableName || t.Name || t.TableCode || this.getOrderTableDisplayName(o) || 'Bàn';
                    if (tId) {
                        tablesMap.set(tId, { TableId: tId, TableName: tName });
                    }
                });
            } else if (o.TableIds && o.TableIds.length > 0) {
                for (let i = 0; i < o.TableIds.length; i++) {
                    const tId = o.TableIds[i];
                    const tName = (o.TableNames && o.TableNames[i]) || this.getOrderTableDisplayName(o) || o.TableName || 'Bàn';
                    tablesMap.set(tId, { TableId: tId, TableName: tName });
                }
            } else if (o.TableId && o.TableId !== '00000000-0000-0000-0000-000000000000') {
                const tName = this.getOrderTableDisplayName(o) || o.TableName || 'Bàn';
                tablesMap.set(o.TableId, { TableId: o.TableId, TableName: tName });
            }

            // Collect items
            (o.Items || []).forEach((item: any) => {
                const key = item.ItemId || item.ItemCode || item.ItemName;
                const qty = item.Quantity || 0;
                const price = item.UnitPrice || item.Price || 0;
                if (allItemsMap.has(key)) {
                    const existing = allItemsMap.get(key);
                    existing.Quantity += qty;
                    existing.Amount += (qty * price);
                } else {
                    allItemsMap.set(key, {
                        ItemId: item.ItemId,
                        ItemCode: item.ItemCode,
                        ItemName: item.ItemName,
                        UnitName: item.UnitName || 'Đĩa',
                        UnitPrice: price,
                        Price: price,
                        Quantity: qty,
                        Amount: qty * price
                    });
                }
            });
        });

        const participatingTables = Array.from(tablesMap.values());

        const currentKept = new Set(this.mergeOrderKeptTableIds());
        participatingTables.forEach(t => {
            if (!currentKept.has(t.TableId)) {
                currentKept.add(t.TableId);
            }
        });
        this.mergeOrderKeptTableIds.set(currentKept);

        if (!participatingIds.includes(this.mergeOrderTargetId)) {
            this.mergeOrderTargetId = primaryId;
        }

        this.mergeOrderPreviewData = {
            TotalCustomerCount: totalCustomerCount,
            TotalAmount: totalAmount,
            MergedItems: Array.from(allItemsMap.values()),
            ParticipatingOrders: participatingOrders,
            ParticipatingTables: participatingTables
        };
    }

    confirmMergeOrder() {
        if (!this.mergeOrderTargetId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn hóa đơn đích nhận gộp!' });
            return;
        }

        const selectedIds = Array.from(this.mergeSelectedOrderIds());
        if (selectedIds.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một order để gộp!' });
            return;
        }

        const primaryId = this.mergePrimaryOrder.OrderId || this.mergePrimaryOrder.BillId || this.mergePrimaryOrder.Id;
        const allParticipatingIds = [primaryId, ...selectedIds];

        const targetOrderId = this.mergeOrderTargetId;
        const sourceOrderIds = allParticipatingIds.filter(id => id !== targetOrderId);

        const keptTableIds = Array.from(this.mergeOrderKeptTableIds());

        this.salesService.mergeOrders(targetOrderId, sourceOrderIds, keptTableIds, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Gộp order thành công!' });
                this.mergeOrderDialogVisible = false;
                this.loadTables();
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Gộp order thất bại' });
            }
        });
    }

    getServiceTypeLabel(type: string): string {
        switch (type) {
            case 'DineIn': return 'Tại bàn';
            case 'TakeAway': return 'Mang về';
            case 'Delivery': return 'Giao hàng';
            case 'Reserved': return 'Đặt trước';
            default: return 'Tại bàn';
        }
    }

    openBillDetail(order: any) {
        if (!order) return;
        const orderId = order.OrderId || order.BillId || order.Id;
        if (!orderId) return;

        this.salesService.getOrderById(orderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const orderData = res.Data;
                const tableId = (orderData.Tables && orderData.Tables.length > 0)
                    ? orderData.Tables[0].TableId
                    : (orderData.TableId || order.TableId || (order.Tables && order.Tables[0]?.TableId) || null);

                const tableCode = (orderData.Tables && orderData.Tables.length > 0)
                    ? (orderData.Tables[0].TableCode || orderData.Tables[0].TableName)
                    : (order.TableCode || (order.Tables && order.Tables[0]?.TableCode) || null);

                const tableName = (orderData.Tables && orderData.Tables.length > 0)
                    ? (orderData.Tables[0].TableName || orderData.Tables[0].TableCode)
                    : (order.TableName || order.TableCode || (order.Tables && (order.Tables[0]?.TableName || order.Tables[0]?.TableCode)) || null);

                const serviceType = orderData.ServiceType || order.ServiceType || (tableId ? 'DineIn' : 'TakeAway');

                const bill = {
                    ...orderData,
                    BillId: orderData.Id || orderData.OrderId || orderData.BillId,
                    BillCode: orderData.OrderCode || orderData.BillCode,
                    ServiceType: serviceType,
                    TableId: tableId,
                    TableCode: tableCode,
                    TableName: tableName || (serviceType === 'DineIn' ? 'Tại bàn' : (serviceType === 'TakeAway' ? 'Mang về' : (serviceType === 'Delivery' ? 'Giao hàng' : ''))),
                    FloorId: (orderData.Tables && orderData.Tables.length > 0) ? orderData.Tables[0].FloorId : (orderData.FloorId || order.FloorId || null),
                    FloorName: orderData.FloorName || order.FloorName || null,
                    Items: (orderData.Items || orderData.OrderItems || []).map((item: any) => ({
                        ...item,
                        Id: item.Id,
                        ItemId: item.ItemId,
                        ItemCode: item.ItemCode,
                        ItemName: item.ItemName,
                        UnitName: item.UnitName || 'Đĩa',
                        Quantity: item.Quantity,
                        Price: item.UnitPrice || item.Price || 0,
                        UnitPrice: item.UnitPrice || item.Price || 0,
                        Amount: item.Amount,
                        DiscountRate: item.DiscountRate || 0,
                        DiscountAmount: item.DiscountAmount || 0,
                        VatRate: item.VatRate || 0,
                        VatAmount: item.VatAmount || 0,
                        TotalAmount: item.TotalAmount || 0,
                        Note: item.Note,
                        IsSentToKitchen: item.IsSentToKitchen,
                        SentQty: item.SentQty || 0,
                        ServedQty: item.ServedQty || 0,
                        SoldOutQty: item.SoldOutQty || 0,
                        RemainingQty: item.RemainingQty || 0,
                        KitchenStatus: item.KitchenStatus || 'NotSent'
                    }))
                };
                this.selectedOrderId = orderId;
                this.startOrderEditing(bill);
                if (this.isMobile()) {
                    this.openFoodDialog();
                }
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không thể lấy thông tin chi tiết đơn hàng!' });
            }
        });
    }

    actionSplitOrderForOrder(order: any) {
        const orderId = order.OrderId || order.BillId || order.Id;
        if (!orderId) return;
        this.openSplitDialog(orderId, order);
    }

    selectedActionOrder: any = null;

    openCardActions(event: MouseEvent, order: any, popover: any) {
        if (event) {
            event.stopPropagation();
        }
        this.selectedActionOrder = order;
        popover.toggle(event);
    }

    expandedOrderCardIds = signal<Set<string>>(new Set<string>());

    toggleCardActions(order: any, event?: any) {
        if (event) event.stopPropagation();
        const id = String(order.OrderId || order.BillId || order.Id || order.OrderCode || '');
        const current = new Set(this.expandedOrderCardIds());
        if (current.has(id)) {
            current.delete(id);
        } else {
            current.add(id);
        }
        this.expandedOrderCardIds.set(current);
    }

    isCardActionsExpanded(order: any): boolean {
        const id = String(order.OrderId || order.BillId || order.Id || order.OrderCode || '');
        return this.expandedOrderCardIds().has(id);
    }

    getFilteredServiceTypeOrders(): any[] {
        return this.serviceTypeOrders;
    }

    trackByTableId(_index: number, table: any): string {
        return table?.TableId || table?.Id || String(_index);
    }

    trackByOrderId(_index: number, order: any): string {
        return order?.OrderId || order?.BillId || order?.Id || String(_index);
    }

    trackByItemId(_index: number, item: any): string {
        return item?.ItemId || item?.Id || String(_index);
    }

    loadTables() {
        const filter = this.statusFilter();

        // 1. If there is an active floor, update the floor tables and their status counts
        if (this.activeFloor) {
            // Only show skeleton loader when tables are completely empty (first load / floor change)
            if (!this.tables || this.tables.length === 0) {
                this.isLoadingTables.set(true);
            }
            this.salesService.getTableMap(this.selectedBranchCode, this.activeFloor.Id, 'All').subscribe((res: any) => {
                this.isLoadingTables.set(false);
                if (res?.Code === 200 && res.Data) {
                    this.allTablesOfFloor = res.Data.Tables || [];

                    // Calculate counts
                    this.emptyCount.set(this.allTablesOfFloor.filter(t => !t.Status || t.Status === 'Empty').length);
                    this.servingCount.set(this.allTablesOfFloor.filter(t => t.Status === 'Serving').length);
                    this.reservedCount.set(this.allTablesOfFloor.filter(t => t.Status === 'Reserved').length);
                    this.waitingPaymentCount.set(this.allTablesOfFloor.filter(t => t.Status === 'WaitingPayment').length);

                    // Set visible tables only if not showing TakeAway/Delivery/WaitingConfirm orders
                    if (filter !== 'TakeAway' && filter !== 'Delivery' && filter !== 'WaitingConfirm') {
                        this.tables = this.allTablesOfFloor.filter(t => filter === 'All' || t.Status === filter);
                    }

                    // Read items directly from CurrentBills (already returned by getTableMap - no extra API calls needed!)
                    this.allTablesOfFloor.forEach((t: any) => {
                        if (t.Status && t.Status !== 'Empty') {
                            const bills = t.CurrentBills || [];
                            let items: any[] = [];
                            for (const bill of bills) {
                                const billItems = (bill.Items || bill.OrderItems || bill.Details || []).filter((i: any) => i && i.IsCancelled !== 1 && i.isCancelled !== 1 && i.KitchenStatus !== 'Cancelled');
                                items = items.concat(billItems);
                            }
                            console.log(`[TableMap] Table ${t.TableCode} (${t.Status}): CurrentBills=`, bills.length, 'items=', items.length, bills[0]);
                            if (items.length > 0) {
                                this.tableActiveItemsMap.set(t.TableId, items);
                                t.LoadedItems = items;
                            } else {
                                // If CurrentBills has no items, fallback to getActiveOrdersByTable
                                this.salesService.getActiveOrdersByTable(t.TableId, this.selectedBranchCode).subscribe((aRes: any) => {
                                    if (aRes?.Code === 200 && aRes.Data) {
                                        const aItems = this.extractItemsFromResponseData(aRes.Data);
                                        console.log(`[Fallback] Table ${t.TableCode}: fallback items=`, aItems.length, aRes.Data);
                                        if (aItems.length > 0) {
                                            this.tableActiveItemsMap.set(t.TableId, aItems);
                                            t.LoadedItems = aItems;
                                            this.tables = [...this.tables];
                                        }
                                    }
                                });
                            }
                        }
                    });
                    // Trigger UI refresh
                    if (filter !== 'TakeAway' && filter !== 'Delivery' && filter !== 'WaitingConfirm') {
                        this.tables = [...this.tables];
                    }
                } else {
                    this.allTablesOfFloor = [];
                    if (filter !== 'TakeAway' && filter !== 'Delivery' && filter !== 'WaitingConfirm') {
                        this.tables = [];
                    }
                    this.emptyCount.set(0);
                    this.servingCount.set(0);
                    this.reservedCount.set(0);
                    this.waitingPaymentCount.set(0);
                }
            });
        } else {
            this.tables = [];
            this.allTablesOfFloor = [];
            this.emptyCount.set(0);
            this.servingCount.set(0);
            this.reservedCount.set(0);
            this.waitingPaymentCount.set(0);
        }

        // 2. If the filter is TakeAway, Delivery or WaitingConfirm, load the corresponding orders list
        if (filter === 'TakeAway' || filter === 'Delivery' || filter === 'WaitingConfirm') {
            const queryStatuses = filter === 'WaitingConfirm' ? 'WaitingConfirm' : 'WaitingService,Serving,WaitingPayment';
            const serviceType = filter === 'WaitingConfirm' ? 'All' : filter;
            this.salesService.getOrdersByServiceType(this.selectedBranchCode, serviceType, queryStatuses).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    this.serviceTypeOrders = res.Data.Orders || [];
                    if (filter === 'TakeAway') {
                        this.takeAwayCount.set(this.serviceTypeOrders.length);
                    } else if (filter === 'Delivery') {
                        this.deliveryCount.set(this.serviceTypeOrders.length);
                    } else {
                        this.waitingConfirmCount.set(this.serviceTypeOrders.length);
                    }
                } else {
                    this.serviceTypeOrders = [];
                    if (filter === 'TakeAway') {
                        this.takeAwayCount.set(0);
                    } else if (filter === 'Delivery') {
                        this.deliveryCount.set(0);
                    } else {
                        this.waitingConfirmCount.set(0);
                    }
                }
            });
        }

        // 3. Synchronize active orders, counters and transaction rooms in one request
        this.syncAllActiveTransactions();
    }

    syncAllActiveTransactions() {
        if (!this.selectedBranchCode) return;

        this.salesService.getOrdersByServiceType(this.selectedBranchCode, 'All', 'WaitingService,Serving,WaitingPayment,Reserved,Checked,WaitingConfirm').subscribe({
            next: (res: any) => {
                const activeTxIds = new Set<string>();
                const orders = res?.Data?.Orders || [];

                orders.forEach((o: any) => {
                    const tName = this.getOrderTableDisplayName(o);
                    if (tName) {
                        o.TableName = tName;
                    }
                });

                // Store all active orders for background matching and Order List
                this.allActiveOrders = orders;
                const liveStatuses = new Set(['WaitingService', 'Serving', 'WaitingPayment']);
                this.takeAwayCount.set(orders.filter((o: any) => o.ServiceType === 'TakeAway' && liveStatuses.has(o.Status)).length);
                this.deliveryCount.set(orders.filter((o: any) => o.ServiceType === 'Delivery' && liveStatuses.has(o.Status)).length);
                this.waitingConfirmCount.set(orders.filter((o: any) => o.Status === 'WaitingConfirm').length);

                const dineInOrders = orders.filter((o: any) => o.ServiceType === 'DineIn' || (!o.ServiceType && (o.TableId || o.TableName)));

                // Automatically map active DineIn order items to floor tables so services display immediately on page load
                if (dineInOrders.length > 0 && this.tables.length > 0) {
                    dineInOrders.forEach((o: any) => {
                        const oItems = o.Items || o.OrderItems || o.Details || [];
                        const tId = o.TableId || (o.Tables && o.Tables[0]?.TableId);
                        const tName = o.TableName || (o.Tables && o.Tables[0]?.TableName);
                        const tCode = o.TableCode || (o.Tables && o.Tables[0]?.TableCode);

                        const matchingTable = this.tables.find(t =>
                            (tId && t.TableId === tId) ||
                            (tName && (t.TableName === tName || t.TableCode === tName)) ||
                            (tCode && (t.TableCode === tCode || t.TableName === tCode))
                        );

                        if (matchingTable && oItems.length > 0) {
                            matchingTable.LoadedItems = oItems;
                            this.tableActiveItemsMap.set(matchingTable.TableId, oItems);
                        }
                    });
                    this.tables = [...this.tables];
                }

                this.allActiveOrders.forEach((o: any) => {
                    const txId = o.OrderCode || o.BillCode;
                    if (txId && txId !== 'DRAFT') {
                        activeTxIds.add(txId);
                    }
                });

                // Join new transactions
                for (const txId of activeTxIds) {
                    if (!this.joinedTransactionIds.has(txId)) {
                        this.paymentRealtimeService.initializeConnection().then(() => {
                            return this.paymentRealtimeService.joinTransaction(txId);
                        }).then(() => {
                            this.joinedTransactionIds.add(txId);
                        }).catch(err => {
                            console.warn('Failed to join transaction room in background:', txId, err);
                        });
                    }
                }

                // Leave transaction rooms that are no longer active
                for (const txId of Array.from(this.joinedTransactionIds)) {
                    if (!activeTxIds.has(txId)) {
                        this.paymentRealtimeService.leaveTransaction(txId).then(() => {
                            this.joinedTransactionIds.delete(txId);
                        }).catch(err => {
                            console.warn('Failed to leave transaction room in background:', txId, err);
                        });
                    }
                }
            },
            error: (err) => {
                console.error('Failed to sync active transactions for branch:', err);
            }
        });
    }

    onStatusFilterChange(filterValue: string) {
        this.statusFilter.set(filterValue);
        this.clearSelection();
        this.loadTables();
    }

    getStatusSeverity(value: string): 'primary' | 'secondary' | 'success' | 'info' | 'warn' | 'help' | 'danger' | 'contrast' {
        switch (value) {
            case 'Empty': return 'info';           // PrimeNG info (Blue)
            case 'Serving': return 'success';        // PrimeNG success (Green)
            case 'Reserved': return 'help';           // PrimeNG help (Purple)
            case 'WaitingPayment': return 'warn';           // PrimeNG warn (Amber/Orange)
            case 'WaitingConfirm': return 'danger';         // PrimeNG danger (Red)
            case 'TakeAway': return 'success';        // PrimeNG success (Green)
            case 'Delivery': return 'info';           // PrimeNG info (Blue)
            default: return 'secondary';
        }
    }

    getBadgeSeverity(value: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        switch (value) {
            case 'Empty': return 'info';
            case 'Serving': return 'danger';
            case 'Reserved': return 'secondary';
            case 'WaitingPayment': return 'warn';
            case 'WaitingConfirm': return 'danger';
            case 'TakeAway': return 'success';
            case 'Delivery': return 'contrast';
            default: return 'secondary';
        }
    }

    getFilterBadgeValue(value: string): string | undefined {
        let count = 0;
        switch (value) {
            case 'Empty': count = this.emptyCount(); break;
            case 'Serving': count = this.servingCount(); break;
            case 'Reserved': count = this.reservedCount(); break;
            case 'WaitingPayment': count = this.waitingPaymentCount(); break;
            case 'TakeAway': count = this.takeAwayCount(); break;
            case 'Delivery': count = this.deliveryCount(); break;
            case 'WaitingConfirm': count = this.waitingConfirmCount(); break;
        }
        return count > 0 ? count.toString() : undefined;
    }

    // Toggle Table Selection on Canvas
    toggleTableSelection(table: any) {
        const currentSelected = [...this.selectedTableIds()];
        const idx = currentSelected.indexOf(table.TableId);
        if (idx > -1) {
            currentSelected.splice(idx, 1);
        } else {
            currentSelected.push(table.TableId);
        }
        this.selectedTableIds.set(currentSelected);
    }

    onTableClick(table: any, event?: MouseEvent) {
        if (!table) return;

        // Multi-select with Ctrl / Cmd / Shift key
        if (event && (event.ctrlKey || event.metaKey || event.shiftKey)) {
            const currentSelected = new Set(this.selectedTableIds());
            if (currentSelected.has(table.TableId)) {
                currentSelected.delete(table.TableId);
            } else {
                currentSelected.add(table.TableId);
            }
            this.selectedTableIds.set(Array.from(currentSelected));
            return;
        }

        // Nếu bàn này đã được chọn trước đó và chỉ đang chọn 1 bàn -> Click lần 2: Mở popup (Chọn món hoặc Chi tiết đơn)
        if (this.isTableSelected(table.TableId) && this.selectedTableIds().length === 1) {
            this.openTableDetails(table);
        } else {
            // Click lần 1: Chỉ chọn bàn
            this.selectedTableIds.set([table.TableId]);
            this.selectedOrderId = null;
            this.activeOrdersOfSelectedTable = table.CurrentBills || [];
        }
    }

    openTableDetails(table: any) {
        if (!table) return;
        this.selectedTableIds.set([table.TableId]);

        if (table.Status === 'Empty') {
            this.startDraftOrderEditing(table);
            this.isEditingOrder = false;
            this.openFoodDialog();
            return;
        }

        if (table.CurrentBills && table.CurrentBills.length > 0) {
            if (table.CurrentBills.length === 1) {
                const billId = table.CurrentBills[0].BillId;
                this.selectedOrderId = billId;
                this.salesService.getOrderById(billId, this.selectedBranchCode).subscribe((res: any) => {
                    if (res?.Code === 200 && res.Data) {
                        const bill = res.Data;
                        bill.BillId = bill.Id;
                        bill.TableId = table.TableId;
                        bill.TableName = bill.TableName || table.TableName;
                        this.startOrderEditing(bill);
                    } else {
                        const fallbackBill = { ...table.CurrentBills[0] };
                        fallbackBill.TableId = table.TableId;
                        fallbackBill.TableName = fallbackBill.TableName || table.TableName;
                        this.startOrderEditing(fallbackBill);
                    }
                });
            } else {
                this.activeOrdersOfSelectedTable = table.CurrentBills;
                this.selectedOrderId = null;
                this.isEditingOrder = false;
                this.isOrderSelectPopupOpen = true;
            }
        } else {
            this.salesService.getActiveOrdersByTable(table.TableId, this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const rawOrders = Array.isArray(res.Data) ? res.Data : (res.Data.Orders || [res.Data]);
                    const activeBill = rawOrders.length > 0 ? rawOrders[0] : null;
                    const items = this.extractItemsFromResponseData(res.Data);
                    this.tableActiveItemsMap.set(table.TableId, items);
                    table.LoadedItems = items;
                    this.tables = [...this.tables];

                    if (activeBill && (activeBill.BillId || activeBill.OrderId || activeBill.Id)) {
                        this.startOrderEditing(activeBill);
                    } else if (table.LoadedItems && table.LoadedItems.length > 0) {
                        this.startOrderEditing({ TableId: table.TableId, TableName: table.TableName, Items: table.LoadedItems });
                    } else {
                        this.startDraftOrderEditing(table);
                        this.isEditingOrder = false;
                        this.openFoodDialog();
                    }
                } else {
                    this.startDraftOrderEditing(table);
                    this.isEditingOrder = false;
                    this.openFoodDialog();
                }
            });
        }
    }

    onTableDoubleClick(table: any) {
        if (!table) return;
        this.openTableDetails(table);
    }

    isTableSelected(tableId: string): boolean {
        return this.selectedTableIds().includes(tableId);
    }

    clearSelection() {
        this.selectedTableIds.set([]);
        this.selectedOrderId = null;
        this.activeOrdersOfSelectedTable = [];
        this.selectedPromotionCode = null;
        this.therapyVoucherCode = '';
        this.appliedOrderLevelPromoDiscount = 0;
        this.appliedOrderLevelManualDiscount = 0;
        this.orderNote = '';
        this.openTime = new Date();
        this.isEditingOrder = false;
    }

    formatLocalIso(date: Date | string | null): string {
        if (!date) return new Date().toISOString();
        const d = date instanceof Date ? date : new Date(date);
        if (isNaN(d.getTime())) return new Date().toISOString();
        const pad = (n: number) => n < 10 ? '0' + n : String(n);
        return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
    }

    // Colors matching user reference image (Teal/Cyan for empty tables, soft red/pink for serving)
    getStatusClasses(table: any): string {
        const isSel = this.isTableSelected(table.TableId);
        const selClass = isSel
            ? 'ring-2 ring-primary ring-offset-1 shadow-lg'
            : '';

        switch (table.Status) {
            case 'Serving': return `${selClass} table-status-serving`;
            case 'WaitingService': return `${selClass} table-status-waiting-service`;
            case 'WaitingPayment': return `${selClass} table-status-waiting-payment`;
            case 'Reserved': return `${selClass} table-status-reserved`;
            case 'Checked': return `${selClass} table-status-checked`;
            default: return `${selClass} table-status-empty`;
        }
    }

    getStatusFilterButtonClass(val: string): string {
        const isSelected = this.statusFilter() === val;
        switch (val) {
            case 'Empty':
                return isSelected
                    ? 'bg-blue-600 text-white border-blue-700 shadow-sm font-extrabold'
                    : 'bg-blue-50/90 text-blue-800 border-blue-300 hover:bg-blue-100 dark:bg-blue-950/40 dark:text-blue-200 dark:border-blue-800';
            case 'Serving':
                return isSelected
                    ? 'bg-emerald-600 text-white border-emerald-700 shadow-sm font-extrabold'
                    : 'bg-emerald-50/90 text-emerald-800 border-emerald-400 hover:bg-emerald-100 dark:bg-emerald-950/40 dark:text-emerald-200 dark:border-emerald-800';
            case 'Reserved':
                return isSelected
                    ? 'bg-purple-600 text-white border-purple-700 shadow-sm font-extrabold'
                    : 'bg-purple-50/90 text-purple-800 border-purple-300 hover:bg-purple-100 dark:bg-purple-950/40 dark:text-purple-200 dark:border-purple-800';
            case 'WaitingPayment':
                return isSelected
                    ? 'bg-amber-500 text-white border-amber-600 shadow-sm font-extrabold'
                    : 'bg-amber-50/90 text-amber-800 border-amber-300 hover:bg-amber-100 dark:bg-amber-950/40 dark:text-amber-200 dark:border-amber-800';
            case 'WaitingConfirm':
                return isSelected
                    ? 'bg-red-600 text-white border-red-700 shadow-sm font-extrabold'
                    : 'bg-red-50/90 text-red-700 border-red-300 hover:bg-red-100 dark:bg-red-950/40 dark:text-red-200 dark:border-red-800';
            case 'TakeAway':
                return isSelected
                    ? 'bg-green-600 text-white border-green-700 shadow-sm font-extrabold'
                    : 'bg-green-50/90 text-green-800 border-green-300 hover:bg-green-100 dark:bg-green-950/40 dark:text-green-200 dark:border-green-800';
            case 'Delivery':
                return isSelected
                    ? 'bg-sky-600 text-white border-sky-700 shadow-sm font-extrabold'
                    : 'bg-sky-50/90 text-sky-800 border-sky-300 hover:bg-sky-100 dark:bg-sky-950/40 dark:text-sky-200 dark:border-sky-800';
            default: // All
                return isSelected
                    ? 'bg-surface-800 text-white border-surface-900 dark:bg-surface-100 dark:text-surface-900 shadow-sm font-extrabold'
                    : 'bg-surface-100 text-surface-700 border-surface-200 hover:bg-surface-200 dark:bg-surface-800 dark:text-surface-300 dark:border-surface-700';
        }
    }

    getFilterBadgeClass(val: string): string {
        const isSelected = this.statusFilter() === val;
        if (isSelected) {
            return 'bg-white text-surface-900 font-extrabold';
        }
        switch (val) {
            case 'Empty': return 'bg-blue-600 text-white font-bold';
            case 'Serving': return 'bg-emerald-600 text-white font-bold';
            case 'Reserved': return 'bg-purple-600 text-white font-bold';
            case 'WaitingPayment': return 'bg-amber-600 text-white font-bold';
            case 'WaitingConfirm': return 'bg-red-600 text-white font-bold animate-pulse';
            case 'TakeAway': return 'bg-green-600 text-white font-bold';
            case 'Delivery': return 'bg-sky-600 text-white font-bold';
            default: return 'bg-red-500 text-white font-bold';
        }
    }



    getIconBadgeClasses(table: any): string {
        switch (table.Status) {
            case 'Serving': return 'icon-badge-serving';
            case 'WaitingService': return 'icon-badge-waiting-service';
            case 'WaitingPayment': return 'icon-badge-waiting-payment';
            case 'Reserved': return 'icon-badge-reserved';
            case 'Checked': return 'icon-badge-checked';
            default: return 'icon-badge-empty';
        }
    }

    /** Helper bóc tách danh sách món ăn từ bất kỳ cấu trúc API response nào */
    extractItemsFromResponseData(data: any): any[] {
        if (!data) return [];
        const isCancelled = (i: any) => i && (i.IsCancelled === 1 || i.isCancelled === 1 || i.KitchenStatus === 'Cancelled');

        if (Array.isArray(data)) {
            if (data.length === 0) return [];
            const first = data[0];
            if (first && (first.ItemName || first.MenuItemName || first.ItemId || first.UnitName)) {
                return data.filter(i => !isCancelled(i));
            }
            let result: any[] = [];
            for (const order of data) {
                const items = order.Items || order.OrderItems || order.Details || order.OrderDetails || order.Services || [];
                result = result.concat(items.filter((i: any) => !isCancelled(i)));
            }
            return result;
        }

        if (typeof data === 'object') {
            if (data.Items && Array.isArray(data.Items) && data.Items.length > 0) {
                return data.Items.filter((i: any) => !isCancelled(i));
            }
            if (data.OrderItems && Array.isArray(data.OrderItems) && data.OrderItems.length > 0) {
                return data.OrderItems.filter((i: any) => !isCancelled(i));
            }
            if (data.Details && Array.isArray(data.Details) && data.Details.length > 0) {
                return data.Details.filter((i: any) => !isCancelled(i));
            }
            const rawOrders = data.Orders || data.ActiveOrders || data.Content || [];
            if (Array.isArray(rawOrders) && rawOrders.length > 0) {
                return this.extractItemsFromResponseData(rawOrders);
            }
        }

        return [];
    }

    /** Lấy tối đa 3 dịch vụ / món ăn khách mua hiển thị ra ngoài card bàn kèm cờ hasMore nếu còn */
    getStatusName(status: string): string {
        return this.getStatusText(status);
    }

    getTableServicesSummary(table: any): { items: { name: string; qty: number }[]; hasMore: boolean } {
        if (!table || table.Status === 'Empty') return { items: [], hasMore: false };

        let items: any[] = [];

        // 1. If currently editing order for this table, use orderItems
        if (this.isEditingOrder && this.currentOrder && this.orderItems && this.orderItems.length > 0) {
            const isMatch = (this.currentOrder.TableId && this.currentOrder.TableId === table.TableId) ||
                (this.currentOrder.TableCode && this.currentOrder.TableCode === table.TableCode) ||
                (this.currentOrder.TableName && this.currentOrder.TableName === table.TableName) ||
                (this.isTableSelected(table.TableId));
            if (isMatch) {
                items = this.orderItems;
            }
        }

        // 2. Direct map of loaded items
        if ((!items || items.length === 0) && table.TableId && this.tableActiveItemsMap.has(table.TableId)) {
            items = this.tableActiveItemsMap.get(table.TableId) || [];
        }

        // 3. Direct LoadedItems property on table
        if ((!items || items.length === 0) && table.LoadedItems && table.LoadedItems.length > 0) {
            items = table.LoadedItems;
        }

        // 4. Extract from table object directly
        if (!items || items.length === 0) {
            items = this.extractItemsFromResponseData(table);
        }

        // 5. Match from allActiveOrders
        if ((!items || items.length === 0) && this.allActiveOrders && this.allActiveOrders.length > 0) {
            const matchedOrder = this.allActiveOrders.find(o =>
                (o.TableId && o.TableId === table.TableId) ||
                (o.TableName && o.TableName === table.TableName) ||
                (o.TableCode && o.TableCode === table.TableCode) ||
                (o.Tables && o.Tables.some((t: any) => t.TableId === table.TableId || t.TableCode === table.TableCode))
            );
            if (matchedOrder) {
                items = this.extractItemsFromResponseData(matchedOrder);
            }
        }

        if (!items || items.length === 0) return { items: [], hasMore: false };

        const mapped = items
            .filter((item: any) => item && item.IsCancelled !== 1 && item.isCancelled !== 1 && item.KitchenStatus !== 'Cancelled')
            .map((item: any) => ({
                name: item.ItemName || item.MenuItemName || item.Name || item.ProductName || 'Dịch vụ',
                qty: item.Quantity || item.Qty || item.Amount || 1
            }));

        const hasMore = mapped.length > 3;
        const displayItems = mapped.slice(0, 3);

        return { items: displayItems, hasMore };
    }

    /** Lấy thời gian đã phục vụ của bàn dạng HH:MM:SS (tính theo OpenTime của bàn/hóa đơn) */
    getTableDuration(table: any): string {
        if (!table || table.Status === 'Empty') {
            if (table) table._firstSeenTime = null;
            return '';
        }

        const activeItems = table.LoadedItems || this.tableActiveItemsMap.get(table.TableId) || [];
        const firstBill = (table.CurrentBills && table.CurrentBills.length > 0) ? table.CurrentBills[0] : (table.CurrentBill || null);
        const firstActiveOrder = (table.ActiveOrders && table.ActiveOrders.length > 0) ? table.ActiveOrders[0] : null;

        // 1. Kiểm tra mốc OpenTime của Bàn / Hóa đơn / Đơn hàng
        let startTimeStr: string | null =
            table.OpenTime ||
            (firstBill ? firstBill.OpenTime : null) ||
            (firstActiveOrder ? firstActiveOrder.OpenTime : null);

        // 2. Kiểm tra mốc thời gian món ăn/dịch vụ lẻ nếu chưa có OpenTime
        if (!startTimeStr && activeItems && activeItems.length > 0) {
            for (const item of activeItems) {
                const iTime = item.CreatedDate || item.CreatedTime || item.OrderTime || item.Created || item.DateCreated || item.CreatedAt;
                if (iTime) {
                    const parsed = this.parseDateTimeMs(iTime);
                    if (parsed !== null) {
                        const currentParsed = this.parseDateTimeMs(startTimeStr);
                        if (!startTimeStr || (currentParsed !== null && parsed < currentParsed)) {
                            startTimeStr = iTime;
                        }
                    }
                }
            }
        }

        // 3. Fallback mốc thời gian tạo Đơn hàng / Bàn khác
        if (!startTimeStr) {
            startTimeStr =
                table.ServingTime || table.CreatedDate || table.CreatedTime || table.UseTime || table.OrderTime || table.InTime || table.CheckInTime || table.StartTime || table.OrderDate || table.Created || table.DateCreated || table.CreateDate || table.CreatedAt || table.BillDate || table.BillTime ||
                (firstBill ? (firstBill.CreatedDate || firstBill.CreatedTime || firstBill.OrderTime || firstBill.ServingTime || firstBill.UseTime || firstBill.CheckInTime || firstBill.StartTime || firstBill.CreatedTime || firstBill.OrderDate || firstBill.Created || firstBill.DateCreated || firstBill.CreateDate || firstBill.CreatedAt || firstBill.BillDate || firstBill.BillTime) : null);
        }

        // 4. Fallback khớp từ danh sách active orders toàn cục
        if (!startTimeStr && this.allActiveOrders && this.allActiveOrders.length > 0) {
            const matchedOrder = this.allActiveOrders.find((o: any) =>
                (o.TableId && o.TableId === table.TableId) ||
                (o.TableName && o.TableName === table.TableName) ||
                (o.TableCode && o.TableCode === table.TableCode) ||
                (o.Tables && o.Tables.some((t: any) => t.TableId === table.TableId || t.TableCode === table.TableCode))
            );
            if (matchedOrder) {
                startTimeStr = matchedOrder.OpenTime || matchedOrder.CreatedDate || matchedOrder.CreatedTime || matchedOrder.OrderTime || matchedOrder.ServingTime || matchedOrder.UseTime || matchedOrder.CheckInTime || matchedOrder.StartTime || matchedOrder.OrderDate || matchedOrder.Created || matchedOrder.CreatedAt || matchedOrder.BillDate;
            }
        }

        const now = this.clockService.time();
        let startTime: number | null = this.parseDateTimeMs(startTimeStr);

        // 5. Nếu hoàn toàn không có mốc thời gian, tự khởi tạo _firstSeenTime để đếm tăng dần từ 00:00:00
        if (startTime === null) {
            const numericMinutes = table.Minutes ?? table.UseMinutes ?? table.ServingMinutes ?? (firstBill ? (firstBill.Minutes ?? firstBill.UseMinutes) : null);
            if (numericMinutes != null && numericMinutes !== undefined) {
                if (!table._firstSeenTime) {
                    table._firstSeenTime = now - Math.floor(numericMinutes * 60 * 1000);
                }
            } else {
                if (!table._firstSeenTime) {
                    table._firstSeenTime = now;
                }
            }
            startTime = table._firstSeenTime;
        }

        const diffMs = Math.max(0, now - startTime!);
        const totalSeconds = Math.floor(diffMs / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;

        const pad = (n: number) => n.toString().padStart(2, '0');
        return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
    }

    /** Lấy mốc thời gian khách vào hoặc đặt món đầu tiên dạng HH:mm */
    getTableOpenTimeStr(table: any): string {
        if (!table || table.Status === 'Empty') return '--:--';

        const activeItems = table.LoadedItems || this.tableActiveItemsMap.get(table.TableId) || [];
        const firstBill = (table.CurrentBills && table.CurrentBills.length > 0) ? table.CurrentBills[0] : (table.CurrentBill || null);
        const firstActiveOrder = (table.ActiveOrders && table.ActiveOrders.length > 0) ? table.ActiveOrders[0] : null;

        let startTimeStr: string | null =
            table.OpenTime ||
            (firstBill ? firstBill.OpenTime : null) ||
            (firstActiveOrder ? firstActiveOrder.OpenTime : null);

        if (!startTimeStr && activeItems && activeItems.length > 0) {
            for (const item of activeItems) {
                const iTime = item.CreatedDate || item.CreatedTime || item.OrderTime || item.Created || item.DateCreated || item.CreatedAt;
                if (iTime) {
                    const parsed = this.parseDateTimeMs(iTime);
                    if (parsed !== null) {
                        const currentParsed = this.parseDateTimeMs(startTimeStr);
                        if (!startTimeStr || (currentParsed !== null && parsed < currentParsed)) {
                            startTimeStr = iTime;
                        }
                    }
                }
            }
        }

        if (!startTimeStr) {
            startTimeStr =
                table.ServingTime || table.CreatedDate || table.CreatedTime || table.UseTime || table.OrderTime || table.InTime || table.CheckInTime || table.StartTime || table.OrderDate || table.Created || table.DateCreated || table.CreateDate || table.CreatedAt || table.BillDate || table.BillTime ||
                (firstBill ? (firstBill.CreatedDate || firstBill.CreatedTime || firstBill.OrderTime || firstBill.ServingTime || firstBill.UseTime || firstBill.CheckInTime || firstBill.StartTime || firstBill.CreatedTime || firstBill.OrderDate || firstBill.Created || firstBill.DateCreated || firstBill.CreateDate || firstBill.CreatedAt || firstBill.BillDate || firstBill.BillTime) : null);
        }

        if (!startTimeStr && this.allActiveOrders && this.allActiveOrders.length > 0) {
            const matchedOrder = this.allActiveOrders.find((o: any) =>
                (o.TableId && o.TableId === table.TableId) ||
                (o.TableName && o.TableName === table.TableName) ||
                (o.TableCode && o.TableCode === table.TableCode) ||
                (o.Tables && o.Tables.some((t: any) => t.TableId === table.TableId || t.TableCode === table.TableCode))
            );
            if (matchedOrder) {
                startTimeStr = matchedOrder.OpenTime || matchedOrder.CreatedDate || matchedOrder.CreatedTime || matchedOrder.OrderTime || matchedOrder.ServingTime || matchedOrder.UseTime || matchedOrder.CheckInTime || matchedOrder.StartTime || matchedOrder.OrderDate || matchedOrder.Created || matchedOrder.CreatedAt || matchedOrder.BillDate;
            }
        }

        const ms = this.parseDateTimeMs(startTimeStr);
        if (ms !== null) {
            const date = new Date(ms);
            const pad = (n: number) => n.toString().padStart(2, '0');
            return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
        }
        return '--:--';
    }



    getStatusText(status: string): string {
        switch (status) {
            case 'Serving': return 'Đang phục vụ';
            case 'WaitingService': return 'Chờ phục vụ';
            case 'WaitingPayment': return 'Chờ thanh toán';
            case 'Reserved': return 'Đặt trước';
            case 'Checked': return 'Đã kiểm đồ';
            case 'WaitingConfirm': return 'Chờ xác nhận';
            case 'TakeAway': return 'Mang về';
            case 'Delivery': return 'Giao hàng';
            case 'Paid': return 'Đã thanh toán';
            case 'Completed': return 'Hoàn thành';
            case 'Cancelled': return 'Đã hủy';
            case 'ApprovedCancelled': return 'Đã duyệt hủy';
            case 'Merged': return 'Đã gộp';
            case 'Empty': return 'Bàn trống';
            default: return status || 'Trống';
        }
    }

    getStatusTextColor(status: string): string {
        switch (status) {
            case 'Empty': return 'text-blue-600 dark:text-blue-400';
            case 'Serving': return 'text-emerald-600 dark:text-emerald-400';
            case 'WaitingService': return 'text-orange-600 dark:text-orange-400';
            case 'Checked': return 'text-teal-600 dark:text-teal-400';
            case 'Reserved': return 'text-purple-600 dark:text-purple-400';
            case 'WaitingPayment': return 'text-amber-600 dark:text-amber-400';
            case 'WaitingConfirm': return 'text-red-600 dark:text-red-400';
            case 'TakeAway': return 'text-green-600 dark:text-green-400';
            case 'Delivery': return 'text-sky-600 dark:text-sky-400';
            default: return 'text-surface-600 dark:text-surface-400';
        }
    }

    getSelectedTablesData(): any[] {
        return this.tables.filter(t => this.isTableSelected(t.TableId));
    }

    getFirstSelectedTable(): any {
        const selected = this.getSelectedTablesData();
        return selected.length > 0 ? selected[0] : null;
    }

    // Helper conditions for button states
    canOpenTable(): boolean {
        return this.selectedTableIds().length > 0;
    }

    canMergeTables(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length >= 1 && selected.some(t => t.Status !== 'Empty');
    }

    canSplitTable(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length === 1 && selected[0].Status !== 'Empty';
    }

    canTransferTable(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length === 1 && selected[0].Status !== 'Empty';
    }

    canCancelTable(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length === 1 && selected[0].Status !== 'Empty';
    }

    canCheckItems(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length === 1 && selected[0].Status !== 'Empty';
    }

    canEditBill(): boolean {
        const selected = this.getSelectedTablesData();
        return selected.length === 1 && selected[0].Status !== 'Empty';
    }

    // ----------------------------------------------------
    // ACTION IMPLEMENTATIONS
    // --    // 1. Open Table
    actionOpenTable() {
        if (!this.canOpenTable()) return;

        // Reset order info
        this.orderWaiterId = null;
        this.orderCustomerCount = 1;
        this.orderCustomerId = null;
        this.selectedPromotionCode = null;
        this.therapyVoucherCode = '';
        if (!this.openTime) {
            this.openTime = new Date();
        }
        this.phoneNumber = '';
        this.deliveryAddress = '';
        this.deliveryNote = '';
        this.shippingFeeNote = '';
        this.orderItems = [];

        const selectedTables = this.getSelectedTablesData();
        if (selectedTables.length === 1) {
            const table = selectedTables[0];
            if (table.Status !== 'Empty') {
                this.confirmationService.confirm({
                    message: 'Bàn đang có khách, vui lòng xác nhận mở thêm order mới',
                    header: 'Xác nhận mở thêm order mới',
                    icon: 'pi pi-exclamation-triangle',
                    acceptLabel: 'Xác nhận',
                    rejectLabel: 'Hủy',
                    accept: () => {
                        this.startDraftOrderEditing(table);
                        this.isEditingOrder = false; // Hide drawer popup!
                        this.openFoodDialog(); // Open fullscreen selection screen!
                    }
                });
            } else {
                this.startDraftOrderEditing(table);
                this.isEditingOrder = false; // Hide drawer popup!
                this.openFoodDialog(); // Open fullscreen selection screen!
            }
        } else {
            // Check if any selected table is occupied
            const hasOccupied = selectedTables.some(t => t.Status !== 'Empty');
            if (hasOccupied) {
                this.confirmationService.confirm({
                    message: 'Bàn đang có khách, vui lòng xác nhận mở thêm order mới',
                    header: 'Xác nhận mở thêm order mới',
                    icon: 'pi pi-exclamation-triangle',
                    acceptLabel: 'Xác nhận',
                    rejectLabel: 'Hủy',
                    accept: () => {
                        this.startDraftOrderEditing(null);
                        this.isEditingOrder = false;
                        this.openFoodDialog();
                    }
                });
            } else {
                this.startDraftOrderEditing(null);
                this.isEditingOrder = false;
                this.openFoodDialog();
            }
        }
    }

    startOrderEditing(bill: any) {
        if (bill) {
            if (!bill.BillId && bill.Id) {
                bill.BillId = bill.Id;
            }
            if (!bill.BillCode && bill.OrderCode) {
                bill.BillCode = bill.OrderCode;
            }
            if (!bill.TableId && bill.Tables && bill.Tables.length > 0) {
                bill.TableId = bill.Tables[0].TableId;
            }
            if (!bill.TableCode && bill.Tables && bill.Tables.length > 0) {
                bill.TableCode = bill.Tables[0].TableCode || bill.Tables[0].TableName;
            }
            if (!bill.TableName && bill.Tables && bill.Tables.length > 0) {
                bill.TableName = bill.Tables[0].TableName || bill.Tables[0].TableCode;
            }
        }
        this.currentOrder = bill;
        this.selectedOrderId = bill?.BillId || bill?.Id || this.selectedOrderId;
        const totalBillDiscount = bill?.DiscountAmount || bill?.discountAmount || 0;
        const hasOrderPromo = bill?.SelectedPromotionCode || bill?.selectedPromotionCode;
        this.selectedPromotionCode = hasOrderPromo || null;
        this.therapyVoucherCode = bill?.TherapyVoucherCode || bill?.therapyVoucherCode || '';
        if (this.therapyVoucherCode) {
            this.onValidateTherapyVoucher(this.therapyVoucherCode);
        } else {
            this.therapyServices = [];
            this.selectedTherapyServiceCode = '';
        }

        // Tính tổng các khoản giảm giá gốc (gồm cả net discount và discountMoney của món trả/báo hết) trên từng món trong bill.Items
        let pureItemDiscountsSum = 0;
        (bill.Items || []).forEach((item: any) => {
            if (item.IsCancelled === 1 || item.isCancelled === 1 || item.KitchenStatus === 'Cancelled') return;
            const rawDisc = item.DiscountAmount || item.discountAmount || 0;
            const discMoney = item.DiscountMoney || item.discountMoney || 0;
            pureItemDiscountsSum += (rawDisc + discMoney);
        });

        const orderLevelDiscountTotal = Math.max(0, totalBillDiscount - pureItemDiscountsSum);

        this.appliedOrderLevelPromoDiscount = hasOrderPromo ? orderLevelDiscountTotal : 0;
        this.appliedOrderLevelManualDiscount = !hasOrderPromo ? orderLevelDiscountTotal : 0;

        if (this.currentOrder) {
            if (this.currentOrder.TableId) {
                const tbl = (this.tables || []).find(t => t.TableId === this.currentOrder.TableId)
                    || (this.allTablesOfFloor || []).find(t => t.TableId === this.currentOrder.TableId)
                    || (this.availableEmptyTables || []).find(t => (t.TableId === this.currentOrder.TableId || t.Id === this.currentOrder.TableId));
                if (tbl) {
                    this.currentOrder.TableCode = tbl.TableCode || tbl.TableName || this.currentOrder.TableCode;
                    this.currentOrder.TableName = tbl.TableName || tbl.TableCode || this.currentOrder.TableName;
                }
            }
            if (!this.currentOrder.TableName && this.currentOrder.TableCode) {
                this.currentOrder.TableName = this.currentOrder.TableCode;
            }
            if (!this.currentOrder.TableCode && this.currentOrder.TableName) {
                this.currentOrder.TableCode = this.currentOrder.TableName;
            }
        }
        this.orderCustomerCount = bill.CustomerCount || 1;
        this.orderWaiterId = bill.WaiterId || null;
        this.orderCustomerId = bill.CustomerId || null;
        if (this.orderCustomerId && !this.customers().some(c => c.Id === this.orderCustomerId)) {
            const customer = {
                Id: this.orderCustomerId,
                Code: bill.CustomerCode,
                Name: bill.CustomerName,
                DisplayLabel: `${bill.CustomerCode || ''} - ${bill.CustomerName || ''}`
            };
            this.customers.update(customers => [...customers, customer]);
        }
        this.phoneNumber = bill.PhoneNumber || '';
        this.deliveryAddress = bill.DeliveryAddress || '';
        this.deliveryNote = bill.DeliveryNote || '';
        this.shippingFeeNote = bill.ShippingFeeNote || '';
        this.orderNote = bill.Note || '';
        this.openTime = bill.OpenTime ? new Date(bill.OpenTime) : (bill.OrderDate ? new Date(bill.OrderDate) : new Date());

        // Load items from bill (filter out cancelled items)
        const validItems = (bill.Items || []).filter((item: any) => {
            if (item.IsCancelled === 1 || item.isCancelled === 1 || item.KitchenStatus === 'Cancelled') return false;
            return true;
        });
        this.orderItems = validItems.map((item: any) => {
            const servedQty = item.ServedQty || 0;
            const actualQty = item.IsChecked === 1
                ? (item.ActualQuantity !== undefined && item.ActualQuantity !== null ? item.ActualQuantity : servedQty)
                : servedQty;
            const returnedQty = item.IsChecked === 1
                ? (item.ReturnedQuantity !== undefined && item.ReturnedQuantity !== null ? item.ReturnedQuantity : Math.max(0, servedQty - actualQty))
                : (item.ReturnedQuantity || 0);

            const itemFixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
            const catalogItem = (this.menuItems || []).find(m => m.ItemId === item.ItemId);
            const fixedTax = itemFixedTax > 0 ? itemFixedTax : (catalogItem?.FixedTaxAmount || 0);

            const promoCode = item.PromotionCode || item.promotionCode || null;
            const unitPrice = item.Price || item.UnitPrice || 0;
            const itemAmount = item.Amount !== undefined ? item.Amount : Math.round(actualQty * unitPrice);
            const itemDiscountAmount = item.DiscountAmount !== undefined ? item.DiscountAmount : (item.discountAmount || 0);
            const itemVatAmount = item.VatAmount !== undefined ? item.VatAmount : (item.vatAmount || 0);
            const itemTotalAmount = item.TotalAmount !== undefined ? item.TotalAmount : (item.totalAmount || 0);

            return {
                Id: item.Id || null,
                ItemId: item.ItemId,
                ItemCode: item.ItemCode,
                ItemName: item.ItemName,
                UnitName: item.UnitName || 'Đĩa',
                Quantity: item.Quantity,
                OriginalQuantity: item.Quantity,
                Price: unitPrice,
                Amount: itemAmount,
                DiscountRate: item.DiscountRate || 0,
                DiscountAmount: itemDiscountAmount,
                DiscountMoney: item.DiscountMoney || item.discountMoney || 0,
                VatRate: item.VatRate || 0,
                FixedTaxAmount: fixedTax,
                VatAmount: itemVatAmount,
                TotalAmount: itemTotalAmount,
                Note: item.Note || '',
                SelectedOptions: item.SelectedOptions || [],
                IsSentToKitchen: item.IsSentToKitchen !== undefined ? item.IsSentToKitchen : (item.SentQty > 0 ? 1 : (bill.Status !== 'WaitingConfirm' && bill.Status !== 'Draft' ? 1 : 0)),
                SentQty: item.SentQty || 0,
                ServedQty: servedQty,
                SoldOutQty: item.SoldOutQty || 0,
                RemainingQty: item.RemainingQty || 0,
                KitchenStatus: item.KitchenStatus || 'NotSent',
                PromotionCode: promoCode,
                IsChecked: item.IsChecked || 0,
                ActualQuantity: actualQty,
                ReturnedQuantity: returnedQty
            };
        });
        this.isBillEditUnlocked = bill.Status === 'WaitingConfirm';
        this.isEditingOrder = true;
        // Keep the exact amounts returned by the API when opening a bill.
        // Recalculation here rounds item level promotion allocations again and
        // can change the persisted bill total by a few đồng before any edits.
    }

    startDraftOrderEditing(table: any) {
        const tableCodeName = table ? (table.TableCode || table.TableName) : '';
        this.currentOrder = {
            BillId: null,
            BillCode: 'DRAFT',
            TableId: table ? table.TableId : null,
            TableCode: table ? table.TableCode : '',
            TableName: tableCodeName,
            WaiterId: null,
            CustomerCount: 1,
            Items: [],
            ServiceType: 'DineIn'
        };
        this.appliedOrderLevelPromoDiscount = 0;
        this.appliedOrderLevelManualDiscount = 0;
        this.selectedPromotionCode = null;
        this.therapyVoucherCode = '';
        this.therapyServices = [];
        this.selectedTherapyServiceCode = '';
        this.orderWaiterId = null;
        this.orderCustomerCount = 1;
        this.orderCustomerId = null;
        this.phoneNumber = '';
        this.deliveryAddress = '';
        this.deliveryNote = '';
        this.shippingFeeNote = '';
        this.orderNote = '';
        this.openTime = this.openTime || new Date();
        this.orderItems = [];
        this.isBillEditUnlocked = true;
        this.isEditingOrder = true;
    }

    openTableAndExecute(callback: (newBillId: string) => void, force: boolean = true, onError?: () => void) {
        if (this.currentOrder && this.currentOrder.BillId) {
            callback(this.currentOrder.BillId);
            return;
        }

        if (this.isOpeningTable) {
            onError?.();
            return;
        }
        this.isOpeningTable = true;

        const serviceType = this.currentOrder?.ServiceType || 'DineIn';
        const isTakeAwayOrDelivery = serviceType === 'TakeAway' || serviceType === 'Delivery';

        if (isTakeAwayOrDelivery) {
            if (this.orderItems.length === 0) {
                this.isOpeningTable = false;
                this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một món ăn!' });
                onError?.();
                return;
            }
            if (serviceType === 'Delivery') {
                if (!this.phoneNumber || !this.phoneNumber.trim()) {
                    this.phoneNumber = '0900000000';
                }
                if (!this.deliveryAddress || !this.deliveryAddress.trim()) {
                    this.deliveryAddress = 'Giao tận nơi';
                }
            }

            const payload = this.getSavePayload(null);
            const apiCall = serviceType === 'TakeAway'
                ? this.salesService.createTakeAwayOrder(payload)
                : this.salesService.createDeliveryOrder(payload);

            apiCall.subscribe({
                next: (res: any) => {
                    this.isOpeningTable = false;
                    if (res?.Code === 200 && res.Data) {
                        const newBill = res.Data;
                        const billId = newBill.OrderId || newBill.BillId;
                        const billCode = newBill.OrderCode || newBill.BillCode;

                        this.currentOrder = {
                            ...this.currentOrder,
                            BillId: billId,
                            BillCode: billCode,
                            Status: newBill.Status
                        };
                        this.loadTables();
                        callback(billId);
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không thể tạo đơn hàng' });
                        onError?.();
                    }
                },
                error: (err: any) => {
                    this.isOpeningTable = false;
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi tạo đơn hàng' });
                    onError?.();
                }
            });
            return;
        }

        const tableId = this.currentOrder?.TableId;
        const selectedIds = tableId ? [tableId] : (this.selectedTableIds().length > 0 && this.viewMode() === 'floor-plan' ? this.selectedTableIds() : []);

        const openTimeString = this.formatLocalIso(this.openTime);
        this.salesService.openDineInTable(selectedIds, this.selectedBranchCode, this.orderCustomerCount, this.orderWaiterId, null, force, openTimeString)
            .subscribe({
                next: (res: any) => {
                    this.isOpeningTable = false;
                    if (res?.Code === 200 && res.Data) {
                        const newBill = res.Data;
                        const billId = newBill.OrderId || newBill.BillId;
                        const billCode = newBill.OrderCode || newBill.BillCode;

                        this.currentOrder = {
                            ...this.currentOrder,
                            BillId: billId,
                            BillCode: billCode,
                            Status: newBill.Status
                        };
                        callback(billId);
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không thể tạo bàn' });
                        onError?.();
                    }
                },
                error: (err: any) => {
                    this.isOpeningTable = false;
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi mở bàn' });
                    onError?.();
                }
            });
    }

    reserveTableAndExecute(callback: (newBillId: string) => void, force: boolean = true) {
        if (this.currentOrder && this.currentOrder.BillId) {
            callback(this.currentOrder.BillId);
            return;
        }

        if (this.isOpeningTable) return;
        this.isOpeningTable = true;

        const tableId = this.currentOrder?.TableId;
        const selectedIds = tableId ? [tableId] : (this.selectedTableIds().length > 0 && this.viewMode() === 'floor-plan' ? this.selectedTableIds() : []);

        const openTimeString = this.formatLocalIso(this.openTime);
        this.salesService.reserveOrder(selectedIds, this.selectedBranchCode, this.orderCustomerCount, this.orderWaiterId, null, force, openTimeString)
            .subscribe({
                next: (res: any) => {
                    this.isOpeningTable = false;
                    if (res?.Code === 200 && res.Data) {
                        const newBill = res.Data;
                        const billId = newBill.OrderId || newBill.BillId;
                        const billCode = newBill.OrderCode || newBill.BillCode;

                        this.currentOrder = {
                            ...this.currentOrder,
                            BillId: billId,
                            BillCode: billCode,
                            Status: newBill.Status
                        };
                        this.loadTables();
                        callback(billId);
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không thể đặt trước bàn' });
                    }
                },
                error: () => {
                    this.isOpeningTable = false;
                }
            });
    }

    getSavePayload(newBillId: string | null): any {
        const waiter = this.waiters.find(w => w.Id === this.orderWaiterId);
        const customer = this.customers().find(c => c.Id === this.orderCustomerId);

        const itemsPayload = this.orderItems
            .filter(item => item.Quantity > 0)
            .map(item => ({
                Id: item.Id || null,
                ItemId: item.ItemId,
                ItemCode: item.ItemCode,
                ItemName: item.ItemName,
                UnitName: item.UnitName,
                Quantity: item.Quantity,
                UnitPrice: item.Price,
                DiscountRate: item.DiscountRate || 0,
                VatRate: item.VatRate || 0,
                FixedTaxAmount: item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0),
                PromotionCode: item.PromotionCode || null,
                Note: item.Note,
                SelectedOptions: item.SelectedOptions || []
            }));

        const tableId = this.currentOrder?.TableId || (this.currentOrder?.Tables && this.currentOrder.Tables.length > 0 ? this.currentOrder.Tables[0].TableId : null);
        const isTakeAwayOrDelivery = this.currentOrder?.ServiceType === 'TakeAway' || this.currentOrder?.ServiceType === 'Delivery';
        const selectedIds = isTakeAwayOrDelivery
            ? []
            : (this.currentOrder?.Tables && this.currentOrder.Tables.length > 0
                ? this.currentOrder.Tables.map((t: any) => t.TableId || t.Id).filter(Boolean)
                : (tableId ? [tableId] : (this.selectedTableIds().length > 0 ? this.selectedTableIds() : [])));

        return {
            OrderId: newBillId,
            BranchCode: this.selectedBranchCode,
            TableIds: selectedIds,
            CustomerId: this.orderCustomerId,
            CustomerIdSpecified: true,
            CustomerCode: customer ? customer.Code : 'GUEST',
            CustomerName: customer ? customer.Name : 'Khách lẻ',
            CustomerCount: this.orderCustomerCount,
            WaiterId: this.orderWaiterId,
            WaiterName: waiter ? waiter.Name : null,
            ServiceType: this.currentOrder?.ServiceType || 'DineIn',
            PhoneNumber: this.phoneNumber,
            DeliveryAddress: this.deliveryAddress,
            DeliveryNote: this.deliveryNote,
            ShippingFeeNote: this.shippingFeeNote,
            Note: this.orderNote || '',
            OpenTime: this.formatLocalIso(this.openTime),
            OrderDate: this.formatLocalIso(this.openTime),
            SelectedPromotionCode: this.selectedPromotionCode,
            TherapyVoucherCode: this.therapyVoucherCode,
            Items: itemsPayload
        };
    }

    optionDialogVisible = false;
    optionDialogItem: any = null;
    optionDialogGroups: any[] = [];
    optionDialogQuantity: number = 1;

    increaseOptionQuantity() {
        const catalogItem = (this.menuItems || []).find(m => m.ItemId === this.optionDialogItem?.ItemId) || this.optionDialogItem;
        if (catalogItem?.IsTrackStock && catalogItem.StockQuantity !== null && catalogItem.StockQuantity !== undefined) {
            if (this.optionDialogQuantity >= catalogItem.StockQuantity) {
                this.messageService.add({
                    severity: 'warn',
                    summary: `Chỉ còn tối đa ${catalogItem.StockQuantity} suất!`
                });
                return;
            }
        }
        this.optionDialogQuantity = (this.optionDialogQuantity || 1) + 1;
    }

    decreaseOptionQuantity() {
        if (this.optionDialogQuantity > 1) {
            this.optionDialogQuantity--;
        }
    }

    onOptionDialogQuantityChange(event: any) {
        const val = parseInt(event?.target?.value, 10);
        let targetVal = isNaN(val) || val < 1 ? 1 : val;
        const catalogItem = (this.menuItems || []).find(m => m.ItemId === this.optionDialogItem?.ItemId) || this.optionDialogItem;
        if (catalogItem?.IsTrackStock && catalogItem.StockQuantity !== null && catalogItem.StockQuantity !== undefined) {
            if (targetVal > catalogItem.StockQuantity) {
                targetVal = catalogItem.StockQuantity;
                this.messageService.add({
                    severity: 'warn',
                    summary: `Chỉ còn tối đa ${catalogItem.StockQuantity} suất!`
                });
            }
        }
        this.optionDialogQuantity = targetVal;
        if (event?.target) {
            event.target.value = targetVal;
        }
    }

    optionDialogTaxAmount(value: any): number {
        const item = this.optionDialogItem;
        const rate = Number(item?.VatRate ?? item?.TaxRatePercent ?? 0);
        const rawPercentTax = Number(value?.ExtraCharge || 0) * rate / 100;
        const percentTax = rate > 0 ? Math.sign(rawPercentTax) * Math.round(Math.abs(rawPercentTax)) : 0;
        const fixedTax = (value?.FixedTax !== null && value?.FixedTax !== undefined && value?.FixedTax !== '')
            ? (Number(value.FixedTax) || 0) : 0;
        return fixedTax !== 0 ? fixedTax : percentTax;
    }

    optionDialogPriceWithTax(value: any): number {
        return Number(value?.ExtraCharge || 0) + this.optionDialogTaxAmount(value);
    }
    optionDialogSelection: Record<string, string[]> = {};

    setOptionSelection(group: any, code: string, checked: boolean) {
        const current = this.optionDialogSelection[group.Code] || [];
        this.optionDialogSelection[group.Code] = group.Multiple
            ? (checked ? [...current, code] : current.filter(x => x !== code))
            : [code];
    }

    confirmMenuOptions() {
        for (const group of this.optionDialogGroups) {
            if (group.Required && !(this.optionDialogSelection[group.Code] || []).length) {
                this.messageService.add({ severity: 'warn', summary: `Vui lòng chọn ${group.Name}` });
                return;
            }
        }
        const selected = this.optionDialogGroups.flatMap(group =>
            (this.optionDialogSelection[group.Code] || []).map(code => {
                const value = group.Values.find((v: any) => v.Code === code);
                return {
                    GroupCode: group.Code,
                    GroupName: group.Name,
                    ValueCode: code,
                    ValueName: value?.Name,
                    ExtraCharge: Number(value?.ExtraCharge || 0),
                    FixedTax: value?.FixedTax !== null && value?.FixedTax !== undefined && value?.FixedTax !== '' ? Number(value.FixedTax) : null
                };
            }));
        const extra = this.optionDialogGroups.reduce((sum, group) => sum + group.Values
            .filter((value: any) => (this.optionDialogSelection[group.Code] || []).includes(value.Code))
            .reduce((valueSum: number, value: any) => valueSum + Number(value.ExtraCharge || 0), 0), 0);
        if (Number(this.optionDialogItem.Price) + extra < 0) {
            this.messageService.add({ severity: 'warn', summary: 'Giá món không thể âm' });
            return;
        }
        const qty = this.optionDialogQuantity && this.optionDialogQuantity > 0 ? this.optionDialogQuantity : 1;
        this.optionDialogVisible = false;
        this.orderAddItem({ ...this.optionDialogItem, Price: Number(this.optionDialogItem.Price) + extra,
            SelectedOptions: selected, __optionsChosen: true }, qty);
    }

    orderAddItem(item: any, quantity: number = 1) {
        const catalogOptionJson = item.OptionGroupsJson || (this.menuItems || []).find(m => m.ItemId === item.ItemId)?.OptionGroupsJson;
        if (catalogOptionJson && !item.__optionsChosen) {
            try {
                const groups = JSON.parse(catalogOptionJson);
                if (Array.isArray(groups) && groups.length) {
                    this.optionDialogItem = item;
                    this.optionDialogGroups = groups;
                    this.optionDialogSelection = {};
                    this.optionDialogQuantity = quantity && quantity > 0 ? quantity : 1;
                    groups.forEach((group: any) => this.optionDialogSelection[group.Code] = group.Values
                        .filter((value: any) => value.IsDefault).map((value: any) => value.Code));
                    this.optionDialogVisible = true;
                    return;
                }
            } catch { /* invalid catalog options are rejected by the API */ }
        }
        if (item.IsAvailable === false) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Món ăn hiện đã hết (Tắt hiển thị)!'
            });
            return;
        }

        const addQty = quantity && quantity > 0 ? quantity : 1;

        // Kiểm tra tồn kho nếu món có cấu hình quản lý tồn
        const catalogItemForStock = (this.menuItems || []).find(m => m.ItemId === item.ItemId) || item;
        if (catalogItemForStock?.IsTrackStock && catalogItemForStock.StockQuantity !== null && catalogItemForStock.StockQuantity !== undefined) {
            const currentSelectedQty = (this.orderItems || [])
                .filter(x => x.ItemId === item.ItemId && !(x.PromotionCode && x.DiscountRate === 100))
                .reduce((sum, x) => sum + (x.Quantity || 0), 0);
            if (currentSelectedQty + addQty > catalogItemForStock.StockQuantity) {
                this.messageService.add({
                    severity: 'warn',
                    summary: `Món '${catalogItemForStock.Name || catalogItemForStock.ItemName}' chỉ còn ${catalogItemForStock.StockQuantity} suất (Đã chọn ${currentSelectedQty} suất)!`
                });
                return;
            }
        }

        // Hủy áp dụng khuyến mại khi mở rộng / thêm món ăn mới
        if (this.selectedPromotionCode || this.appliedOrderLevelPromoDiscount > 0) {
            const oldPromo = this.selectedPromotionCode;
            this.selectedPromotionCode = null;
            this.appliedOrderLevelPromoDiscount = 0;

            // Loại bỏ các món tặng từ CTKM (nếu có)
            this.orderItems = (this.orderItems || []).filter(x => !(x.DiscountRate === 100 && (x.PromotionCode || (x.Note && x.Note.startsWith("Món tặng")))));

            // Reset chiết khấu/khuyến mại phân bổ trên các món ăn về 0
            this.orderItems.forEach(x => {
                if (!x.PromotionCode || x.PromotionCode === oldPromo) {
                    x.DiscountRate = 0;
                    x.DiscountAmount = 0;
                    x.PromotionCode = null;
                }
            });

            this.messageService.add({
                severity: 'info',
                summary: 'Thông báo',
                detail: 'Đã hủy áp dụng khuyến mại khi thêm món mới vào đơn hàng.'
            });
        }

        const existing = this.orderItems.find(x => x.ItemId === item.ItemId &&
            JSON.stringify(x.SelectedOptions || []) === JSON.stringify(item.SelectedOptions || []) &&
            !(x.PromotionCode && x.DiscountRate === 100));
        const itemFixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
        const catalogItem = (this.menuItems || []).find(m => m.ItemId === item.ItemId);
        const fixedTax = itemFixedTax > 0 ? itemFixedTax : (catalogItem?.FixedTaxAmount || 0);

        if (existing) {
            existing.Quantity += addQty;
            if ((!existing.FixedTaxAmount || existing.FixedTaxAmount <= 0) && fixedTax > 0) {
                existing.FixedTaxAmount = fixedTax;
            }
            this.recalculateItemTotal(existing);
        } else {
            const newItem = {
                Id: null,
                ItemId: item.ItemId,
                ItemCode: item.ItemCode,
                ItemName: item.ItemName,
                UnitName: item.UnitName,
                Quantity: addQty,
                Price: item.Price,
                Amount: item.Price,
                DiscountRate: 0,
                DiscountAmount: 0,
                VatRate: item.VatRate || 0,
                FixedTaxAmount: fixedTax,
                VatAmount: 0,
                TotalAmount: item.Price,
                Note: '',
                SelectedOptions: item.SelectedOptions || [],
                IsSentToKitchen: 0,
                SentQty: 0,
                ServedQty: 0,
                SoldOutQty: 0,
                RemainingQty: 0,
                KitchenStatus: 'NotSent'
            };
            this.orderItems.push(newItem);
            this.recalculateItemTotal(newItem);
        }
    }

    getActualUsedQty(item: any): number {
        if (!item) return 0;
        if (item.IsCancelled === 1 || item.isCancelled === 1 || item.KitchenStatus === 'Cancelled') return 0;
        if (item.IsChecked === 1 && item.ActualQuantity !== undefined && item.ActualQuantity !== null) {
            return item.ActualQuantity;
        }
        const orderQty = item.Quantity || 0;
        const soldOut = item.SoldOutQty || 0;
        const returned = item.ReturnedQuantity || 0;
        const cancelled = item.CancelledQuantity || item.cancelledQuantity || item.CancelQuantity || 0;
        return Math.max(0, orderQty - soldOut - returned - cancelled);
    }

    recalculateAllItemTotals() {
        const orderLevelDiscount = (this.appliedOrderLevelPromoDiscount || 0) + (this.appliedOrderLevelManualDiscount || 0);
        const totalNetBeforeOrderDiscount = (this.orderItems || []).reduce((acc, item) => {
            const actualQty = this.getActualUsedQty(item);
            const unitPrice = item.Price || 0;
            const vatRate = item.VatRate || 0;
            const fixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
            const unitVat = fixedTax > 0 ? fixedTax : Math.round(unitPrice * (vatRate / 100));
            const unitGross = unitPrice + unitVat;
            const itemDisc = item.PromotionCode && item.DiscountAmount !== undefined && item.DiscountAmount !== null
                ? item.DiscountAmount
                : (item.DiscountRate > 0)
                    ? (actualQty * Math.round(unitGross * (item.DiscountRate / 100)))
                    : (item.DiscountAmount || 0);
            return acc + Math.max(0, (actualQty * unitGross) - itemDisc);
        }, 0);

        (this.orderItems || []).forEach(item => {
            const qty = this.getActualUsedQty(item);
            const unitPrice = item.Price || 0;
            const expectedAmount = Math.round(qty * unitPrice);

            let fixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
            if ((!fixedTax || fixedTax <= 0) && this.menuItems && this.menuItems.length) {
                const catalogItem = this.menuItems.find(m => m.ItemId === item.ItemId);
                if (catalogItem && catalogItem.FixedTaxAmount > 0) {
                    fixedTax = catalogItem.FixedTaxAmount;
                    item.FixedTaxAmount = catalogItem.FixedTaxAmount;
                }
            }

            item.Amount = expectedAmount;

            const vatRate = item.VatRate || 0;
            const unitVat = fixedTax > 0 ? fixedTax : Math.round(unitPrice * (vatRate / 100));
            const unitGross = unitPrice + unitVat;

            let itemDiscount = 0;
            if (item.PromotionCode && item.DiscountAmount !== undefined && item.DiscountAmount !== null) {
                // Promotion discount from the API already includes percentage, fixed
                // per-unit, and any QR-order discount allocated to this line.
                itemDiscount = item.DiscountAmount;
            } else if (item.DiscountRate > 0) {
                const unitDisc = Math.round(unitGross * (item.DiscountRate / 100));
                itemDiscount = unitDisc * qty;
            } else if (item.DiscountAmount !== undefined && item.DiscountAmount !== null) {
                itemDiscount = item.DiscountAmount;
            }

            const itemGrossAmount = Math.round(unitGross * qty);
            const itemNetBeforeOrder = Math.max(0, itemGrossAmount - itemDiscount);
            const itemOrderDiscount = (orderLevelDiscount > 0 && totalNetBeforeOrderDiscount > 0)
                ? Math.round(orderLevelDiscount * (itemNetBeforeOrder / totalNetBeforeOrderDiscount))
                : 0;

            const catalogItem = (this.menuItems || []).find(m => m.ItemId === item.ItemId);
            const calc = OrderCalculator.calculateItem({
                qty,
                unitPrice,
                basePrice: catalogItem?.SalePrice ?? catalogItem?.Price ?? item.SalePrice ?? item.Price,
                itemDiscount,
                itemOrderDiscount,
                vatRate: item.VatRate || 0,
                fixedTax
            });

            item.DiscountAmount = calc.discountAmount;
            item.VatAmount = calc.vatAmount;
            item.TotalAmount = calc.totalAmount;
        });

        if (this.currentOrder) {
            const subTotal = (this.orderItems || []).reduce((acc, item) => acc + (item.Amount || 0), 0);
            const promoItemSum = (this.orderItems || []).reduce((acc, item) => acc + (item.PromotionCode ? (item.DiscountAmount || 0) : 0), 0);
            const manualItemSum = (this.orderItems || []).reduce((acc, item) => acc + (!item.PromotionCode ? (item.DiscountAmount || 0) : 0), 0);
            const orderDiscount = (this.appliedOrderLevelPromoDiscount || 0) + promoItemSum + (this.appliedOrderLevelManualDiscount || 0) + manualItemSum;
            const vatAmount = (this.orderItems || []).reduce((acc, item) => acc + (item.VatAmount || 0), 0);
            const isDelivery = (this.currentOrder?.ServiceType === 'Delivery') || (this.checkBillDetail?.ServiceType === 'Delivery');
            const shippingFee = isDelivery ? this.parseShippingFee(this.shippingFeeNote || this.currentOrder?.ShippingFeeNote || this.checkBillDetail?.ShippingFeeNote) : 0;
            const totalAmount = Math.max(0, subTotal - orderDiscount + vatAmount) + shippingFee;

            this.currentOrder.SubTotal = subTotal;
            this.currentOrder.DiscountAmount = orderDiscount;
            this.currentOrder.VatAmount = totalAmount <= 0 ? 0 : vatAmount;
            this.currentOrder.TotalAmount = totalAmount;
            this.currentOrder.SelectedPromotionCode = this.selectedPromotionCode;
            this.currentOrder.Items = this.orderItems;
        }
    }

    recalculateItemTotal(item: any) {
        this.recalculateAllItemTotals();
    }

    getOrderSubTotal(): number {
        return (this.orderItems || []).reduce((acc, item) => acc + (item.Amount || 0), 0);
    }

    getAppliedOrderDiscount(): number {
        const promotionItemDiscountsSum = (this.orderItems || []).reduce((acc, item) => {
            const hasPromo = item.PromotionCode || item.promotionCode;
            return acc + (hasPromo ? (item.DiscountAmount || 0) : 0);
        }, 0);
        return (this.appliedOrderLevelPromoDiscount || 0) + promotionItemDiscountsSum;
    }

    getAppliedManualDiscount(): number {
        const manualItemDiscountsSum = (this.orderItems || []).reduce((acc, item) => {
            const hasPromo = item.PromotionCode || item.promotionCode;
            return acc + (!hasPromo ? (item.DiscountAmount || 0) : 0);
        }, 0);
        return (this.appliedOrderLevelManualDiscount || 0) + manualItemDiscountsSum;
    }

    getOrderDiscountAmount(): number {
        return this.getAppliedOrderDiscount() + this.getAppliedManualDiscount();
    }

    getOrderPreTaxAmount(): number {
        return Math.max(0, this.getOrderSubTotal() - this.getOrderDiscountAmount());
    }

    getOrderVatAmount(): number {
        const subTotal = this.getOrderSubTotal();
        const disc = this.getOrderDiscountAmount();
        const rawVat = (this.orderItems || []).reduce((acc, item) => acc + (item.VatAmount || 0), 0);
        if (Math.max(0, subTotal - disc + rawVat) <= 0) return 0;
        return rawVat;
    }

    parseShippingFee(val: any): number {
        if (!val) return 0;
        if (typeof val === 'number') return val;
        let str = String(val).trim().toLowerCase();
        if (!str) return 0;
        str = str.replace(/([0-9]+(?:\.[0-9]+)?)\s*k\b/g, (_, p1) => String(parseFloat(p1) * 1000));
        const match = str.match(/\d[\d\.\,]*/);
        if (match) {
            const cleanNum = match[0].replace(/[\.\,]/g, '');
            const num = parseFloat(cleanNum);
            return isNaN(num) ? 0 : num;
        }
        return 0;
    }

    getShippingFeeAmount(): number {
        const isDelivery = (this.currentOrder?.ServiceType === 'Delivery') || (this.checkBillDetail?.ServiceType === 'Delivery');
        if (isDelivery) {
            return this.parseShippingFee(this.shippingFeeNote || this.currentOrder?.ShippingFeeNote || this.checkBillDetail?.ShippingFeeNote);
        }
        return 0;
    }

    getOrderTotalAmount(): number {
        const baseTotal = Math.max(0, this.getOrderSubTotal() - this.getOrderDiscountAmount() + this.getOrderVatAmount());
        return baseTotal + this.getShippingFeeAmount();
    }

    getRoundedTotalAmount(): number {
        return Math.round(this.getOrderTotalAmount());
    }

    getChangeAmount(): number {
        const payAmt = Number(this.paymentAmount) || 0;
        const totalAmt = this.getRoundedTotalAmount();
        return Math.max(0, Math.round(payAmt) - totalAmt);
    }

    orderIncreaseQty(item: any) {
        item.Quantity += 1;
        if (item.IsChecked === 1 || (item.ActualQuantity !== undefined && item.ActualQuantity !== null)) {
            item.ActualQuantity = item.Quantity;
        }
        this.recalculateItemTotal(item);
    }

    orderDecreaseQty(item: any) {
        if (!item) return;
        if (item.Quantity > 0) {
            item.Quantity -= 1;
            if (item.IsChecked === 1 || (item.ActualQuantity !== undefined && item.ActualQuantity !== null)) {
                item.ActualQuantity = item.Quantity;
            }
            this.recalculateItemTotal(item);
        }
    }

    orderChangeQty(event: { item: any, quantity: number }) {
        if (!event?.item) return;
        const targetQty = Math.max(1, Math.floor(Number(event.quantity) || 1));
        event.item.Quantity = targetQty;
        if (event.item.IsChecked === 1 || (event.item.ActualQuantity !== undefined && event.item.ActualQuantity !== null)) {
            event.item.ActualQuantity = targetQty;
        }
        this.recalculateItemTotal(event.item);
    }

    orderRemoveItem(item: any) {
        this.orderItems = this.orderItems.filter(x => x !== item);
        this.recalculateAllItemTotals();
    }

    orderCancelSingleItem(item: any) {
        this.cancellingItem = item;
        const orig = this.currentOrder?.Items?.find((x: any) => (x.Id && item.Id ? x.Id === item.Id : x.ItemId === item.ItemId));
        const baseQty = Math.max(item.Quantity || 0, item.OriginalQuantity || 0, orig?.Quantity || 0, orig?.OriginalQuantity || 0);
        this.cancellingItemBaseQty = baseQty;
        this.cancelItemQty = 1;
        this.cancelItemReason = '';
        this.isCancellingItem = false;
        this.cancelItemDialogVisible = true;
    }

    adjustCancelItemQty(delta: number) {
        const maxQty = this.cancellingItemBaseQty || this.cancellingItem?.Quantity || 1;
        this.cancelItemQty = Math.max(1, Math.min(maxQty, (this.cancelItemQty || 1) + delta));
    }

    setCancelItemQty(qty: number) {
        const maxQty = this.cancellingItemBaseQty || this.cancellingItem?.Quantity || 1;
        this.cancelItemQty = Math.max(1, Math.min(maxQty, qty));
    }

    onCancelItemQtyChange() {
        const maxQty = this.cancellingItemBaseQty || this.cancellingItem?.Quantity || 1;
        if (!this.cancelItemQty || this.cancelItemQty < 1) {
            this.cancelItemQty = 1;
        } else if (this.cancelItemQty > maxQty) {
            this.cancelItemQty = maxQty;
        } else {
            this.cancelItemQty = Math.floor(Number(this.cancelItemQty));
        }
    }

    onCancelItemQtyInput(event: any) {
        const val = event?.target?.value;
        if (val === '' || val === null || val === undefined) return;
        const maxQty = this.cancellingItemBaseQty || this.cancellingItem?.Quantity || 1;
        const num = Number(val);
        if (!isNaN(num) && num > maxQty) {
            this.cancelItemQty = maxQty;
            event.target.value = maxQty;
        }
    }

    confirmCancelSingleItem() {
        if (this.isCancellingItem || !this.cancellingItem || !this.cancelItemQty || this.cancelItemQty < 1) {
            return;
        }

        this.isCancellingItem = true;
        const itemToCancel = this.cancellingItem;
        const orig = this.currentOrder?.Items?.find((x: any) => (x.Id && itemToCancel.Id ? x.Id === itemToCancel.Id : x.ItemId === itemToCancel.ItemId));
        const baseQty = Math.max(itemToCancel.Quantity || 0, itemToCancel.OriginalQuantity || 0, orig?.Quantity || 0, orig?.OriginalQuantity || 0, this.cancellingItemBaseQty || 0);
        const qtyToCancel = Math.min(this.cancelItemQty, baseQty);
        const reasonText = this.cancelItemReason?.trim() || 'Hủy món';
        const remainingQty = Math.max(0, baseQty - qtyToCancel);
        const shouldDispatchCancellation = this.enableKitchenPrint() &&
            (itemToCancel.SentQty > 0 || itemToCancel.IsSentToKitchen === 1 || itemToCancel.OriginalQuantity > 0 || orig?.IsSentToKitchen === 1 || orig?.SentQty > 0);

        // In phiếu hủy món gửi Bếp/Bar nếu đã từng gửi bếp hoặc đang bật chế độ in bếp
        if (shouldDispatchCancellation) {
            const cancelSlip = {
                SlipType: 'Cancel',
                KitchenAreaName: itemToCancel.KitchenAreaName || orig?.KitchenAreaName || 'Bếp / Bar',
                OrderCode: this.currentOrder?.BillCode || this.currentOrder?.OrderCode || '',
                TableName: this.currentOrder?.TableName || 'Tại bàn',
                WaiterName: this.currentOrder?.WaiterName || '',
                CustomerCount: this.currentOrder?.CustomerCount || 1,
                PrintTime: new Date(),
                OrderNote: this.currentOrder?.Note || '',
                Items: [{
                    MenuItemName: itemToCancel.ItemName,
                    Quantity: qtyToCancel,
                    Note: itemToCancel.Note,
                    Reason: `Hủy món: ${reasonText}`
                }]
            };
        }

        // Cập nhật lại số lượng trong danh sách món orderItems
        if (remainingQty <= 0) {
            this.orderItems = this.orderItems.filter(x => (x.Id && itemToCancel.Id ? x.Id !== itemToCancel.Id : x !== itemToCancel));
        } else {
            let target = this.orderItems.find(x => (x.Id && itemToCancel.Id ? x.Id === itemToCancel.Id : x.ItemId === itemToCancel.ItemId));
            if (!target) {
                target = itemToCancel;
                this.orderItems.push(target);
            }
            target.Quantity = remainingQty;
            target.OriginalQuantity = remainingQty;
            if (target.SentQty) {
                target.SentQty = Math.max(0, target.SentQty - qtyToCancel);
            }
            if (target.IsChecked === 1 || (target.ActualQuantity !== undefined && target.ActualQuantity !== null)) {
                target.ActualQuantity = remainingQty;
            }
            this.recalculateItemTotal(target);
        }

        // Cập nhật original items trong currentOrder
        if (this.currentOrder?.Items) {
            if (orig) {
                if (remainingQty <= 0) {
                    this.currentOrder.Items = this.currentOrder.Items.filter((x: any) => x !== orig);
                } else {
                    orig.Quantity = remainingQty;
                    orig.OriginalQuantity = remainingQty;
                }
            }
        }

        this.recalculateAllItemTotals();
        this.cancelItemDialogVisible = false;
        this.cancellingItem = null;
        this.cancellingItemBaseQty = 0;

        // Lưu ngay lập tức xuống backend nếu là đơn đã lưu (đang phục vụ / có billId)
        if (this.currentOrder?.BillId) {
            const payload = this.getSavePayload(this.currentOrder.BillId);
            this.salesService.saveOrder(payload).subscribe({
                next: (res: any) => {
                    this.isCancellingItem = false;
                    if (res?.Code === 200) {
                        if (shouldDispatchCancellation) {
                            this.dispatchKitchenCancellations(this.currentOrder?.BillId, [{ OrderItemId: itemToCancel.Id, Quantity: qtyToCancel }], `Hủy món: ${reasonText}`);
                        }
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Hủy món thành công',
                            detail: `Đã hủy ${qtyToCancel} phần món "${itemToCancel.ItemName}" và cập nhật hóa đơn!`
                        });
                        this.loadTables();
                        const orderIdToReload = this.currentOrder?.BillId;
                        if (orderIdToReload) {
                            this.salesService.getOrderById(orderIdToReload, this.selectedBranchCode).subscribe((orderRes: any) => {
                                if (orderRes?.Code === 200 && orderRes.Data) {
                                    this.startOrderEditing(orderRes.Data);
                                }
                            });
                        }
                    } else {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Lỗi cập nhật',
                            detail: res?.Message || 'Có lỗi xảy ra khi lưu thay đổi hóa đơn.'
                        });
                    }
                },
                error: () => {
                    this.isCancellingItem = false;
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: 'Không thể kết nối đến máy chủ để cập nhật hóa đơn.'
                    });
                }
            });
        } else {
            this.isCancellingItem = false;
            this.messageService.add({
                severity: 'success',
                summary: 'Hủy món',
                detail: `Đã hủy ${qtyToCancel} phần món "${itemToCancel.ItemName}"!`
            });
        }
    }
    async orderSave() {
        if (this.isButtonDisabled('Lưu')) return;
        if (!this.currentOrder) return;

        if (this.orderItems.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một món ăn!' });
            return;
        }

        if (this.currentOrder.ServiceType === 'Delivery') {
            if (!this.phoneNumber || !this.phoneNumber.trim()) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: 'Điện thoại giao hàng bắt buộc!' });
                return;
            }
            if (!this.deliveryAddress || !this.deliveryAddress.trim()) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: 'Địa chỉ giao hàng bắt buộc!' });
                return;
            }
            const phoneRegex = /^[0-9]{8,12}$/;
            if (!phoneRegex.test(this.phoneNumber.trim())) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: 'Số điện thoại không đúng định dạng!' });
                return;
            }
        }

        const isVoucherValid = await this.validateTherapyVoucherIfNeeded();
        if (!isVoucherValid) return;

        // Chặn tuyệt đối việc giảm số lượng các món đã lưu khi sửa bill
        if (this.currentOrder?.BillId && this.currentOrder?.Status !== 'Reserved') {
            const originalItems = this.currentOrder.Items || [];
            for (const orig of originalItems) {
                const current = this.orderItems.find((x: any) => (x.Id && orig.Id ? x.Id === orig.Id : x.ItemId === orig.ItemId));
                const currentQty = current ? current.Quantity : 0;
                const origQty = orig.Quantity !== undefined ? orig.Quantity : (orig.OriginalQuantity || 0);
                if (origQty > 0 && currentQty < origQty) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Không thể lưu hóa đơn',
                        detail: `Chỉ được giảm số lượng món đã gửi bếp khi đơn ở trạng thái Đặt trước! Với đơn đang phục vụ, vui lòng dùng nút 'Hủy món' để in phiếu hủy báo Bếp/Bar. (Món: ${orig.ItemName})`
                    });
                    return;
                }
            }

            for (const item of this.orderItems) {
                const originalQty = item.OriginalQuantity !== undefined ? item.OriginalQuantity : 0;
                if (originalQty > 0 && item.Quantity < originalQty) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Không thể lưu hóa đơn',
                        detail: `Chỉ được giảm số lượng món đã gửi bếp khi đơn ở trạng thái Đặt trước! Với đơn đang phục vụ, vui lòng dùng nút 'Hủy món' để in phiếu hủy báo Bếp/Bar. (Món: ${item.ItemName})`
                    });
                    return;
                }
            }
        }

        this.openTableAndExecute((newBillId) => {
            const payload = this.getSavePayload(newBillId);
            this.salesService.saveOrder(payload).subscribe((res: any) => {
                if (res?.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Lưu hóa đơn', detail: 'Đã lưu hóa đơn thành công!' });
                    this.isEditingOrder = false;
                    this.foodDialogVisible = false;
                    this.clearSelection();
                    this.loadTables();
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Có lỗi xảy ra' });
                }
            });
        });
    }

    async orderReserve() {
        if (this.isButtonDisabled('Đặt trước')) return;
        if (!this.currentOrder) return;

        const isVoucherValid = await this.validateTherapyVoucherIfNeeded();
        if (!isVoucherValid) return;

        const proceedReserve = () => {
            this.reserveTableAndExecute((newBillId) => {
                const payload = this.getSavePayload(newBillId);
                this.salesService.saveOrder(payload).subscribe((res: any) => {
                    if (res?.Code === 200) {
                        this.messageService.add({ severity: 'success', summary: 'Đặt trước', detail: 'Đã đặt trước bàn thành công!' });
                        if (this.foodDialogVisible) {
                            this.skipCloseDrawerFromPopup = true;
                        }
                        this.foodDialogVisible = false;
                        this.salesService.getOrderById(newBillId, this.selectedBranchCode).subscribe((orderRes: any) => {
                            if (orderRes?.Code === 200 && orderRes.Data) {
                                this.startOrderEditing(orderRes.Data);
                            } else {
                                this.isEditingOrder = true;
                            }
                            this.loadTables();
                        });
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Có lỗi xảy ra' });
                    }
                });
            });
        };

        if (this.orderItems.length === 0) {
            this.confirmationService.confirm({
                message: 'Bàn đặt trước chưa có món ăn. Xác nhận mở bàn?',
                header: 'Xác nhận đặt trước bàn',
                icon: 'pi pi-exclamation-triangle',
                acceptLabel: 'Xác nhận',
                rejectLabel: 'Hủy',
                accept: () => {
                    proceedReserve();
                }
            });
        } else {
            proceedReserve();
        }
    }

    orderCheckIn() {
        if (this.isButtonDisabled('Mở bàn')) return;
        if (!this.currentOrder || !this.currentOrder.BillId) return;

        const billId = this.currentOrder.BillId;

        this.salesService.checkInOrder(billId, this.selectedBranchCode).subscribe({
            next: (res: any) => {
                if (res?.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Mở bàn', detail: 'Đã mở bàn đặt trước thành công!' });
                    this.salesService.getOrderById(billId, this.selectedBranchCode).subscribe((orderRes: any) => {
                        if (orderRes?.Code === 200 && orderRes.Data) {
                            this.startOrderEditing(orderRes.Data);
                        }
                        this.loadTables();
                    });
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không thể mở bàn đặt trước' });
                }
            },
            error: (err: any) => {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi mở bàn đặt trước' });
            }
        });
    }

    async orderSendKitchen() {
        if (this.isOrderInLockedPeriod()) {
            await this.orderSave();
            return;
        }
        const shouldSendKitchen = this.requireKitchenSend() || this.enableKitchenPrint();
        if (shouldSendKitchen && this.isButtonDisabled('Gửi bếp') && this.isButtonDisabled('Gửi Bếp/Bar')) return;
        if (this.isSendingKitchen()) return;
        if (!this.currentOrder) return;

        if (this.orderItems.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một món ăn!' });
            return;
        }

        const isVoucherValid = await this.validateTherapyVoucherIfNeeded();
        if (!isVoucherValid) return;

        this.isSendingKitchen.set(true);

        this.openTableAndExecute(
            (newBillId) => {
                const payload = this.getSavePayload(newBillId);
                this.salesService.saveOrder(payload).subscribe({
                    next: (saveRes: any) => {
                        if (saveRes?.Code === 200) {
                            const isWaiting = this.currentOrder?.Status === 'WaitingConfirm';
                            if (!shouldSendKitchen) {
                                if (isWaiting) {
                                    this.salesService.sendOrderToKitchenBar(newBillId, this.selectedBranchCode).subscribe({
                                        next: (res: any) => {
                                            this.isSendingKitchen.set(false);
                                            if (res?.Code === 200) {
                                                this.showPrintDispatchResults(res);
                                                if (this.enableKitchenPrint() && res.Data?.PrintSlips && res.Data.PrintSlips.length > 0) {
                                                    this.printKitchenSlips(res.Data.PrintSlips);
                                                }
                                                this.messageService.add({ severity: 'success', summary: 'Duyệt đơn', detail: 'Đã duyệt thành công!' });
                                                this.isEditingOrder = false;
                                                this.foodDialogVisible = false;
                                                this.clearSelection();
                                                this.loadTables();
                                            } else {
                                                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Có lỗi xảy ra' });
                                            }
                                        },
                                        error: (err: any) => {
                                            this.isSendingKitchen.set(false);
                                            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi gửi bếp/bar' });
                                        }
                                    });
                                } else {
                                    const msg = !this.currentOrder?.BillId ? 'Đã mở bàn thành công!' : 'Đã lưu thành công!';
                                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: msg });

                                    this.salesService.getOrderById(newBillId, this.selectedBranchCode).subscribe({
                                        next: (orderRes: any) => {
                                            if (orderRes?.Code === 200 && orderRes.Data) {
                                                const serviceType = orderRes.Data.ServiceType;
                                                const isNonDineIn = serviceType === 'TakeAway' || serviceType === 'Delivery';
                                                if (isNonDineIn) {
                                                    this.isEditingOrder = false;
                                                    this.foodDialogVisible = false;
                                                    this.clearSelection();
                                                } else {
                                                    if (this.foodDialogVisible) {
                                                        this.skipCloseDrawerFromPopup = true;
                                                    }
                                                    this.foodDialogVisible = false;
                                                    this.startOrderEditing(orderRes.Data);
                                                }
                                            }
                                            this.loadTables();
                                            this.isSendingKitchen.set(false);
                                        },
                                        error: (err: any) => {
                                            this.isSendingKitchen.set(false);
                                            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi tải thông tin đơn hàng' });
                                        }
                                    });
                                }
                            } else {
                                this.salesService.sendOrderToKitchenBar(newBillId, this.selectedBranchCode).subscribe({
                                    next: (res: any) => {
                                        if (res?.Code === 200) {
                                            this.showPrintDispatchResults(res);
                                            if (this.enableKitchenPrint() && res.Data?.PrintSlips?.length > 0) {
                                                this.printKitchenSlips(res.Data.PrintSlips);
                                            }
                                            this.messageService.add({
                                                severity: 'success',
                                                summary: isWaiting ? 'Duyệt đơn' : 'Gửi bếp/bar',
                                                detail: isWaiting ? 'Đã duyệt và gửi bếp/bar thành công!' : 'Đã gửi bếp/bar thành công!'
                                            });

                                            this.salesService.getOrderById(newBillId, this.selectedBranchCode).subscribe({
                                                next: (orderRes: any) => {
                                                    if (orderRes?.Code === 200 && orderRes.Data) {
                                                        const serviceType = orderRes.Data.ServiceType;
                                                        const isNonDineIn = serviceType === 'TakeAway' || serviceType === 'Delivery';
                                                        if (isNonDineIn || isWaiting) {
                                                            this.isEditingOrder = false;
                                                            this.foodDialogVisible = false;
                                                            this.clearSelection();
                                                        } else {
                                                            if (this.foodDialogVisible) {
                                                                this.skipCloseDrawerFromPopup = true;
                                                            }
                                                            this.foodDialogVisible = false;
                                                            this.startOrderEditing(orderRes.Data);
                                                        }
                                                    }
                                                    this.loadTables();
                                                    this.isSendingKitchen.set(false);
                                                },
                                                error: (err: any) => {
                                                    this.isSendingKitchen.set(false);
                                                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi tải thông tin đơn hàng' });
                                                }
                                            });
                                        } else {
                                            this.isSendingKitchen.set(false);
                                            this.messageService.add({ severity: 'error', summary: 'Lỗi gửi bếp/bar', detail: res.Message });
                                        }
                                    },
                                    error: (err: any) => {
                                        this.isSendingKitchen.set(false);
                                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi gửi bếp/bar' });
                                    }
                                });
                            }
                        } else {
                            this.isSendingKitchen.set(false);
                            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: saveRes.Message || 'Lưu hóa đơn thất bại' });
                        }
                    },
                    error: (err: any) => {
                        this.isSendingKitchen.set(false);
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.message || 'Có lỗi xảy ra khi lưu đơn hàng' });
                    }
                });
            },
            true,
            () => {
                this.isSendingKitchen.set(false);
            }
        );
    }

    orderPrintTemporary() {
        this.openEInvoicePrompt('order');
    }

    openEInvoicePrompt(type: 'order' | 'check' | 'payment' | 'card', data?: any) {
        if (type === 'order') {
            if (this.isButtonDisabled('In tạm tính')) return;
            if (!this.currentOrder) return;
            if (this.orderItems.length === 0) {
                this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một món ăn!' });
                return;
            }
        } else if (type === 'check') {
            if (!this.checkBillDetail) return;
        } else if (type === 'payment') {
            if (!this.currentOrder) return;
        } else if (type === 'card') {
            if (!data || !(data.OrderId || data.Id || data.BillId)) return;
        }

        const targetBranch = (type === 'card' && data?.BranchCode) ? data.BranchCode : this.selectedBranchCode;
        if (!this.isCurrentBranchEInvoiceApplied(targetBranch)) {
            if (type === 'order') {
                this.performOrderPrintTemporary(false);
            } else if (type === 'check') {
                this.performCheckPrintTemporaryBill(false);
            } else if (type === 'payment') {
                this.performPaymentPrintTemporary(false);
            } else if (type === 'card') {
                this.performCardPrintTemporary(data, false);
            }
            return;
        }

        this.pendingPrintContext = { type, data };
        this.eInvoiceDialogVisible = true;
    }

    closeEInvoicePrompt() {
        this.eInvoiceDialogVisible = false;
        this.pendingPrintContext = null;
    }

    executePrintTemporaryBill(showInvoiceQr: boolean) {
        const context = this.pendingPrintContext;
        this.eInvoiceDialogVisible = false;
        this.pendingPrintContext = null;

        if (!context) return;

        if (context.type === 'order') {
            this.performOrderPrintTemporary(showInvoiceQr);
        } else if (context.type === 'check') {
            this.performCheckPrintTemporaryBill(showInvoiceQr);
        } else if (context.type === 'payment') {
            this.performPaymentPrintTemporary(showInvoiceQr);
        } else if (context.type === 'card') {
            this.performCardPrintTemporary(context.data, showInvoiceQr);
        }
    }

    async performOrderPrintTemporary(showInvoiceQr: boolean = true) {
        if (this.isButtonDisabled('In tạm tính')) return;
        if (!this.currentOrder) return;

        if (this.orderItems.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một món ăn!' });
            return;
        }

        const isVoucherValid = await this.validateTherapyVoucherIfNeeded();
        if (!isVoucherValid) return;

        this.openTableAndExecute((newBillId) => {
            const payload = this.getSavePayload(newBillId);
            this.salesService.saveOrder(payload).subscribe((saveRes: any) => {
                if (saveRes?.Code === 200) {
                    this.salesService.printTemporaryBill(newBillId, this.selectedBranchCode).subscribe((res: any) => {
                        if (res?.Code === 200) {
                            this.salesService.getOrderById(newBillId, this.selectedBranchCode).subscribe((orderRes: any) => {
                                if (orderRes?.Code === 200 && orderRes.Data) {
                                    const updatedOrder = orderRes.Data;
                                    const shippingFee = (updatedOrder.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'Delivery' || this.checkBillDetail?.ServiceType === 'Delivery') ? this.parseShippingFee(updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote) : 0;
                                    const totalWithShipping = (updatedOrder.TotalAmount || 0) + shippingFee;

                                    const printOrder = {
                                        ServiceType: updatedOrder.ServiceType || this.currentOrder?.ServiceType || this.checkBillDetail?.ServiceType,
                                        ShippingFeeNote: updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote,
                                        TableName: updatedOrder.TableName || (updatedOrder.Tables && updatedOrder.Tables.length > 0 ? updatedOrder.Tables.map((t: any) => t.TableName || t.Name).join(' + ') : '') || this.getFirstSelectedTable()?.TableName || this.currentOrder?.TableName || (updatedOrder.ServiceType === 'TakeAway' ? 'Mang về' : (updatedOrder.ServiceType === 'Delivery' ? 'Giao hàng' : '')),
                                        OpenTime: updatedOrder.OpenTime,
                                        WaiterName: updatedOrder.WaiterName,
                                        BillCode: updatedOrder.OrderCode,
                                        Items: (updatedOrder.Items || []).map((item: any) => ({
                                            ItemId: item.ItemId,
                                            ItemName: item.ItemName,
                                            Quantity: this.getActualUsedQty(item),
                                            Price: item.UnitPrice || 0,
                                            DiscountRate: item.DiscountRate || 0,
                                            DiscountAmount: item.DiscountAmount || 0,
                                            VatRate: item.VatRate || 0,
                                            VatAmount: item.VatAmount,
                                            FixedTaxAmount: item.FixedTaxAmount,
                                            Note: item.Note || '',
                                            SelectedOptions: item.SelectedOptions || []
                                        }))
                                    };
                                    this.printTemporaryBillWithQr(printOrder, newBillId, (qrUrl) => {
                                        this.printReceipt(printOrder, 'Bill', qrUrl, res.Data, showInvoiceQr);
                                        this.messageService.add({ severity: 'success', summary: 'In tạm tính', detail: 'Đã in tạm tính thành công! Trạng thái bàn chuyển sang Chờ thanh toán.' });
                                        this.isEditingOrder = false;
                                        this.clearSelection();
                                        this.loadTables();
                                        this.loadOrderList();
                                    }, totalWithShipping);
                                }
                            });
                        } else {
                            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                        }
                    });
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: saveRes.Message || 'Lưu hóa đơn thất bại' });
                }
            });
        });
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

        const imgs = iframeDoc.images;
        if (imgs && imgs.length > 0) {
            let loaded = 0;
            const total = imgs.length;
            const checkDone = () => {
                loaded++;
                if (loaded >= total) setTimeout(triggerPrint, 100);
            };
            for (let i = 0; i < total; i++) {
                if (imgs[i].complete) {
                    loaded++;
                } else {
                    imgs[i].onload = checkDone;
                    imgs[i].onerror = checkDone;
                }
            }
            if (loaded >= total) {
                setTimeout(triggerPrint, 250);
            } else {
                setTimeout(triggerPrint, 1500);
            }
        } else {
            iframeWin.onload = triggerPrint;
            setTimeout(triggerPrint, 350);
        }
    }

    private showPrintDispatchResults(response: any) {
        const results = response?.Data?.PrintDispatchResults || [];
        if (!results.length) return;
        const failed = results.filter((result: any) => result.Status === 'Failed');
        const details = results.map((result: any) => `${result.KitchenAreaName}: ${result.PrinterName || 'chưa có máy in'} — ${result.Message || result.Status}`).join(' · ');
        this.messageService.add({
            severity: failed.length ? 'warn' : 'info',
            summary: failed.length ? 'Có phiếu chưa in được' : 'Đã gửi phiếu đến máy in',
            detail: details
        });
    }

    private dispatchKitchenCancellations(orderId: string | undefined, items: any[], reason: string): void {
        if (!this.enableKitchenPrint() || !orderId || !items?.length) return;
        const printItems = items
            .filter((item: any) => item?.OrderItemId && item.Quantity > 0)
            .map((item: any) => ({ OrderItemId: item.OrderItemId, Quantity: item.Quantity }));
        if (!printItems.length) return;

        this.salesService.dispatchKitchenCancellations({
            OrderId: orderId,
            BranchCode: this.selectedBranchCode,
            Reason: reason,
            Items: printItems
        }).subscribe({
            next: (response: any) => {
                if (response?.Code !== 200) {
                    this.messageService.add({ severity: 'warn', summary: 'Chưa gửi được phiếu hủy', detail: response?.Message || 'Kiểm tra cấu hình máy in Bếp/Bar.' });
                    return;
                }
                this.showPrintDispatchResults(response);
                if (response.Data?.PrintSlips?.length) this.printKitchenSlips(response.Data.PrintSlips);
            },
            error: (error: any) => this.messageService.add({
                severity: 'warn',
                summary: 'Chưa gửi được phiếu hủy',
                detail: error?.error?.Message || error?.error?.message || 'Kiểm tra kết nối backend và cấu hình máy in.'
            })
        });
    }

    printKitchenSlips(slips: any[], fallbackOrder?: any) {
        if (!this.enableKitchenPrint()) return;
        if (!slips || slips.length === 0) return;

        let processedSlips = [...slips];

        // If kitchen-print-by-area is disabled (false), merge slips of the same type
        if (!this.kitchenPrintByArea()) {
            const merged: any[] = [];
            const kitchenSlips = processedSlips.filter(s => s.SlipType !== 'Cancel');
            const cancelSlips = processedSlips.filter(s => s.SlipType === 'Cancel');

            if (kitchenSlips.length > 0) {
                const first = kitchenSlips[0];
                const allItems: any[] = [];
                kitchenSlips.forEach(s => allItems.push(...(s.Items || [])));
                merged.push({
                    ...first,
                    KitchenAreaName: 'Bếp / Bar',
                    Items: allItems
                });
            }

            if (cancelSlips.length > 0) {
                const first = cancelSlips[0];
                const allItems: any[] = [];
                cancelSlips.forEach(s => allItems.push(...(s.Items || [])));
                merged.push({
                    ...first,
                    KitchenAreaName: 'Bếp / Bar',
                    Items: allItems
                });
            }

            processedSlips = merged;
        }

        let slipsHtml = '';

        processedSlips.forEach((slip: any, index: number) => {
            const isCancel = slip.SlipType === 'Cancel';
            const areaName = slip.KitchenAreaName || '';
            let title = 'PHIẾU GỬI BẾP';
            if (isCancel) {
                title = 'PHIẾU HUỶ MÓN';
            } else if (areaName) {
                const upperArea = areaName.toUpperCase();
                if (upperArea.includes('BAR')) {
                    title = 'PHIẾU BÁO BAR';
                } else if (upperArea.includes('BẾP')) {
                    title = `PHIẾU BÁO ${upperArea}`;
                } else {
                    title = `PHIẾU GỬI BẾP - ${upperArea}`;
                }
            }

            let rawTable = slip.TableName || (fallbackOrder?.TableName || this.getFirstSelectedTable()?.TableName || 'Tại bàn');
            if (!rawTable.includes(' - ') && !rawTable.includes('Sàn:')) {
                const floorName = this.activeFloor?.Name || (fallbackOrder?.FloorName || '');
                const branch = slip.BranchCode || this.selectedBranchCode || '';
                const parts = [rawTable];
                if (floorName) parts.push(`Sàn: ${floorName}`);
                if (branch) parts.push(branch);
                rawTable = parts.join(' - ');
            }
            const tableFormatted = rawTable.replace(/ - /g, ' -<br>');

            const d = slip.PrintTime ? new Date(slip.PrintTime) : new Date();
            const timeStr = String(d.getHours()).padStart(2, '0') + ':' +
                String(d.getMinutes()).padStart(2, '0') + ':' +
                String(d.getSeconds()).padStart(2, '0') + ' ' +
                d.getDate() + '/' + (d.getMonth() + 1) + '/' + d.getFullYear();

            const orderNote = slip.OrderNote || this.orderNote || fallbackOrder?.Note || '';

            let itemsRows = '';
            (slip.Items || []).forEach((item: any) => {
                const itemName = item.MenuItemName || item.ItemName || 'Món';
                const qty = item.Quantity || 0;
                const note = item.Note || '';
                const reason = item.Reason || '';

                itemsRows += `
                    <tr style="border-bottom: 1px dashed #000; font-size: 13px;">
                        <td style="padding: 6px 4px 6px 0; font-weight: 700; color: #000; vertical-align: middle; width: 48%;">
                            <div>${itemName}</div>
                            ${(reason || note) ? `
                            <div style="font-size: 13px; font-weight: 700; font-style: italic; color: #000; margin-top: 2px;">
                                * ${reason ? reason + (note ? ' - ' + note : '') : note}
                            </div>
                            ` : ''}
                        </td>
                        <td style="padding: 6px 4px; font-weight: 700; text-align: center; color: #000; vertical-align: middle; width: 38%; font-size: 12px; line-height: 1.3;">
                            ${tableFormatted}
                        </td>
                        <td style="padding: 6px 0 6px 4px; font-size: 18px; font-weight: 900; text-align: right; color: #000; vertical-align: middle; width: 14%; padding-right: 4px;">
                            ${qty}
                        </td>
                    </tr>
                `;
            });

            slipsHtml += `
                <div class="ticket-slip ${index < processedSlips.length - 1 ? 'has-page-break' : ''}">
                    <div class="text-center">
                        <div class="title">${title}</div>
                    </div>
                    <div class="divider"></div>
                    <div class="info-row">
                        <span>Thời gian: ${timeStr}</span>
                    </div>
                    <div class="divider"></div>
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="border-bottom: 2px solid #000; font-weight: 900; font-size: 13px; color: #000;">
                                <td style="text-align: left; padding-bottom: 4px; width: 48%;">Tên món</td>
                                <td style="text-align: center; padding-bottom: 4px; width: 38%;">Bàn</td>
                                <td style="text-align: right; padding-bottom: 4px; width: 14%; padding-right: 4px;">SL</td>
                            </tr>
                        </thead>
                        <tbody>
                            ${itemsRows}
                        </tbody>
                    </table>
                    <div class="divider-dashed"></div>
                    <div class="divider"></div>
                    ${orderNote ? `
                    <div style="font-size: 13px; font-weight: 800; padding: 4px 0; color: #000;">
                        * Ghi chú đơn: ${orderNote}
                    </div>
                    <div class="divider"></div>
                    ` : ''}
                    <div class="text-center" style="margin-top: 8px; font-size: 14px; font-weight: 900; color: #000;">
                        --- HẾT ---
                    </div>

                    <!-- Paper Feed spacer for thermal cutter blade -->
                    <div style="height: 35mm; min-height: 35mm; clear: both; line-height: 35mm;">&nbsp;</div>
                </div>
            `;
        });

        const fullHtml = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>Phiếu Bếp / Bar</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
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
                        background-color: #fff;
                    }
                    body {
                        font-family: 'Inter', Arial, sans-serif;
                        font-size: 13px;
                        line-height: 1.35;
                        color: #000;
                    }
                    .ticket-slip {
                        width: 80mm;
                        padding: 0 4px 4px 4px;
                        box-sizing: border-box;
                    }
                    .has-page-break {
                        page-break-after: always;
                        break-after: page;
                        border-bottom: 1px dashed #999;
                        margin-bottom: 20px;
                    }
                    .text-center { text-align: center; }
                    .title {
                        font-size: 18px;
                        font-weight: 900;
                        text-transform: uppercase;
                        margin-top: 0;
                        margin-bottom: 4px;
                        color: #000;
                        letter-spacing: 0.5px;
                    }
                    .divider {
                        border-top: 2px solid #000;
                        margin: 6px 0;
                    }
                    .divider-dashed {
                        border-top: 1px dashed #000;
                        margin: 6px 0 4px 0;
                    }
                    .info-row {
                        font-size: 12px;
                        font-weight: 700;
                        margin-bottom: 2px;
                        color: #000;
                    }
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
                        .ticket-slip {
                            width: 80mm;
                            padding: 0 4px 4px 4px;
                        }
                        .has-page-break {
                            border-bottom: none;
                            margin-bottom: 0;
                        }
                    }
                </style>
            </head>
            <body>
                ${slipsHtml}
            </body>
            </html>
        `;

        this.printHtmlViaHiddenIframe(fullHtml);
    }

    printReceipt(order: any, type: 'Kitchen' | 'Bill', qrUrl?: string, invoiceRegisterUrl?: string, showInvoiceQr: boolean = true) {
        const dateStr = new Date().toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });
        const checkInTime = order.OpenTime ? new Date(order.OpenTime).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : dateStr;
        const loggedInName = this.currentEmployee()?.Name 
            || this.currentEmployee()?.FullName 
            || this.currentUser()?.FullName 
            || this.currentUser()?.fullName 
            || this.currentUser()?.Name 
            || this.currentUser()?.name 
            || this.currentUser()?.UserName 
            || this.currentUser()?.userName 
            || (this.currentUser()?.email ? this.currentUser().email.split('@')[0] : '');

        const waiter = loggedInName 
            || order.CashierUserName 
            || order.CashierName 
            || (this.orderWaiterId ? this.waiters.find(w => w.Id === this.orderWaiterId)?.Name : null) 
            || order.WaiterName 
            || order.CreatedByUserName 
            || 'N/A';
        const tableName = order.TableName 
            || (order.Tables && order.Tables.length > 0 ? order.Tables.map((t: any) => t.TableName || t.Name || t.TableCode).join(' + ') : '') 
            || order.TableNames 
            || order.TableInfo 
            || this.getFirstSelectedTable()?.TableName 
            || this.currentOrder?.TableName 
            || (order.ServiceType === 'TakeAway' ? 'Mang về' : (order.ServiceType === 'Delivery' ? 'Giao hàng' : 'N/A'));
        const items = order.Items || this.orderItems;

        let contentHtml = '';
        if (type === 'Bill') {
            // Vietnamese Number to Words converter
            const docSoTien = (soTien: number): string => {
                if (soTien === 0) return 'Không đồng';
                const ChuSo = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];
                const Tien = ["", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ"];

                function DocBlock(so: number, day: boolean): string {
                    let chuoi = "";
                    const tram = Math.floor(so / 100);
                    const chuc = Math.floor((so % 100) / 10);
                    const donvi = so % 10;

                    if (tram > 0 || day) {
                        chuoi += ChuSo[tram] + " trăm ";
                    }

                    if (chuc > 0) {
                        if (chuc === 1) chuoi += "mười ";
                        else chuoi += ChuSo[chuc] + " mươi ";
                    } else if (donvi > 0 && (tram > 0 || day)) {
                        chuoi += "lẻ ";
                    }

                    if (donvi > 0) {
                        if (donvi === 1 && chuc > 1) chuoi += "mốt";
                        else if (donvi === 5 && chuc > 0) chuoi += "lăm";
                        else chuoi += ChuSo[donvi];
                    }
                    return chuoi;
                }

                let viTri = 0;
                let chuoi = "";
                let temp = Math.round(soTien);

                if (temp < 0) return "Trừ " + docSoTien(-temp);

                const blocks: number[] = [];
                while (temp > 0) {
                    blocks.push(temp % 1000);
                    temp = Math.floor(temp / 1000);
                }

                for (let i = blocks.length - 1; i >= 0; i--) {
                    const block = blocks[i];
                    if (block > 0) {
                        const hienThi = DocBlock(block, i < blocks.length - 1);
                        chuoi += hienThi + " " + Tien[i] + " ";
                    }
                }

                chuoi = chuoi.trim();
                if (chuoi.length > 0) {
                    chuoi = chuoi.substring(0, 1).toUpperCase() + chuoi.substring(1) + " đồng chẵn.";
                }
                return chuoi.replace(/\s+/g, ' ');
            };

            let subtotal = 0;
            let discount = 0;
            let vat = 0;
            let total = 0;

            const calcResult = OrderCalculator.calculateTaxGroups(items, item => this.getActualUsedQty(item));
            const taxGroups = calcResult.taxGroups;
            subtotal = calcResult.subtotal;
            discount = calcResult.discount;
            vat = calcResult.vat;
            total = calcResult.total;

            const isDeliveryOrder = order.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'Delivery' || (this.checkBillDetail?.ServiceType === 'Delivery');
            const shippingFeeAmt = isDeliveryOrder ? this.parseShippingFee(order.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote || this.checkBillDetail?.ShippingFeeNote) : 0;
            total += shippingFeeAmt;

            // Group VAT breakdown
            const group8 = taxGroups['8%'] || { rate: 8, subtotal: 0, vatAmt: 0, totalAmt: 0 };
            const group10 = taxGroups['10%'] || { rate: 10, subtotal: 0, vatAmt: 0, totalAmt: 0 };

            let otherSubtotal = 0;
            let otherVatAmt = 0;
            let otherTotalAmt = 0;
            Object.keys(taxGroups).forEach(k => {
                if (k !== '8%' && k !== '10%') {
                    otherSubtotal += taxGroups[k].subtotal;
                    otherVatAmt += taxGroups[k].vatAmt;
                    otherTotalAmt += taxGroups[k].totalAmt;
                }
            });

            let itemsHtml = '';
            let lineIdx = 0;
            const escapeReceiptText = (value: unknown) => String(value ?? '').replace(/[&<>"']/g, char =>
                ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char] || char);
            items.forEach((item: any) => {
                const qty = this.getActualUsedQty(item);
                if (qty <= 0) return;
                lineIdx++;
                const unitPrice = item.Price !== undefined ? item.Price : (item.UnitPrice !== undefined ? item.UnitPrice : 0);
                const fixedTax = (item.FixedTaxAmount !== undefined && item.FixedTaxAmount !== null && Number(item.FixedTaxAmount) > 0)
                    ? Number(item.FixedTaxAmount)
                    : (item.fixedTaxAmount !== undefined && item.fixedTaxAmount !== null && Number(item.fixedTaxAmount) > 0 ? Number(item.fixedTaxAmount) : 0);
                const vatRate = Number(item.VatRate || 0);

                const selectedOptions = item.SelectedOptions || [];
                const optionCharges = selectedOptions.map((option: any) => Number(option.ExtraCharge || 0));
                const optionTaxes = selectedOptions.map((option: any) => {
                    const rawPercentTax = Number(option.ExtraCharge || 0) * vatRate / 100;
                    const percentTax = vatRate > 0 ? Math.sign(rawPercentTax) * Math.round(Math.abs(rawPercentTax)) : 0;
                    const fixed = (option.FixedTax !== null && option.FixedTax !== undefined && option.FixedTax !== '')
                        ? (Number(option.FixedTax) || 0) : 0;
                    return fixed !== 0 ? fixed : percentTax;
                });
                const optionTaxedPrices = optionCharges.map((charge: number, idx: number) => charge + optionTaxes[idx]);
                const basePrice = Math.max(0, Number(unitPrice) - optionCharges.reduce((sum: number, charge: number) => sum + charge, 0));
                const baseTax = fixedTax > 0 ? Math.round(fixedTax) : (vatRate > 0 ? Math.round(basePrice * vatRate / 100) : 0);
                const baseDisplayPrice = basePrice + baseTax;
                const unitTax = baseTax + optionTaxes.reduce((sum: number, tax: number) => sum + tax, 0);
                const displayUnitPrice = unitPrice + unitTax;
                if (optionTaxedPrices.length) {
                    optionTaxedPrices[optionTaxedPrices.length - 1] += displayUnitPrice - baseDisplayPrice
                        - optionTaxedPrices.reduce((sum: number, price: number) => sum + price, 0);
                }

                let unitDiscount = 0;
                let discountPercent = 0;
                if (item.DiscountRate !== undefined && item.DiscountRate !== null && Number(item.DiscountRate) > 0) {
                    discountPercent = Number(item.DiscountRate);
                    unitDiscount = Math.round(displayUnitPrice * (discountPercent / 100));
                } else if (item.DiscountAmount !== undefined && item.DiscountAmount !== null && item.DiscountAmount > 0 && qty > 0) {
                    unitDiscount = Math.round(item.DiscountAmount / qty);
                    if (displayUnitPrice > 0) {
                        discountPercent = Math.round((unitDiscount / displayUnitPrice) * 100);
                    }
                }

                const unitTotal = Math.max(0, displayUnitPrice - unitDiscount);
                const lineTotal = Math.round(unitTotal * qty);
                const discountPercentStr = discountPercent > 0 ? `${discountPercent}%` : '0%';

                const itemNote = item.Note || item.note || item.ItemNote || item.NoteText || item.Notes || '';
                const optionGroups = new Map<string, { name: string; values: string[]; price: number }>();
                selectedOptions.forEach((option: any, index: number) => {
                    const key = String(option.GroupCode || option.GroupName || '').toLowerCase();
                    const group = optionGroups.get(key) || { name: option.GroupName || option.GroupCode || '', values: [] as string[], price: 0 };
                    group.values.push(option.ValueName || option.ValueCode || '');
                    group.price += optionTaxedPrices[index];
                    optionGroups.set(key, group);
                });
                const optionRows = Array.from(optionGroups.values()).map(group => {
                    return `<tr style="font-size: 9px; color: #000; font-weight: 500; -webkit-print-color-adjust: exact; print-color-adjust: exact;">
                        <td></td>
                        <td style="text-align: left; padding: 2px 0 3px 5px; color: #000; font-weight: 500;">+ ${escapeReceiptText(group.name)}: ${escapeReceiptText(group.values.join(', '))}</td>
                        <td></td>
                        <td style="text-align: right; padding: 2px 0 3px 0; white-space: nowrap; color: #000; font-weight: 500;">+${group.price.toLocaleString('vi-VN')}</td>
                        <td></td><td></td>
                    </tr>`;
                }).join('');
                itemsHtml += `
                    <tr>
                        <td style="text-align: left; padding: 3px 0; vertical-align: top;">${lineIdx}</td>
                        <td style="text-align: left; padding: 3px 0; font-weight: 500;">
                            ${item.ItemName || item.MenuItemName}
                            ${itemNote ? `<br><span style="font-size: 10px; font-weight: 700; color: #000;">* Ghi chú: ${itemNote}</span>` : ''}
                        </td>
                        <td style="text-align: center; padding: 3px 0; vertical-align: top;">${qty}</td>
                        <td style="text-align: right; padding: 3px 0; vertical-align: top;">${baseDisplayPrice.toLocaleString('vi-VN')}</td>
                        <td style="text-align: right; padding: 3px 0; vertical-align: top;">${discountPercentStr}</td>
                        <td style="text-align: right; padding: 3px 0; vertical-align: top; font-weight: 500;">
                            ${lineTotal.toLocaleString('vi-VN')}
                        </td>
                    </tr>
                    ${optionRows}
                `;
            });

            // Collect applied promotions
            let appliedPromoNames: string[] = [];
            const promoCode = order.SelectedPromotionCode || this.selectedPromotionCode;
            if (promoCode) {
                const promo = (this.promotionsList() || []).find((p: any) => p.Code === promoCode);
                const pName = promo ? (promo.Name || promo.Description) : null;
                if (pName) {
                    appliedPromoNames.push(pName);
                } else if (order.DiscountLabel) {
                    appliedPromoNames.push(order.DiscountLabel);
                } else {
                    appliedPromoNames.push(`Khuyến mại: ${promoCode}`);
                }
            } else if (order.DiscountLabel) {
                appliedPromoNames.push(order.DiscountLabel);
            }
            (order.Items || []).forEach((item: any) => {
                const itemPromoCode = item.PromotionCode || item.promotionCode;
                if (itemPromoCode) {
                    const promo = (this.promotionsList() || []).find((p: any) => p.Code === itemPromoCode);
                    const pName = promo ? (promo.Name || promo.Description) : null;
                    if (pName && !appliedPromoNames.includes(pName)) {
                        appliedPromoNames.push(pName);
                    } else if (!pName && !appliedPromoNames.some(x => x.includes(itemPromoCode))) {
                        appliedPromoNames.push(`Khuyến mại: ${itemPromoCode}`);
                    }
                }
            });

            // Order-level note
            const mainOrderNote = order.Note || this.orderNote || order.OrderNote || '';

            // Customer registration URL for e-invoice
            let landingUrl = invoiceRegisterUrl;
            if (!landingUrl) {
                landingUrl = environment.eInvoiceLandingUrl || '/register-invoice/';
                if (landingUrl.startsWith('/')) {
                    landingUrl = `${window.location.protocol}//${window.location.host}${landingUrl}`;
                }
                landingUrl = `${landingUrl}${order.OrderId || order.Id || ''}`;
            }
            const invoiceQrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=100x100&data=${encodeURIComponent(landingUrl)}`;

            const formattedTotalWords = docSoTien(total);

            contentHtml = `
                <html>
                <head>
                  <title>Hóa Đơn Thanh Toán</title>
                  <link rel="preconnect" href="https://fonts.googleapis.com">
                  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
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
                      font-family: 'Inter', sans-serif;
                      font-size: 9.5px;
                      line-height: 1.35;
                      padding: 0 4px 4px 4px;
                      box-sizing: border-box;
                      color: #000;
                    }
                    .text-center { text-align: center; }
                    .text-right { text-align: right; }
                    .bold { font-weight: 700; }
                    .divider { border-top: 1px dashed #000; margin: 5px 0; }
                    .double-divider { border-top: 1px solid #000; border-bottom: 1px solid #000; height: 2px; margin: 5px 0; }

                    .store-title { font-size: 11px; font-weight: 700; text-transform: uppercase; margin-top: 0; margin-bottom: 2px; }
                    .bill-title { font-size: 13px; font-weight: 800; text-transform: uppercase; margin: 4px 0; letter-spacing: 0.5px; }

                    .info-table { width: 100%; border-collapse: collapse; margin-bottom: 4px; font-size: 9px; }
                    .info-table td { padding: 1px 0; }

                    .items-table { width: 100%; border-collapse: collapse; margin: 4px 0; font-size: 9px; }
                    .items-table th { font-weight: 700; border-bottom: 1px dashed #000; padding: 4px 0; text-align: left; }
                    .items-table td { padding: 3px 0; }

                    .summary-table { width: 100%; border-collapse: collapse; font-size: 10px; margin-top: 4px; }
                    .summary-table th { font-weight: 700; padding: 2px 3px; text-align: right; white-space: nowrap; }
                    .summary-table td { padding: 2px 3px; text-align: right; white-space: nowrap; }

                    .total-box { display: flex; justify-content: space-between; font-weight: 800; font-size: 12px; margin: 5px 0 2px 0; }
                    .words-text { font-size: 10px; font-style: italic; margin-bottom: 4px; line-height: 1.2; }

                    .qr-section { margin: 8px 0; display: flex; align-items: center; gap: 8px; page-break-inside: avoid; }
                    .qr-copy { flex: 1; min-width: 0; text-align: left; }
                    .qr-title { font-size: 11px; font-weight: 800; text-transform: uppercase; margin-bottom: 4px; color: #000; }
                    .qr-sub { font-size: 10px; color: #000; font-weight: 500; line-height: 1.3; }
                    .qr-img { width: 110px; height: 110px; flex: 0 0 110px; object-fit: contain; }

                    .invoice-card-black {
                      border: 1px solid #000;
                      border-radius: 4px;
                      padding: 6px;
                      margin: 6px 0;
                      background: #fff;
                      display: flex;
                      align-items: center;
                      gap: 8px;
                      page-break-inside: avoid;
                    }
                    .invoice-card-black .qr-title { color: #000; font-size: 11px; font-weight: 800; margin-bottom: 3px; letter-spacing: 0.5px; }
                    .invoice-card-black .qr-sub { color: #000; font-size: 9px; font-weight: 500; line-height: 1.25; }
                    .invoice-card-black .qr-img { width: 105px; height: 105px; flex-basis: 105px; }

                    .footer-section { font-size: 9px; font-style: italic; margin-top: 6px; margin-bottom: 4px; line-height: 1.3; }

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
                        padding: 0 4px 4px 4px;
                      }
                      .no-print { display: none; }
                    }
                  </style>
                </head>
                <body>
                  <div class="text-center">
                    <div class="store-title">${order.BranchName || this.branchUser()[0]?.Name || 'AN FOOD'}</div>
                    <div class="bill-title">HÓA ĐƠN THANH TOÁN</div>
                  </div>

                  <table class="info-table">
                    <tr>
                      <td style="width: 35%;">Số HĐ:</td>
                      <td style="width: 65%; font-weight: 600;">${order.BillCode || order.OrderCode || 'N/A'}</td>
                    </tr>
                    <tr>
                      <td>Bàn:</td>
                      <td style="font-weight: 700;">${tableName} &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Số khách: ${order.CustomerCount || this.orderCustomerCount || 1}</td>
                    </tr>
                    <tr>
                      <td>Ngày giờ:</td>
                      <td>${checkInTime}</td>
                    </tr>
                    <tr>
                      <td>Thu ngân:</td>
                      <td>${waiter}</td>
                    </tr>
                  </table>

                  <div class="divider"></div>

                  <table class="items-table">
                    <thead>
                      <tr>
                        <th style="width: 7%; text-align: left;">STT</th>
                        <th style="width: 35%; text-align: left;">Tên món</th>
                        <th style="width: 8%; text-align: center;">SL</th>
                        <th style="width: 18%; text-align: right;">Đơn giá</th>
                        <th style="width: 12%; text-align: right;">%KM</th>
                        <th style="width: 20%; text-align: right;">Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      ${itemsHtml}
                    </tbody>
                  </table>

                  ${mainOrderNote ? `
                  <div style="font-size: 9.5px; font-weight: 800; color: #000; border: 1px solid #000; padding: 4px 6px; margin: 5px 0; border-radius: 4px;">
                    * GHI CHÚ ĐƠN: ${mainOrderNote}
                  </div>
                  ` : ''}

                  <div class="divider"></div>

                  <div class="bold" style="font-size: 9.5px; margin-bottom: 2px;">TỔNG HỢP</div>
                  <table class="summary-table">
                    <thead>
                      <tr style="border-bottom: 1px solid #ccc;">
                        <th style="text-align: left; width: 28%;"></th>
                        <th style="width: 22%;">Trước thuế</th>
                        <th style="width: 22%;">Thuế GTGT</th>
                        <th style="width: 28%;">Cộng tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td style="text-align: left;">Thuế suất 8%</td>
                        <td>${group8.subtotal.toLocaleString('vi-VN')}</td>
                        <td>${group8.vatAmt.toLocaleString('vi-VN')}</td>
                        <td>${group8.totalAmt.toLocaleString('vi-VN')}</td>
                      </tr>
                      <tr>
                        <td style="text-align: left;">Thuế suất 10%</td>
                        <td>${group10.subtotal.toLocaleString('vi-VN')}</td>
                        <td>${group10.vatAmt.toLocaleString('vi-VN')}</td>
                        <td>${group10.totalAmt.toLocaleString('vi-VN')}</td>
                      </tr>
                      <tr>
                        <td style="text-align: left;">Thuế suất khác</td>
                        <td>${otherSubtotal.toLocaleString('vi-VN')}</td>
                        <td>${otherVatAmt.toLocaleString('vi-VN')}</td>
                        <td>${otherTotalAmt.toLocaleString('vi-VN')}</td>
                      </tr>
                    </tbody>
                  </table>
                  ${shippingFeeAmt > 0 ? `
                  <div style="display: flex; justify-content: space-between; font-weight: 700; font-size: 10px; margin: 4px 0 2px 0;">
                    <span>Phí giao hàng:</span>
                    <span>+${shippingFeeAmt.toLocaleString('vi-VN')}đ</span>
                  </div>
                  ` : ''}
                  <div class="divider"></div>

                  <div class="total-box">
                    <span>TỔNG CỘNG</span>
                    <span>${total.toLocaleString('vi-VN')}đ</span>
                  </div>
                  <div class="text-right" style="font-size: 9px; margin-bottom: 4px;">(Đã bao gồm thuế GTGT ${shippingFeeAmt > 0 ? '& phí giao hàng' : ''})</div>
                  <div class="words-text">
                    Số tiền viết bằng chữ:<br>
                    <strong>${formattedTotalWords}</strong>
                  </div>

                  <div class="divider"></div>

                  ${appliedPromoNames.length > 0 ? `
                  <div style="font-size: 9.5px; margin: 4px 0 6px 0; text-align: left;">
                    <div style="font-weight: 700; color: #000;">HĐ đã được KM:</div>
                    ${appliedPromoNames.map(name => `<div style="padding-left: 10px; color: #000; font-size: 9.5px;">${name}</div>`).join('')}
                  </div>
                  ` : ''}

                  <div style="font-size: 11px; font-weight: 800; font-style: italic; margin: 6px 0 6px 0; color: #000; text-align: center; line-height: 1.35;">
                    Vui lòng kiểm tra kỹ lại nội dung trước khi thanh<br>toán!
                  </div>

                  <div class="divider"></div>

                  <!-- Main Payment QR Code -->
                  ${qrUrl ? `
                  <div class="qr-section">
                    <div class="qr-copy">
                      <div class="qr-title">THANH TOÁN</div>
                      <div class="qr-sub">Quét mã QR để thanh toán. Hỗ trợ tất cả ngân hàng và ví điện tử.</div>
                    </div>
                    <img class="qr-img" src="${qrUrl}" />
                  </div>
                  <div class="divider"></div>
                  ` : ''}

                  <!-- Secondary E-Invoice QR Code -->
                  ${showInvoiceQr ? `
                  <div class="invoice-card-black">
                    <img class="qr-img" src="${invoiceQrUrl}" />
                    <div class="qr-copy">
                      <div class="qr-title">XUẤT HÓA ĐƠN</div>
                      <div class="qr-sub">Quét mã QR để nhập thông tin xuất hóa đơn.<br>Vui lòng thực hiện trong vòng 2 giờ kể từ thời điểm in/thanh toán.</div>
                    </div>
                  </div>
                  <div class="divider"></div>
                  ` : ''}

                  <div class="text-center" style="font-size: 10px; margin-top: 12px;">Trân trọng cảm ơn!</div>
                  <div class="text-center" style="font-size: 10px; font-weight: bold;">AnFood - Một sản phẩm của An Solution</div>
                  <div class="text-center" style="margin-top: 16px;">-</div>
                </body>
                </html>
            `;
        } else {
            let itemsHtml = '';
            items.forEach((item: any) => {
                const itemNote = item.Note || item.note || item.ItemNote || item.NoteText || item.Notes || '';
                const optionLabel = (item.SelectedOptions || []).map((option: any) => `${option.GroupName}: ${option.ValueName}`).join(', ');
                itemsHtml += `
                    <tr style="border-bottom: 1px dashed #000;">
                        <td style="padding: 6px 0; font-size: 15px; font-weight: 800; color: #000; width: 75%;">${item.ItemName || item.MenuItemName}${optionLabel ? `<br><small>${optionLabel}</small>` : ''}</td>
                        <td style="padding: 6px 0; font-size: 20px; font-weight: 900; color: #000; text-align: center; width: 25%;">x${item.Quantity}</td>
                    </tr>
                ` + (itemNote ? `
                    <tr>
                        <td colspan="2" style="font-size: 15px; font-weight: 800; font-style: italic; color: #000; padding-bottom: 8px; padding-top: 2px;">
                            * GHI CHÚ: ${itemNote}
                        </td>
                    </tr>
                ` : '');
            });

            const mainOrderNote = order.Note || this.orderNote || order.OrderNote || '';

            contentHtml = `
                <html>
                <head>
                    <title>Phiếu Báo Bếp</title>
                    <link rel="preconnect" href="https://fonts.googleapis.com">
                    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
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
                        .title { font-size: 20px; font-weight: 900; text-transform: uppercase; margin-top: 0; margin-bottom: 4px; color: #000; }
                        .table-title { font-size: 22px; font-weight: 900; border: 2px solid #000; padding: 4px 10px; display: inline-block; margin-top: 4px; color: #000; border-radius: 4px; }
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
                        <div class="title">PHIẾU BÁO BẾP</div>
                        <div class="table-title">BÀN: ${tableName}</div>
                    </div>
                    <div class="divider"></div>
                    <div class="info-row">
                        <span>Giờ gửi: ${dateStr}</span>
                        <span>Nhân viên: ${waiter}</span>
                    </div>
                    <div class="info-row">
                        <span>Số khách: ${this.orderCustomerCount}</span>
                    </div>
                    <div class="divider"></div>
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="border-bottom: 2px solid #000; font-weight: 900; font-size: 14px; color: #000;">
                                <td style="text-align: left; padding-bottom: 6px;">Tên món</td>
                                <td style="text-align: center; padding-bottom: 6px;">SL</td>
                            </tr>
                        </thead>
                        <tbody>
                            ${itemsHtml}
                        </tbody>
                    </table>
                    <div class="divider"></div>
                    ${mainOrderNote ? `
                    <div style="font-size: 15px; font-weight: 900; border: 2px solid #000; padding: 6px; margin: 8px 0; border-radius: 4px; color: #000;">
                        * GHI CHÚ ĐƠN HÀNG: ${mainOrderNote}
                    </div>
                    <div class="divider"></div>
                    ` : ''}
                    <div class="text-center" style="margin-top: 10px; font-size: 14px; font-weight: 900; color: #000;">
                        --- HẾT PHIẾU BÁO BẾP ---
                    </div>

                    <!-- Paper Feed for Thermal Printer Auto-Cutter Blade (35mm feed pushes paper past cutter) -->
                    <div style="height: 35mm; min-height: 35mm; clear: both; line-height: 35mm;">&nbsp;</div>
                </body>
                </html>
            `;
        }

        this.printHtmlViaHiddenIframe(contentHtml);
    }

    orderOpenPayment() {
        if (this.isButtonDisabled('Thanh toán')) return;

        const totalAmt = this.getRoundedTotalAmount();

        if (this.requireKitchenSend() && totalAmt > 0) {
            const hasUnsent = this.orderItems.some((item: any) => item.Quantity > 0 && (!item.IsSentToKitchen || item.SentQty < item.Quantity));
            const hasPending = this.orderItems.some((item: any) => item.Quantity > 0 && item.SentQty > (item.ServedQty || 0) + (item.SoldOutQty || 0));

            if (hasUnsent || hasPending) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Chưa hoàn thành chế biến',
                    detail: 'Vui lòng gửi order cho bếp và thanh toán sau khi bếp hoàn thành các yêu cầu gọi món của đơn'
                });
                return;
            }
        }

        if (totalAmt === 0) {
            this.paymentType = 'Cash';
            this.paymentAmount = 0;
            this.confirmPayment();
            return;
        }

        this.paymentType = 'Cash';
        this.paymentAmount = this.getOrderTotalAmount();
        this.paymentDialogVisible = true;
        this.paymentQrUrl = null;
        if (this.paymentTransactionId) {
            void this.paymentRealtimeService.leaveTransaction(this.paymentTransactionId);
        }
        this.paymentTransactionId = '';
    }

    selectPaymentType(type: string) {
        this.paymentType = type;
        if (type === 'Transfer') {
            this.generatePaymentQr();
        } else {
            this.paymentQrUrl = null;
            if (this.paymentTransactionId) {
                void this.paymentRealtimeService.leaveTransaction(this.paymentTransactionId);
                this.paymentTransactionId = '';
            }
        }
    }

    generatePaymentQr() {
        if (!this.currentOrder) return;
        this.paymentQrUrl = null;

        this.openTableAndExecute((newBillId) => {
            const totalAmt = this.getOrderTotalAmount();

            if (this.currentOrder.Status === 'WaitingPayment' && this.cachedQrs.has(newBillId)) {
                const cached = this.cachedQrs.get(newBillId)!;
                this.paymentQrUrl = cached.qrUrl;
                this.paymentTransactionId = cached.transactionId;
                void this.paymentRealtimeService.initializeConnection().then(() => {
                    return this.paymentRealtimeService.joinTransaction(cached.transactionId);
                });
                return;
            }

            this.cachedQrs.delete(newBillId);
            const transactionId = `${this.currentOrder.BillCode || 'PAY-' + newBillId}-${Date.now()}`;
            this.paymentTransactionId = transactionId;

            const qrPayload = {
                TransactionId: transactionId,
                BranchCode: this.selectedBranchCode,
                AmountDue: totalAmt,
                CustomerCode: this.orderCustomerId || '',
                IsFastCreate: false,
                CustomerName: '',
                CreatedByUserName: '',
                CreatedByCode: ''
            };

            void this.paymentRealtimeService.initializeConnection().then(() => {
                return this.paymentRealtimeService.joinTransaction(transactionId);
            });

            this.paymentService.createTransaction(qrPayload).subscribe({
                next: (qrRes: any) => {
                    const qrUrl = qrRes?.Data?.qr_image_url ?? qrRes?.Data?.qrImageUrl ?? qrRes?.Data?.QRCode ?? null;
                    this.paymentQrUrl = qrUrl;
                    if (qrUrl) {
                        this.cachedQrs.set(newBillId, { qrUrl, transactionId });
                    }
                },
                error: () => {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tạo mã QR thanh toán!' });
                }
            });
        });
    }

    onPaymentDialogHide() {
        this.paymentQrUrl = null;
        if (this.paymentTransactionId) {
            void this.paymentRealtimeService.leaveTransaction(this.paymentTransactionId);
        }
        this.paymentTransactionId = '';
    }

    confirmPayment() {
        if (!this.currentOrder || this.isPaying()) return;

        this.isPaying.set(true);
        if (this.currentOrder?.BillId) {
            this._locallyHandledPaidOrderId = String(this.currentOrder.BillId).toLowerCase();
        }

        this.openTableAndExecute((newBillId) => {
            this._locallyHandledPaidOrderId = String(newBillId).toLowerCase();
            const totalAmt = this.getRoundedTotalAmount();
            const paymentAmt = Number(this.paymentAmount) || 0;
            if (this.paymentType === 'Cash' && Math.round(paymentAmt) < totalAmt) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Tiền khách đưa phải lớn hơn hoặc bằng tổng tiền cần thanh toán!' });
                this.isPaying.set(false);
                return;
            }

            const payload = this.getSavePayload(newBillId);
            this.salesService.saveOrder(payload).subscribe({
                next: (saveRes: any) => {
                    if (saveRes?.Code === 200) {
                        const paymentPayload = {
                            OrderId: newBillId,
                            BranchCode: this.selectedBranchCode,
                            PaymentType: this.paymentType,
                            PaymentAmount: totalAmt,
                            CashReceivedAmount: this.paymentType === 'Cash' ? Math.round(paymentAmt) : totalAmt,
                            TransactionId: this.paymentType === 'Transfer' ? (this.paymentTransactionId || 'TXN-' + Date.now()) : (this.paymentType !== 'Cash' ? 'TXN-' + Date.now() : null)
                        };

                        const paymentOrderKey = String(newBillId).trim().toLowerCase();
                        const localPaymentExpiresAt = Date.now() + 15000;
                        this.localPaymentOrderIds.set(paymentOrderKey, localPaymentExpiresAt);
                        setTimeout(() => {
                            if (this.localPaymentOrderIds.get(paymentOrderKey) === localPaymentExpiresAt) {
                                this.localPaymentOrderIds.delete(paymentOrderKey);
                            }
                        }, 15000);
                        this.salesService.payOrder(paymentPayload).subscribe({
                            next: (res: any) => {
                                this.isPaying.set(false);
                                if (res?.Code === 200) {
                                    this.messageService.add({ severity: 'success', summary: 'Thanh toán', detail: 'Thanh toán thành công' });
                                    this.paymentDialogVisible = false;
                                    this.checkDialogVisible = false;
                                    this.foodDialogVisible = false;
                                    this.editBillDialogVisible = false;
                                    this.isEditingOrder = false;
                                    this.clearSelection();
                                    this._tableReloadTrigger$.next();

                                    this.paymentQrUrl = null;
                                    this.paymentTransactionId = '';
                                    setTimeout(() => { this._locallyHandledPaidOrderId = ''; }, 3000);
                                } else {
                                    this.localPaymentOrderIds.delete(paymentOrderKey);
                                    this.messageService.add({ severity: 'error', summary: 'Lỗi thanh toán', detail: res.Message });
                                }
                            },
                            error: (err: any) => {
                                this.isPaying.set(false);
                                this.localPaymentOrderIds.delete(paymentOrderKey);
                                this.messageService.add({ severity: 'error', summary: 'Lỗi thanh toán', detail: err.error?.Message || 'Không thể kết nối đến máy chủ.' });
                            }
                        });
                    } else {
                        this.isPaying.set(false);
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: saveRes.Message || 'Lưu hóa đơn thất bại' });
                    }
                },
                error: (err: any) => {
                    this.isPaying.set(false);
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err.error?.Message || 'Lưu hóa đơn thất bại' });
                }
            });
        });
    }

    paymentPrintTemporary() {
        this.openEInvoicePrompt('payment');
    }

    performPaymentPrintTemporary(showInvoiceQr: boolean = true) {
        if (!this.currentOrder) return;
        this.openTableAndExecute((newBillId) => {
            this.salesService.printTemporaryBill(newBillId, this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200) {
                    this.salesService.getOrderById(newBillId, this.selectedBranchCode).subscribe((orderRes: any) => {
                        if (orderRes?.Code === 200 && orderRes.Data) {
                            const updatedOrder = orderRes.Data;
                            const shippingFee = (updatedOrder.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'Delivery' || this.checkBillDetail?.ServiceType === 'Delivery') ? this.parseShippingFee(updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote) : 0;
                            const totalWithShipping = (updatedOrder.TotalAmount || 0) + shippingFee;

                            const printOrder = {
                                ServiceType: updatedOrder.ServiceType || this.currentOrder?.ServiceType || this.checkBillDetail?.ServiceType,
                                ShippingFeeNote: updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote,
                                TableName: updatedOrder.TableName || (updatedOrder.Tables && updatedOrder.Tables.length > 0 ? updatedOrder.Tables.map((t: any) => t.TableName || t.Name).join(' + ') : '') || this.getFirstSelectedTable()?.TableName || this.currentOrder?.TableName || (updatedOrder.ServiceType === 'TakeAway' ? 'Mang về' : (updatedOrder.ServiceType === 'Delivery' ? 'Giao hàng' : '')),
                                OpenTime: updatedOrder.OpenTime,
                                WaiterName: updatedOrder.WaiterName,
                                BillCode: updatedOrder.OrderCode,
                                Items: (updatedOrder.Items || []).map((item: any) => ({
                                    ItemId: item.ItemId,
                                    ItemName: item.ItemName,
                                    Quantity: this.getActualUsedQty(item),
                                    Price: item.UnitPrice || 0,
                                    DiscountRate: item.DiscountRate || 0,
                                    DiscountAmount: item.DiscountAmount || 0,
                                    VatRate: item.VatRate || 0,
                                    VatAmount: item.VatAmount,
                                    FixedTaxAmount: item.FixedTaxAmount,
                                    Note: item.Note || '',
                                    SelectedOptions: item.SelectedOptions || []
                                }))
                            };

                            this.printTemporaryBillWithQr(printOrder, newBillId, (qrUrl) => {
                                this.printReceipt(printOrder, 'Bill', qrUrl, res.Data, showInvoiceQr);
                                this.messageService.add({ severity: 'success', summary: 'In tạm tính', detail: 'Đã in tạm tính thành công!' });
                                this.loadTables();
                                this.loadOrderList();
                            }, totalWithShipping);
                        }
                    });
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            });
        });
    }

    orderCancel() {
        if (this.isButtonDisabled('Hủy bill')) return;
        if (!this.currentOrder) return;
        if (!this.currentOrder.BillId) {
            this.isEditingOrder = false;
            this.foodDialogVisible = false;
            this.clearSelection();
            return;
        }
        this.cancelTableId = this.currentOrder.BillId;
        this.selectedCancelReasons = [];
        this.cancelDialogVisible = true;
    }

    orderClose() {
        this.foodDialogVisible = false;
        this.isEditingOrder = false;
        this.clearSelection();
        this.loadTables();
    }

    cancelEditBill() {
        this.isBillEditUnlocked = false;
        if (!this.currentOrder?.BillId) {
            // No bill yet → just close dialog
            this.foodDialogVisible = false;
            this.orderClose();
            return;
        }

        if (this.currentOrder.ServiceType === 'TakeAway' || this.currentOrder.ServiceType === 'Delivery') {
            // For TakeAway/Delivery: reload order and keep dialog open
            this.salesService.getOrderById(this.currentOrder.BillId, this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    this.startOrderEditing(res.Data);
                    this.foodDialogVisible = true; // keep open
                }
            });
        } else {
            // DineIn: reload order data and keep dialog open
            this.salesService.getOrderById(this.currentOrder.BillId, this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    this.startOrderEditing(res.Data);
                    this.foodDialogVisible = true; // keep open
                } else {
                    this.foodDialogVisible = false;
                    this.loadTables();
                }
            });
        }
    }

    orderCloseFromPopup() {
        if (this.isSendingKitchen()) return;
        if (this.skipCloseDrawerFromPopup) {
            this.skipCloseDrawerFromPopup = false;
            return;
        }
        this.foodDialogVisible = false;
        this.isEditingOrder = false;
        this.clearSelection();
    }

    actionApproveAndSendKitchen(order: any) {
        if (!this.hasPermission(['SalesCatalog.DiningTable.ConfirmOrder', 'SALEROOM_CASHIER_CONFIRM_ORDER', 'Sales.ConfirmOrder'])) {
            this.messageService.add({
                severity: 'error',
                summary: 'Không có quyền',
                detail: 'Bạn không có quyền xác nhận đơn hàng!'
            });
            return;
        }

        const isRequire = this.requireKitchenSend();
        this.confirmationService.confirm({
            message: isRequire ? `Xác nhận duyệt và gửi bếp/bar cho đơn ${order.OrderCode}?` : `Xác nhận duyệt đơn ${order.OrderCode}?`,
            header: 'Xác nhận duyệt đơn',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Đồng ý',
            rejectLabel: 'Hủy',
            accept: () => {
                this.salesService.sendOrderToKitchenBar(order.OrderId, this.selectedBranchCode).subscribe((res: any) => {
                    if (res?.Code === 200) {
                        this.showPrintDispatchResults(res);
                        if (this.enableKitchenPrint() && res.Data?.PrintSlips && res.Data.PrintSlips.length > 0) {
                            this.printKitchenSlips(res.Data.PrintSlips, order);
                        }
                        this.messageService.add({ severity: 'success', summary: 'Duyệt đơn', detail: isRequire ? 'Đã duyệt và gửi bếp/bar thành công!' : 'Đã duyệt thành công!' });
                        this.loadTables();
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Thất bại', detail: res.Message || 'Có lỗi xảy ra' });
                    }
                });
            }
        });
    }

    actionMergeQrOrder(order: any) {
        this.currentQrMergeOrder = order;
        this.selectedTargetOrderId = null;

        const activeCount = order.ActiveOrders?.length || 0;
        if (activeCount === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Không thể gộp', detail: 'Chưa có hóa đơn nào phù hợp để gộp!' });
            return;
        }

        if (activeCount === 1) {
            const targetOrder = order.ActiveOrders[0];
            const targetLabel = targetOrder.TableName
                ? `ở bàn ${targetOrder.TableName}`
                : (targetOrder.ServiceType === 'TakeAway' ? 'mang về' : targetOrder.ServiceType === 'Delivery' ? 'giao hàng' : 'đang hoạt động');
            this.confirmationService.confirm({
                message: `Xác nhận gộp đơn QR mới này vào hóa đơn ${targetOrder.OrderCode} (${targetLabel})?`,
                header: 'Xác nhận gộp đơn',
                icon: 'pi pi-clone',
                acceptLabel: 'Đồng ý',
                rejectLabel: 'Hủy',
                accept: () => {
                    this.executeMergeQrOrder(order.OrderId, targetOrder.OrderId, targetOrder.OrderCode);
                }
            });
        } else {
            this.mergeQrDialogVisible = true;
        }
    }

    confirmMergeQrOrder() {
        if (!this.currentQrMergeOrder || !this.selectedTargetOrderId) return;

        const targetOrder = this.currentQrMergeOrder.ActiveOrders.find((o: any) => o.OrderId === this.selectedTargetOrderId);
        const targetCode = targetOrder ? targetOrder.OrderCode : '';

        this.mergeQrDialogVisible = false;
        this.executeMergeQrOrder(this.currentQrMergeOrder.OrderId, this.selectedTargetOrderId, targetCode);
    }

    executeMergeQrOrder(waitingConfirmOrderId: string, targetOrderId: string, targetOrderCode: string) {
        this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: 'Chức năng gộp đơn QR đã được lược bỏ' });
    }

    actionCancelServiceTypeOrder(order: any) {
        this.cancelTableId = order.OrderId;
        this.selectedCancelReasons = [];
        this.cancelDialogVisible = true;
    }

    // 2. Mang về (Takeaway)
    actionTakeAway() {
        this.clearSelection();
        this.currentOrder = {
            BillId: null,
            BillCode: 'DRAFT',
            TableId: null,
            TableName: 'Mang về',
            ServiceType: 'TakeAway',
            WaiterId: null,
            CustomerCount: 1,
            Items: []
        };
        this.orderWaiterId = null;
        this.orderCustomerCount = 1;
        this.orderCustomerId = null;
        this.phoneNumber = '';
        this.deliveryAddress = '';
        this.deliveryNote = '';
        this.shippingFeeNote = '';
        this.orderItems = [];
        this.isEditingOrder = false;
        this.openFoodDialog();
    }

    // 3. Giao hàng (Delivery)
    actionDelivery() {
        this.clearSelection();
        this.currentOrder = {
            BillId: null,
            BillCode: 'DRAFT',
            TableId: null,
            TableName: 'Giao hàng',
            ServiceType: 'Delivery',
            WaiterId: null,
            CustomerCount: 1,
            Items: []
        };
        this.orderWaiterId = null;
        this.orderCustomerCount = 1;
        this.orderCustomerId = null;
        this.phoneNumber = '';
        this.deliveryAddress = '';
        this.deliveryNote = '';
        this.shippingFeeNote = '';
        this.orderItems = [];
        this.isEditingOrder = false;
        this.openFoodDialog();
    }

    selectServiceTypeOrder(order: any) {
        this.openBillDetail(order);
    }

    actionPaymentForOrder(order: any) {
        this.salesService.getOrderById(order.OrderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const orderData = res.Data;

                // Validate if any items are not sent or pending kitchen completion
                const itemsList = orderData.Items || [];
                const orderTotal = Math.round(orderData.TotalAmount ?? orderData.totalAmount ?? 0);

                if (this.requireKitchenSend() && orderTotal > 0) {
                    const hasUnsent = itemsList.some((item: any) => item.Quantity > 0 && (!item.IsSentToKitchen || item.SentQty < item.Quantity));
                    const hasPending = itemsList.some((item: any) => item.Quantity > 0 && item.SentQty > (item.ServedQty || 0) + (item.SoldOutQty || 0));

                    if (hasUnsent || hasPending) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Chưa hoàn thành chế biến',
                            detail: 'Vui lòng gửi order cho bếp và thanh toán sau khi bếp hoàn thành các yêu cầu gọi món của đơn'
                        });
                        return;
                    }
                }

                const tableId = (orderData.Tables && orderData.Tables.length > 0) ? orderData.Tables[0].TableId : (orderData.TableId || null);
                const tableName = (orderData.Tables && orderData.Tables.length > 0)
                    ? orderData.Tables.map((t: any) => t.TableName || t.Name).join(' + ')
                    : (orderData.TableName || orderData.TableNames || (orderData.ServiceType === 'TakeAway' ? 'Mang về' : (orderData.ServiceType === 'Delivery' ? 'Giao hàng' : '')));

                const bill = {
                    ...orderData,
                    BillId: orderData.Id || orderData.OrderId || orderData.BillId,
                    BillCode: orderData.OrderCode || orderData.BillCode,
                    TableId: tableId,
                    TableName: tableName,
                    Tables: orderData.Tables,
                    ServiceType: orderData.ServiceType,
                    Items: itemsList
                };
                this.selectedOrderId = orderData.Id;
                this.startOrderEditing(bill);
                this.orderOpenPayment();
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không thể lấy thông tin chi tiết đơn hàng!' });
            }
        });
    }

    actionCancelForOrder(order: any) {
        if (this.isButtonDisabled('Hủy bill')) return;
        this.cancelTableId = order.OrderId;
        this.selectedCancelReasons = [];
        this.cancelDialogVisible = true;
    }

    // 4. Merge Tables (Gộp bàn)
    actionMergeTablesPreview() {
        const selected = this.getSelectedTablesData();
        if (selected.length === 1) {
            const table = selected[0];
            const bill = table.CurrentBills?.[0] || {
                TableId: table.TableId,
                TableName: table.TableName,
                CustomerCount: 1,
                TotalAmount: 0
            };
            this.actionMergeOrderForOrder(bill);
        } else if (selected.length >= 2) {
            this.actionMergeTables();
        } else {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất 1 bàn đang có khách để gộp!' });
        }
    }

    actionMergeTables() {
        const selected = this.getSelectedTablesData();
        const billIds = selected.map(t => t.CurrentBills?.[0]?.BillId).filter(Boolean);

        if (billIds.length < 2) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất 2 bàn bận để gộp!' });
            return;
        }

        const requests = billIds.map(id => this.salesService.getOrderById(id, this.selectedBranchCode));

        forkJoin(requests).subscribe((responses: any[]) => {
            const allItems: any[] = [];
            let totalCustomerCount = 0;

            responses.forEach(res => {
                if (res?.Code === 200 && res.Data) {
                    const order = res.Data;
                    totalCustomerCount += (order.CustomerCount || 0);

                    const items = order.Items || [];
                    items.forEach((item: any) => {
                        const existing = allItems.find(x => x.ItemCode === item.ItemCode);
                        if (existing) {
                            existing.Quantity += item.Quantity;
                            existing.Amount += (item.Quantity * item.Price);
                        } else {
                            allItems.push({
                                ItemCode: item.ItemCode,
                                ItemName: item.ItemName,
                                Price: item.Price,
                                Quantity: item.Quantity,
                                Amount: item.Quantity * item.Price
                            });
                        }
                    });
                }
            });

            const totalAmount = allItems.reduce((sum, item) => sum + item.Amount, 0);

            this.mergePreview = {
                TableNames: selected.map(t => t.TableName),
                MergedItems: allItems,
                TotalCustomerCount: totalCustomerCount,
                TotalAmount: totalAmount
            };

            this.mergeSelectedTableIds.clear();
            selected.forEach(t => {
                this.mergeSelectedTableIds.set(t.TableId, true);
            });

            this.mergeTargetTableId = '';
            this.mergeDialogVisible = true;
        });
    }

    onMergeTableToggle(tableId: string, checked: boolean) {
        this.mergeSelectedTableIds.set(tableId, checked);
    }

    onMergeTargetTableChange() {
        if (this.mergeTargetTableId) {
            this.mergeSelectedTableIds.set(this.mergeTargetTableId, true);
        }
    }

    confirmMerge() {
        if (!this.mergeTargetTableId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn bàn đích để gộp!' });
            return;
        }

        const selected = this.getSelectedTablesData();
        const targetTable = selected.find(t => t.TableId === this.mergeTargetTableId);
        if (!targetTable) return;

        const sourceTables = selected.filter(t => t.TableId !== this.mergeTargetTableId);
        const targetOrderId = this.getActiveOrderIdForTable(targetTable);
        const sourceOrderIds = sourceTables.map(t => t.CurrentBills?.[0]?.BillId).filter(Boolean);

        if (!targetOrderId) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không tìm thấy hóa đơn của bàn đích!' });
            return;
        }

        const keptTableIds = Array.from(this.mergeSelectedTableIds.entries())
            .filter(([_, checked]) => checked)
            .map(([id]) => id);

        this.salesService.mergeOrders(targetOrderId, sourceOrderIds, keptTableIds, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Gộp bàn', detail: 'Gộp bàn thành công!' });
                this.mergeDialogVisible = false;
                this.clearSelection();
                this.loadTables();
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    // 5. Split Table / Split Order (Tách bàn / Tách order)
    openSplitDialog(orderId: string, sourceOrder?: any) {
        if (!orderId || !this.selectedBranchCode) return;

        this.selectedOrderId = orderId;
        this.salesService.getCheckItems(orderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.splitPreview = res.Data;
                this.splitItems = (res.Data.Items || []).map((item: any) => {
                    const originalCancelled = (item.CancelledQuantity || 0) + (item.SoldOutQty || 0);
                    return {
                        Id: item.Id || null,
                        ItemId: item.ItemId,
                        ItemCode: item.ItemCode,
                        ItemName: item.ItemName,
                        Price: item.Price,
                        // Original quantities
                        OriginalQty: item.Quantity,
                        OriginalServed: item.ServedQty || 0,
                        OriginalCancelled: originalCancelled,
                        OriginalWaiting: Math.max(0, item.Quantity - (item.ServedQty || 0) - originalCancelled),
                        PromotionCode: item.PromotionCode || item.promotionCode || null,
                        DiscountRate: item.DiscountRate || 0,
                        // Split quantities
                        Quantity: 0,
                        ActualQuantity: 0,
                        CancelledQuantity: 0
                    };
                });
                this.splitIsNewBill = true;
                this.splitCustomerCount = 1;

                const floorId = this.activeFloor?.Id;
                this.salesService.getAvailableTargetTables(this.selectedBranchCode, floorId).subscribe((targetRes: any) => {
                    if (targetRes?.Code === 200 && targetRes.Data) {
                        this.availableTargetTables = targetRes.Data || [];
                        if (this.splitPreview && this.splitPreview.TableIds && this.splitPreview.TableIds.length > 1) {
                            for (let i = 0; i < this.splitPreview.TableIds.length; i++) {
                                const tId = this.splitPreview.TableIds[i];
                                const tName = this.splitPreview.TableNames[i];
                                if (!this.availableTargetTables.some((x: any) => x.TableId === tId)) {
                                    this.availableTargetTables.push({
                                        TableId: tId,
                                        TableName: tName + ' (Bàn đã gộp)'
                                    });
                                }
                            }
                        }
                    }
                    this.onSplitModeChange();
                    this.splitDialogVisible = true;
                });
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không tải được thông tin đơn hàng' });
            }
        });
    }

    actionSplitTablePreview() {
        if (!this.canSplitTable()) return;
        const table = this.getFirstSelectedTable();
        if (!this.ensureOrderSelected(table, () => this.actionSplitTablePreview())) {
            return;
        }

        const activeBillId = this.getActiveOrderIdForTable(table);
        if (!activeBillId) return;

        this.openSplitDialog(activeBillId);
    }

    isSplitTargetTableDisabled(): boolean {
        return this.splitIsNewBill === true || String(this.splitIsNewBill) === 'true';
    }

    onSplitModeChange() {
        if (this.splitIsNewBill === true || String(this.splitIsNewBill) === 'true') {
            const sourceTable = this.getFirstSelectedTable();
            const sourceTableId = (this.splitPreview?.TableIds && this.splitPreview.TableIds.length > 0)
                ? this.splitPreview.TableIds[0]
                : (sourceTable?.Id || sourceTable?.TableId);
            this.splitTargetTableId = sourceTableId || '';
        } else {
            this.splitTargetTableId = '';
        }
    }

    onSplitQuantityChange(item: any) {
        if (item.Quantity == null || item.Quantity < 0) {
            item.Quantity = 0;
        }
        if (item.Quantity > item.OriginalQty) {
            item.Quantity = item.OriginalQty;
        }

        if (!this.requireKitchenSend()) {
            item.ActualQuantity = Math.min(item.Quantity, item.OriginalServed || 0);
            item.CancelledQuantity = 0;
            return;
        }

        const qty = item.Quantity;
        const waitingToTake = Math.min(qty, item.OriginalWaiting || 0);
        const remainAfterWaiting = qty - waitingToTake;

        const servedToTake = Math.min(remainAfterWaiting, item.OriginalServed || 0);
        const remainAfterServed = remainAfterWaiting - servedToTake;

        const cancelledToTake = Math.min(remainAfterServed, item.OriginalCancelled || 0);

        item.ActualQuantity = servedToTake;
        item.CancelledQuantity = cancelledToTake;
    }

    confirmSplit() {
        if (this.splitIsNewBill && !this.splitTargetTableId) {
            this.onSplitModeChange();
        }

        if (!this.requireKitchenSend()) {
            for (const item of this.splitItems) {
                item.ActualQuantity = Math.min(item.Quantity, item.OriginalServed || 0);
                item.CancelledQuantity = 0;
            }
        }

        const itemsToSplit = this.splitItems
            .filter(x => x.Quantity > 0)
            .map(x => ({
                Id: x.Id,
                ItemId: x.ItemId,
                Quantity: x.Quantity,
                ActualQuantity: x.ActualQuantity || 0,
                CancelledQuantity: x.CancelledQuantity || 0
            }));

        if (itemsToSplit.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Chưa chọn số lượng món cần tách!' });
            return;
        }

        // Validate quantities for each item
        for (const item of this.splitItems) {
            if (item.Quantity <= 0) continue;

            const splitWaiting = item.Quantity - (item.ActualQuantity || 0) - (item.CancelledQuantity || 0);

            if (item.Quantity > item.OriginalQty) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: `Số lượng tách của món '${item.ItemName}' không được lớn hơn số lượng gốc!` });
                return;
            }
            if (this.requireKitchenSend()) {
                if (item.ActualQuantity < 0 || item.ActualQuantity > item.OriginalServed || item.ActualQuantity > item.Quantity) {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: `Số lượng đã trả tách của món '${item.ItemName}' không hợp lệ!` });
                    return;
                }
                if (item.CancelledQuantity < 0 || item.CancelledQuantity > item.OriginalCancelled || item.CancelledQuantity > (item.Quantity - item.ActualQuantity)) {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: `Số lượng hủy tách của món '${item.ItemName}' không hợp lệ!` });
                    return;
                }
                if (splitWaiting < 0 || splitWaiting > item.OriginalWaiting) {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: `Số lượng chờ tách của món '${item.ItemName}' không hợp lệ!` });
                    return;
                }
            }
        }

        const activeBillId = this.splitPreview?.BillId || this.selectedOrderId;
        if (!activeBillId) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không xác định được đơn hàng cần tách!' });
            return;
        }

        const activeCustomerCount = this.splitPreview?.CustomerCount ?? this.splitPreview?.OriginalCustomerCount ?? 1;
        const maxSplitCustomers = activeCustomerCount - 1;
        if (this.splitCustomerCount < 1) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: 'Số lượng khách tách phải từ 1 trở lên!' });
            return;
        }
        if (activeCustomerCount > 1 && this.splitCustomerCount > maxSplitCustomers) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: `Số lượng khách tách tối đa là ${maxSplitCustomers} (bàn gốc phải còn ít nhất 1 khách)!` });
            return;
        }

        let totalOriginalQty = this.splitItems.reduce((acc, item) => acc + (item.OriginalQty - item.OriginalCancelled), 0);
        let totalSplitQty = this.splitItems.reduce((acc, item) => acc + (item.Quantity - item.CancelledQuantity), 0);
        if (totalSplitQty >= totalOriginalQty) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi validate', detail: 'Không được tách hết toàn bộ món khỏi order gốc!' });
            return;
        }

        if (!this.splitIsNewBill && !this.splitTargetTableId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn bàn trống để nhận món!' });
            return;
        }

        const payload = {
            SourceOrderId: activeBillId,
            TargetTableId: this.splitTargetTableId || '00000000-0000-0000-0000-000000000000',
            IsNewBill: this.splitIsNewBill,
            ItemsToSplit: itemsToSplit,
            CustomerCount: this.splitCustomerCount,
            BranchCode: this.selectedBranchCode
        };

        this.salesService.splitOrder(payload).subscribe({
            next: (res: any) => {
                if (res?.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Tách order', detail: 'Tách order thành công!' });
                    this.splitDialogVisible = false;
                    this.clearSelection();
                    this.loadTables();
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Đã xảy ra lỗi khi tách order!' });
                }
            },
            error: (err: any) => {
                const errMsg = err?.error?.Message || err?.message || 'Không thể kết nối đến máy chủ!';
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: errMsg });
            }
        });
    }

    // 6. Transfer Table (Chuyển bàn)
    actionTransferTablePreview() {
        if (!this.canTransferTable()) return;
        const table = this.getFirstSelectedTable();
        if (!this.ensureOrderSelected(table, () => this.actionTransferTablePreview())) {
            return;
        }

        this.salesService.getAvailableTargetTables(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.availableTargetTables = res.Data || [];
                this.transferTargetTableIds = [];

                const tableCode = table ? (table.TableCode || table.TableName || '') : '';
                const activeBillId = this.getActiveOrderIdForTable(table);
                const activeBill = table?.CurrentBills?.find((b: any) => (b.BillId === activeBillId || b.Id === activeBillId)) || this.currentOrder;
                const orderCode = activeBill ? (activeBill.BillCode || activeBill.OrderCode || activeBill.Code || '') : '';

                this.transferNote = `Chuyển từ bàn ${tableCode} ${orderCode}`.trim();
                this.transferSelectedFloorId = '';
                this.transferSearchText = '';
                this.transferFloorOptions = [{ Id: '', Name: 'Tất cả các tầng' }, ...this.floors];
                this.transferDialogVisible = true;
            }
        });
    }

    toggleTransferTargetTable(tableId: string) {
        const idx = this.transferTargetTableIds.indexOf(tableId);
        if (idx > -1) {
            this.transferTargetTableIds.splice(idx, 1);
        } else {
            this.transferTargetTableIds.push(tableId);
        }
    }

    confirmTransfer() {
        if (this.transferTargetTableIds.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Chưa chọn bàn muốn chuyển đến!' });
            return;
        }

        const sourceTable = this.getFirstSelectedTable();
        const activeBillId = this.selectedOrderId || (this.currentOrder?.BillId || this.currentOrder?.OrderId) || this.getActiveOrderIdForTable(sourceTable);
        if (!activeBillId) return;

        this.salesService.transferTableOrder(activeBillId, this.transferTargetTableIds, this.selectedBranchCode, this.transferNote).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Chuyển bàn', detail: 'Chuyển bàn thành công!' });
                this.transferDialogVisible = false;
                this.clearSelection();
                this.loadTables();
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    // 7. Cancel Table (Hủy bàn)
    actionCancelTablePrompt() {
        if (this.isButtonDisabled('Hủy bill')) return;
        if (!this.canCancelTable()) return;
        const table = this.getFirstSelectedTable();
        if (!this.ensureOrderSelected(table, () => this.actionCancelTablePrompt())) {
            return;
        }

        const activeBillId = this.getActiveOrderIdForTable(table);
        if (!activeBillId) return;
        this.cancelTableId = activeBillId;
        this.selectedCancelReasons = [];
        this.cancelDialogVisible = true;
    }

    confirmCancelTable() {
        if (!this.selectedCancelReasons || this.selectedCancelReasons.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn ít nhất một lý do hủy!' });
            return;
        }

        const reasonText = this.selectedCancelReasons.join(', ');
        const orderToCancel = this.currentOrder;
        const sentItemsToCancel = (this.orderItems || []).filter(item => item.SentQty > 0 && (item.Quantity - (item.SoldOutQty || 0) > 0));
        const cancelOrderId = this.cancelTableId;
        const cancelOrderKey = String(cancelOrderId).trim().toLowerCase();
        const localCancellationExpiresAt = Date.now() + 15000;
        this.localCancellationOrderIds.set(cancelOrderKey, localCancellationExpiresAt);
        setTimeout(() => {
            if (this.localCancellationOrderIds.get(cancelOrderKey) === localCancellationExpiresAt) {
                this.localCancellationOrderIds.delete(cancelOrderKey);
            }
        }, 15000);

        const targetCancelId = String(this.cancelTableId || this.currentOrder?.BillId || '').toLowerCase();
        this._locallyHandledCancelledOrderId = targetCancelId;
        this.salesService.cancelOrder(cancelOrderId, this.selectedBranchCode, reasonText).subscribe({
            next: (res: any) => {
                if (res?.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Hủy hóa đơn', detail: 'Hủy hóa đơn thành công!' });

                    if (this.enableKitchenPrint() && !res.Data?.WasReserved && res.Data?.HadKitchenSentItems && sentItemsToCancel.length > 0) {
                        const cancelSlip = {
                            SlipType: 'Cancel',
                            KitchenAreaName: 'Bếp / Bar',
                            OrderCode: orderToCancel?.BillCode || orderToCancel?.OrderCode || '',
                            TableName: orderToCancel?.TableName || 'Tại bàn',
                            WaiterName: orderToCancel?.WaiterName || '',
                            CustomerCount: orderToCancel?.CustomerCount || 1,
                            PrintTime: new Date(),
                            OrderNote: orderToCancel?.Note || '',
                            Items: sentItemsToCancel.map(i => ({
                                MenuItemName: i.ItemName,
                                Quantity: i.SentQty || i.Quantity,
                                Note: i.Note,
                                Reason: `Hủy toàn bộ đơn: ${reasonText}`
                            }))
                        };
                        this.dispatchKitchenCancellations(cancelOrderId, sentItemsToCancel.map((item: any) => ({ OrderItemId: item.Id, Quantity: item.SentQty || item.Quantity })), `Hủy toàn bộ đơn: ${reasonText}`);
                    }

                    this.cancelDialogVisible = false;
                    this.isEditingOrder = false;
                    this.foodDialogVisible = false; // Hide food dialog popup too!
                    this.clearSelection();
                    this.loadTables();
                } else {
                    this.localCancellationOrderIds.delete(cancelOrderKey);
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            },
            error: (err: any) => {
                this.localCancellationOrderIds.delete(cancelOrderKey);
                this.messageService.add({ severity: 'error', summary: 'Không hủy được hóa đơn', detail: err?.error?.Message || 'Vui lòng thử lại.' });
            }
        });
    }

    // 8. Check Items (Kiểm đồ)
    checkBillItems: any[] = [];

    actionCheckItems() {
        if (!this.canCheckItems()) return;
        const table = this.getFirstSelectedTable();
        if (!table.CurrentBills || table.CurrentBills.length === 0) return;
        const activeBill = table.CurrentBills[0];

        this.salesService.getCheckItems(activeBill.BillId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.checkBillDetail = res.Data;
                this.checkBillItems = (res.Data.Items || []).map((item: any) => {
                    // OriginalServedQty = số lượng đã trả (reference cố định để tính TrảLại)
                    // = Quantity - SoldOutQty - CancelledQuantity (backend đã tính sẵn trong ServedQuantity)
                    const served = item.ServedQuantity !== undefined ? item.ServedQuantity : item.Quantity;
                    return {
                        ...item,
                        OriginalServedQty: served,  // reference cố định, không thay đổi
                        ServedQuantity: served,      // Thực dùng, user có thể tăng/giảm
                        ReturnedQuantity: 0          // Trả lại = ServedQty - ThựcDùng = 0 ban đầu
                    };
                });
                this.checkDialogVisible = true;
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    checkIncreaseQty(item: any) {
        item.ServedQuantity += 1;
        // Trả lại = Số đã trả (OriginalServedQty) - Thực dùng
        item.ReturnedQuantity = Math.max(0, item.OriginalServedQty - item.ServedQuantity);
    }

    checkDecreaseQty(item: any) {
        if (item.ServedQuantity > 0) {
            item.ServedQuantity -= 1;
            // Trả lại = Số đã trả (OriginalServedQty) - Thực dùng
            item.ReturnedQuantity = Math.max(0, item.OriginalServedQty - item.ServedQuantity);
        }
    }

    confirmCheckItems(callback?: () => void) {
        if (!this.checkBillDetail) return;

        const payloadItems = this.checkBillItems.map(item => ({
            ItemId: item.ItemId,
            CheckedQuantity: item.ServedQuantity,
            Note: item.Note || ''
        }));

        this.salesService.checkItems(this.checkBillDetail.BillId, payloadItems, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Kiểm đồ', detail: 'Đã xác nhận kiểm đồ thành công!' });
                this.checkDialogVisible = false;
                this.loadTables();
                this.loadOrderList();
                if (!callback && this.selectedOrderId && this.selectedOrderId === this.checkBillDetail.BillId) {
                    this.salesService.getOrderById(this.selectedOrderId, this.selectedBranchCode).subscribe({
                        next: (orderRes: any) => {
                            if (orderRes?.Code === 200 && orderRes.Data) {
                                const orderStatus = orderRes.Data.Status;
                                if (orderStatus === 'Paid' || orderStatus === 'Completed' || orderStatus === 'Cancelled' || orderStatus === 'ApprovedCancelled' || orderStatus === 'Merged') {
                                    this.isEditingOrder = false;
                                    this.clearSelection();
                                } else {
                                    this.startOrderEditing(orderRes.Data);
                                }
                            } else {
                                this.isEditingOrder = false;
                                this.clearSelection();
                            }
                        },
                        error: () => {
                            this.isEditingOrder = false;
                            this.clearSelection();
                        }
                    });
                }
                if (callback) {
                    callback();
                }
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi kiểm đồ', detail: res.Message });
            }
        });
    }

    checkConfirm() {
        this.confirmCheckItems();
    }

    checkPrintCheckTicket() {
        this.confirmCheckItems(() => {
            if (this.checkBillDetail) {
                this.salesService.printCheckItems(this.checkBillDetail.BillId, this.selectedBranchCode).subscribe((res: any) => {
                    this.messageService.add({ severity: 'success', summary: 'In phiếu kiểm đồ', detail: 'Đã in phiếu kiểm đồ thành công!' });
                });
            }
        });
    }

    checkPrintTemporaryBill() {
        this.openEInvoicePrompt('check');
    }

    performCheckPrintTemporaryBill(showInvoiceQr: boolean = true) {
        const isTakeAwayOrDelivery = this.currentOrder?.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'TakeAway' || this.checkBillDetail?.ServiceType === 'Delivery' || this.checkBillDetail?.ServiceType === 'TakeAway';

        const printProc = () => {
            if (this.checkBillDetail) {
                this.salesService.getOrderById(this.checkBillDetail.BillId, this.selectedBranchCode).subscribe((orderRes: any) => {
                    if (orderRes?.Code === 200 && orderRes.Data) {
                        const updatedOrder = orderRes.Data;
                        this.salesService.printTemporaryBill(updatedOrder.Id, this.selectedBranchCode).subscribe((res: any) => {
                            if (res?.Code === 200) {
                                const shippingFee = (updatedOrder.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'Delivery' || this.checkBillDetail?.ServiceType === 'Delivery') ? this.parseShippingFee(updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote) : 0;
                                const totalWithShipping = (updatedOrder.TotalAmount || 0) + shippingFee;

                                const printOrder = {
                                    ServiceType: updatedOrder.ServiceType || this.currentOrder?.ServiceType || this.checkBillDetail?.ServiceType,
                                    ShippingFeeNote: updatedOrder.ShippingFeeNote || this.shippingFeeNote || this.currentOrder?.ShippingFeeNote,
                                    TableName: updatedOrder.TableName || (updatedOrder.Tables && updatedOrder.Tables.length > 0 ? updatedOrder.Tables.map((t: any) => t.TableName || t.Name).join(' + ') : '') || this.checkBillDetail?.TableName || this.getFirstSelectedTable()?.TableName || this.currentOrder?.TableName || (updatedOrder.ServiceType === 'TakeAway' ? 'Mang về' : (updatedOrder.ServiceType === 'Delivery' ? 'Giao hàng' : '')),
                                    OpenTime: updatedOrder.OpenTime,
                                    WaiterName: updatedOrder.WaiterName,
                                    BillCode: updatedOrder.OrderCode,
                                    Items: (updatedOrder.Items || []).map((item: any) => ({
                                        ItemId: item.ItemId,
                                        ItemName: item.ItemName,
                                        Quantity: this.getActualUsedQty(item),
                                        Price: item.UnitPrice || 0,
                                        DiscountRate: item.DiscountRate || 0,
                                        DiscountAmount: item.DiscountAmount || 0,
                                        VatRate: item.VatRate || 0,
                                        VatAmount: item.VatAmount,
                                        FixedTaxAmount: item.FixedTaxAmount,
                                        Note: item.Note || '',
                                        SelectedOptions: item.SelectedOptions || []
                                    }))
                                };
                                this.printTemporaryBillWithQr(printOrder, updatedOrder.Id, (qrUrl) => {
                                    this.printReceipt(printOrder, 'Bill', qrUrl, res.Data, showInvoiceQr);
                                    this.messageService.add({ severity: 'success', summary: 'In tạm tính', detail: 'Đã in tạm tính thành công! Trạng thái chuyển sang Chờ thanh toán.' });
                                    this.loadTables();
                                    this.loadOrderList();
                                }, totalWithShipping);
                            } else {
                                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                            }
                        });
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: orderRes?.Message || 'Không thể lấy thông tin hóa đơn mới nhất!' });
                    }
                });
            }
        };

        if (isTakeAwayOrDelivery) {
            printProc();
        } else {
            this.confirmCheckItems(printProc);
        }
    }

    performCardPrintTemporary(tableOrder: any, showInvoiceQr: boolean = true) {
        const orderId = tableOrder?.OrderId || tableOrder?.BillId || tableOrder?.Id;
        if (!orderId) return;
        this.salesService.printTemporaryBill(orderId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.salesService.getOrderById(orderId, this.selectedBranchCode).subscribe((orderRes: any) => {
                    if (orderRes?.Code === 200 && orderRes.Data) {
                        const updatedOrder = orderRes.Data;
                        const shippingFee = (updatedOrder.ServiceType === 'Delivery' || this.currentOrder?.ServiceType === 'Delivery' || tableOrder?.ServiceType === 'Delivery') ? this.parseShippingFee(updatedOrder.ShippingFeeNote || this.shippingFeeNote || tableOrder?.ShippingFeeNote) : 0;
                        const totalWithShipping = (updatedOrder.TotalAmount || 0) + shippingFee;

                        const printOrder = {
                            ServiceType: updatedOrder.ServiceType || tableOrder?.ServiceType || this.currentOrder?.ServiceType,
                            ShippingFeeNote: updatedOrder.ShippingFeeNote || this.shippingFeeNote || tableOrder?.ShippingFeeNote,
                            TableName: updatedOrder.TableName || (updatedOrder.Tables && updatedOrder.Tables.length > 0 ? updatedOrder.Tables.map((t: any) => t.TableName || t.Name).join(' + ') : '') || tableOrder.TableName || this.currentOrder?.TableName || (updatedOrder.ServiceType === 'TakeAway' ? 'Mang về' : (updatedOrder.ServiceType === 'Delivery' ? 'Giao hàng' : '')),
                            OpenTime: updatedOrder.OpenTime,
                            WaiterName: updatedOrder.WaiterName,
                            BillCode: updatedOrder.OrderCode,
                            Items: (updatedOrder.Items || []).map((item: any) => ({
                                ItemId: item.ItemId,
                                ItemName: item.ItemName,
                                Quantity: this.getActualUsedQty(item),
                                Price: item.UnitPrice || 0,
                                DiscountRate: item.DiscountRate || 0,
                                DiscountAmount: item.DiscountAmount || 0,
                                VatRate: item.VatRate || 0,
                                VatAmount: item.VatAmount,
                                FixedTaxAmount: item.FixedTaxAmount,
                                Note: item.Note || '',
                                SelectedOptions: item.SelectedOptions || []
                            }))
                        };
                        this.printTemporaryBillWithQr(printOrder, orderId, (qrUrl) => {
                            this.printReceipt(printOrder, 'Bill', qrUrl, res.Data, showInvoiceQr);
                            this.messageService.add({ severity: 'success', summary: 'In tạm tính', detail: 'Đã in tạm tính thành công! Trạng thái chuyển sang Chờ thanh toán.' });
                            this.loadTables();
                            this.loadOrderList();
                        }, totalWithShipping);
                    }
                });
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'In tạm tính thất bại' });
            }
        });
    }

    checkPay() {
        this.confirmCheckItems(() => {
            if (this.checkBillDetail) {
                this.currentOrder = {
                    BillId: this.checkBillDetail.BillId,
                    BillCode: this.checkBillDetail.BillCode,
                    TableName: this.checkBillDetail.TableName || this.getFirstSelectedTable()?.TableName || this.currentOrder?.TableName,
                    CustomerCount: this.checkBillDetail.CustomerCount,
                    ServiceType: this.checkBillDetail.ServiceType || 'DineIn'
                };
                this.orderItems = this.checkBillItems.map(item => {
                    const qty = item.ServedQuantity !== undefined ? item.ServedQuantity : (item.Quantity || 0);
                    const price = item.Price || item.UnitPrice || 0;
                    const amount = item.Amount !== undefined ? item.Amount : Math.round(qty * price);
                    const rawDisc = item.DiscountAmount !== undefined ? item.DiscountAmount : Math.round(amount * (item.DiscountRate || 0) / 100);
                    const discMoney = item.DiscountMoney || item.discountMoney || 0;
                    const netDisc = Math.max(0, rawDisc - discMoney);
                    const calc = OrderCalculator.calculateItem({
                        qty,
                        unitPrice: price,
                        itemDiscount: netDisc,
                        vatRate: item.VatRate || 0
                    });
                    const vatAmount = (item.VatAmount !== undefined && discMoney === 0) ? item.VatAmount : calc.vatAmount;
                    const totalAmount = (item.TotalAmount !== undefined && discMoney === 0) ? item.TotalAmount : calc.totalAmount;

                    return {
                        ItemId: item.ItemId,
                        ItemCode: item.ItemCode,
                        ItemName: item.ItemName,
                        UnitName: item.UnitName,
                        Quantity: qty,
                        Price: price,
                        Amount: amount,
                        DiscountRate: item.DiscountRate || 0,
                        DiscountAmount: netDisc,
                        DiscountMoney: discMoney,
                        VatRate: item.VatRate || 0,
                        VatAmount: vatAmount,
                        TotalAmount: totalAmount,
                        Note: item.Note || '',
                        IsSentToKitchen: item.IsSentToKitchen || 1,
                        PromotionCode: item.PromotionCode || item.promotionCode || null
                    };
                });
                this.orderOpenPayment();
            }
        });
    }

    // 9. Edit Bill (Sửa hóa đơn)
    actionEditBill() {
        if (!this.canEditBill()) return;
        const table = this.getFirstSelectedTable();
        if (!table.CurrentBills || table.CurrentBills.length === 0) return;
        const activeBill = table.CurrentBills[0];

        this.salesService.getCheckItems(activeBill.BillId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.editBillDetail = res.Data;
                this.editBillItems = (res.Data.Items || []).map((item: any) => ({
                    ...item,
                    OriginalQuantity: item.Quantity
                }));
                this.editCustomerCount = res.Data.CustomerCount || 1;
                this.editBillDialogVisible = true;
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    canDecreaseEditItem(item: any): boolean {
        if (!item) return false;
        return item.Quantity > 0;
    }

    canDeleteEditItem(item: any): boolean {
        if (!item) return false;
        return true;
    }

    editIncreaseQty(item: any) {
        item.Quantity += 1;
        item.Amount = Math.round(item.Quantity * item.Price);
        if (item.DiscountRate) {
            const unitDisc = Math.round(item.Price * item.DiscountRate / 100);
            item.DiscountAmount = unitDisc * item.Quantity;
        }
    }

    editDecreaseQty(item: any) {
        if (item && item.Quantity > 0) {
            item.Quantity -= 1;
            item.Amount = Math.round(item.Quantity * item.Price);
            if (item.DiscountRate) {
                const unitDisc = Math.round(item.Price * item.DiscountRate / 100);
                item.DiscountAmount = unitDisc * item.Quantity;
            }
        }
    }

    editRemoveItem(item: any) {
        this.editBillItems = this.editBillItems.filter(x => x !== item);
    }

    editAddMockFood() {
        const foods = [
            { ItemCode: 'SP100', ItemName: 'Bún Chả Nem Cua Bể', Price: 65000 },
            { ItemCode: 'SP101', ItemName: 'Gỏi Cuốn Tôm Thịt', Price: 35000 },
            { ItemCode: 'SP102', ItemName: 'Lẩu Thái Chua Cay', Price: 350000 },
            { ItemCode: 'SP103', ItemName: 'Nước Ép Dưa Hấu', Price: 30000 },
            { ItemCode: 'SP104', ItemName: 'Mực Nướng Muối Ớt', Price: 120000 }
        ];

        const rFood = foods[Math.floor(Math.random() * foods.length)];
        const existing = this.editBillItems.find(x => x.ItemCode === rFood.ItemCode);
        if (existing) {
            existing.Quantity += 1;
            existing.Amount = Math.round(existing.Quantity * existing.Price);
            if (existing.DiscountRate) {
                const unitDisc = Math.round(existing.Price * existing.DiscountRate / 100);
                existing.DiscountAmount = unitDisc * existing.Quantity;
            }
        } else {
            this.editBillItems.push({
                ItemId: '00000000-0000-0000-0000-000000000000',
                ItemCode: rFood.ItemCode,
                ItemName: rFood.ItemName,
                Price: rFood.Price,
                Quantity: 1,
                Amount: rFood.Price,
                DiscountRate: 0,
                DiscountAmount: 0,
                ServedQuantity: 0,
                RemainingQuantity: 1
            });
        }
    }

    getEditTotalAmount(): number {
        return this.editBillItems.reduce((acc, item) => acc + (item.Amount - (item.DiscountAmount || 0)), 0);
    }

    confirmUpdateBill() {
        if (!this.editBillDetail) return;

        if (this.editBillDetail.Status !== 'Reserved') {
            for (const orig of (this.editBillDetail.Items || [])) {
                const current = this.editBillItems.find((x: any) => (x.Id && orig.Id ? x.Id === orig.Id : x.ItemId === orig.ItemId));
                const currentQty = current ? current.Quantity : 0;
                if (currentQty < orig.Quantity) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Không thể lưu hóa đơn',
                        detail: `Chỉ được giảm số lượng món đã gửi bếp khi đơn ở trạng thái Đặt trước! Với đơn đang phục vụ, vui lòng dùng nút 'Hủy món' để in phiếu hủy báo Bếp/Bar. (Món: ${orig.ItemName})`
                    });
                    return;
                }
            }

            for (const item of this.editBillItems) {
                const originalQty = item.OriginalQuantity !== undefined ? item.OriginalQuantity : 0;
                if (originalQty > 0 && item.Quantity < originalQty) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Không thể lưu hóa đơn',
                        detail: `Chỉ được giảm số lượng món đã gửi bếp khi đơn ở trạng thái Đặt trước! Với đơn đang phục vụ, vui lòng dùng nút 'Hủy món' để in phiếu hủy báo Bếp/Bar. (Món: ${item.ItemName})`
                    });
                    return;
                }
            }
        }

        const itemsPayload = this.editBillItems.map(item => ({
            Id: item.Id || null,
            ItemId: item.ItemId,
            ItemCode: item.ItemCode,
            ItemName: item.ItemName,
            UnitName: item.UnitName || 'Đĩa',
            Quantity: item.Quantity,
            UnitPrice: item.Price,
            DiscountRate: item.DiscountRate || 0,
            VatRate: item.VatRate || 0,
            FixedTaxAmount: item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0),
            Note: item.Note
        }));

        const tableIds = this.editBillDetail.TableIds || 
            (this.editBillDetail.TableId ? [this.editBillDetail.TableId] : 
            (this.editBillDetail.Tables?.length ? this.editBillDetail.Tables.map((t: any) => t.TableId || t.Id).filter(Boolean) : []));

        const payload = {
            OrderId: this.editBillDetail.BillId,
            BranchCode: this.selectedBranchCode,
            TableIds: tableIds,
            CustomerCount: this.editCustomerCount,
            WaiterId: this.editBillDetail.WaiterId,
            WaiterName: this.editBillDetail.WaiterName,
            ServiceType: this.editBillDetail.ServiceType || 'DineIn',
            Items: itemsPayload
        };

        this.salesService.saveOrder(payload).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Sửa hóa đơn', detail: 'Cập nhật hóa đơn thành công!' });
                this.editBillDialogVisible = false;
                this.clearSelection();
                this.loadTables();
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    getSelectedTableNamesSummary(): string {
        const selected = this.getSelectedTablesData();
        const names = selected.map(t => t.TableCode || t.TableName).filter(Boolean);
        if (names.length > 0) {
            if (names.length <= 3) {
                return names.join(', ');
            }
            return names.slice(0, 3).join(', ') + '...';
        }
        if (this.currentOrder) {
            return this.currentOrder.TableCode || this.currentOrder.TableName || 'Không có bàn';
        }
        return '';
    }

    getMergeTableNamesSummary(): string {
        return this.mergePreview?.TableNames?.join(' + ') || '';
    }

    toggleMultiSelectMode() {
        this.multiSelectMode.set(!this.multiSelectMode());
        if (!this.multiSelectMode()) {
            this.clearSelection();
        }
    }

    onCanvasClick(event: MouseEvent) {
        this.clearSelection();
        this.isEditingOrder = false;
    }

    selectOrderFromPopup(bill: any) {
        this.isOrderSelectPopupOpen = false;
        this.selectedOrderId = bill.BillId;

        const selected = this.getSelectedTablesData();
        if (selected.length === 1) {
            const selectedTable = selected[0];

            // Fetch the latest order details directly from the server to guarantee fresh kitchen status
            this.salesService.getOrderById(bill.BillId, this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const fullBill = res.Data;
                    fullBill.BillId = fullBill.Id; // normalize ID field for drawer mapping
                    fullBill.TableId = selectedTable.TableId;
                    fullBill.TableName = fullBill.TableName || selectedTable.TableName;
                    this.startOrderEditing(fullBill);
                } else {
                    const fallbackBill = { ...bill };
                    fallbackBill.TableId = selectedTable.TableId;
                    fallbackBill.TableName = fallbackBill.TableName || selectedTable.TableName;
                    this.startOrderEditing(fallbackBill);
                }
            });
        }

        if (this.pendingActionAfterOrderSelect) {
            const action = this.pendingActionAfterOrderSelect;
            this.pendingActionAfterOrderSelect = null;
            action();
        } else {
            this.isEditingOrder = true;
        }
    }

    ensureOrderSelected(table: any, action: () => void): boolean {
        if (!table || !table.CurrentBills || table.CurrentBills.length === 0) return false;
        if (table.CurrentBills.length === 1) {
            this.selectedOrderId = table.CurrentBills[0].BillId;
            return true;
        }
        if (this.selectedOrderId) {
            return true;
        }
        this.activeOrdersOfSelectedTable = table.CurrentBills;
        this.pendingActionAfterOrderSelect = action;
        this.isOrderSelectPopupOpen = true;
        return false;
    }

    getActiveOrderIdForTable(table: any): string | null {
        if (!table || !table.CurrentBills || table.CurrentBills.length === 0) return null;
        if (table.CurrentBills.length === 1) {
            return table.CurrentBills[0].BillId;
        }
        return this.selectedOrderId;
    }

    orderCheckItems() {
        if (this.isButtonDisabled('Kiểm đồ')) return;
        if (!this.currentOrder || !this.currentOrder.BillId) return;
        const billId = this.currentOrder.BillId;

        this.salesService.getCheckItems(billId, this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.checkBillDetail = res.Data;
                this.checkBillItems = (res.Data.Items || []).map((item: any) => {
                    const served = item.ServedQty !== undefined ? item.ServedQty : (item.ServedQuantity !== undefined ? item.ServedQuantity : item.Quantity);
                    const actual = item.IsChecked === 1
                        ? (item.ActualQuantity !== undefined && item.ActualQuantity !== null ? item.ActualQuantity : served)
                        : served;
                    const returned = item.IsChecked === 1
                        ? (item.ReturnedQuantity || 0)
                        : (actual < served ? (served - actual) : 0);
                    return {
                        ...item,
                        Quantity: item.Quantity,
                        SentQty: item.SentQty || 0,
                        ServedQty: served,
                        SoldOutQty: item.SoldOutQty || 0,
                        OriginalServedQty: served,
                        ServedQuantity: actual,
                        ReturnedQuantity: returned
                    };
                });
                this.checkDialogVisible = true;
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
            }
        });
    }

    canCheckItemsForCurrentOrder(): boolean {
        return this.currentOrder && this.currentOrder.BillId && this.currentOrder.BillCode !== 'DRAFT' &&
            ['Reserved', 'Serving', 'WaitingPayment', 'Checked'].includes(this.currentOrder.Status);
    }

    hasMissingRequiredFields(): boolean {
        if (!this.currentOrder) return false;
        if (this.currentOrder.ServiceType === 'Delivery') {
            return !this.phoneNumber || !this.phoneNumber.trim() || !this.deliveryAddress || !this.deliveryAddress.trim();
        }
        return false;
    }

    hasPermission(permission: string | string[]): boolean {
        if (!permission) return true;
        const perms = this.userPermissions() || [];
        const normalized = perms.map(p => p.replace(/\./g, '_').toUpperCase());

        if (
            normalized.includes('SALEROOM_CASHIER_ADMIN')
        ) {
            return true;
        }

        const targets = Array.isArray(permission) ? permission : [permission];
        return targets.some(target => {
            if (target === '*') return true;
            const query = target.replace(/\./g, '_').toUpperCase();
            return normalized.includes(query);
        });
    }

    hasUnfinishedKitchenItems(): boolean {
        if (!this.requireKitchenSend()) return false;
        if (!this.orderItems || this.orderItems.length === 0) return false;
        return this.orderItems.some(item => ((item.ServedQty || 0) + (item.SoldOutQty || 0)) < item.Quantity);
    }

    isButtonDisabled(action: string): boolean {
        if (this.isSendingKitchen()) {
            return action !== 'Đóng';
        }

        if (!this.currentOrder) {
            return action !== 'Đóng';
        }

        const status = this.currentOrder.Status;
        const hasBillId = !!this.currentOrder.BillId;

        const SELL_PERMISSIONS = ['SalesCatalog.DiningTable.Sell', 'SALEROOM_CASHIER_SELL', 'Sales.Sell'];
        const VIEW_BILL_PERMISSIONS = ['SalesCatalog.DiningTable.ViewBill', 'SALEROOM_CASHIER_VIEW_BILL', 'Sales.ViewBill'];
        const CONFIRM_PERMISSIONS = ['SalesCatalog.DiningTable.ConfirmOrder', 'SALEROOM_CASHIER_CONFIRM_ORDER', 'Sales.ConfirmOrder'];

        // If paid or cancelled, all action controls are disabled except Close
        if (status === 'Paid' || status === 'Cancelled') {
            return action !== 'Đóng';
        }

        switch (action) {
            case 'Chọn khuyến mại':
                return !this.hasPermission(SELL_PERMISSIONS);

            case 'Lưu':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (hasBillId) {
                    if (this.isBillLockedForEditing()) return true;
                    return !this.isBillEditUnlocked;
                }
                if (this.hasMissingRequiredFields()) {
                    return true;
                }
                return false;

            case 'Sửa bill':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (!hasBillId) return true;
                if (this.isBillLockedForEditing()) return true;
                return false;

            case 'Đặt trước':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (this.hasMissingRequiredFields()) {
                    return true;
                }
                return hasBillId;

            case 'Mở bàn':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                return !hasBillId || status !== 'Reserved';

            case 'Gửi bếp':
            case 'Gửi Bếp/Bar':
                if (this.isOrderInLockedPeriod()) return this.isButtonDisabled('Lưu');
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (!this.userPermissions().includes('Sales.SendKitchen') && !this.hasPermission('SALEROOM_CASHIER_ADMIN')) {
                    return true;
                }
                if (this.hasMissingRequiredFields()) {
                    return true;
                }
                if (this.orderItems.length === 0) {
                    return true;
                }
                if (status === 'WaitingConfirm') {
                    return false;
                }
                if (hasBillId) {
                    const hasUnsentItems = this.orderItems.some(item => {
                        const sentQty = (item.SentQty !== undefined && item.SentQty !== null)
                            ? item.SentQty
                            : (item.IsSentToKitchen === 1 ? item.Quantity : 0);
                        return item.Quantity > sentQty;
                    });
                    return !hasUnsentItems;
                }
                return false;

            case 'Kiểm đồ':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (!hasBillId) return true;
                return !this.canCheckItemsForCurrentOrder();

            case 'Hủy bill':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                return !hasBillId;

            case 'In tạm tính':
                if (!this.hasPermission(VIEW_BILL_PERMISSIONS)) return true;
                if (!hasBillId) return true;
                return false;

            case 'Thanh toán':
                if (!this.hasPermission(SELL_PERMISSIONS)) return true;
                if (!hasBillId) return true;
                return false;

            case 'Xác nhận đơn':
                return !this.hasPermission(CONFIRM_PERMISSIONS);

            case 'Đóng':
                return false;

            default:
                return true;
        }
    }

    parseDateOnly(val: any): Date | null {
        if (!val) return null;
        if (val instanceof Date) {
            return new Date(val.getFullYear(), val.getMonth(), val.getDate());
        }
        if (typeof val === 'string') {
            const trimmed = val.trim();
            const dmy = trimmed.match(/^(\d{1,2})[-/](\d{1,2})[-/](\d{4})/);
            if (dmy) {
                return new Date(Number(dmy[3]), Number(dmy[2]) - 1, Number(dmy[1]));
            }
            const ymd = trimmed.match(/^(\d{4})[-/](\d{1,2})[-/](\d{1,2})/);
            if (ymd) {
                return new Date(Number(ymd[1]), Number(ymd[2]) - 1, Number(ymd[3]));
            }
            const parsed = new Date(trimmed);
            if (!isNaN(parsed.getTime())) {
                return new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
            }
        }
        return null;
    }

    getEffectiveLockDate(): Date | null {
        if (!this.invoiceLockEnabled()) return null;
        if (this.invoiceLockAuto()) {
            const today = new Date();
            const days = this.invoiceLockAutoDays() || 0;
            const automaticDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - days, 0, 0, 0, 0);
            const configuredDate = this.parseDateOnly(this.invoiceLockDate());
            // The settings screen also saves the calculated date. Keep the
            // displayed cutoff when it is later than the locally calculated one.
            return configuredDate && configuredDate > automaticDate ? configuredDate : automaticDate;
        }
        const raw = this.invoiceLockDate();
        if (!raw) return null;
        return this.parseDateOnly(raw);
    }

    getFormattedEffectiveLockDate(): string {
        const d = this.getEffectiveLockDate();
        if (!d) return '';
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        return `${day}/${month}/${year}`;
    }

    isBillLockedForEditing(): boolean {
        if (!this.invoiceLockEnabled()) return false;
        if (!this.currentOrder?.BillId) return false;

        const lockDate = this.getEffectiveLockDate();
        if (!lockDate) return false;

        const rawBillDate = this.currentOrder?.OpenTime
            || this.currentOrder?.OrderDate
            || this.currentOrder?.CreatedOnDate
            || this.openTime;
        const billDate = this.parseDateOnly(rawBillDate);
        if (!billDate) return false;

        return billDate.getTime() <= lockDate.getTime();
    }

    onEditBillClick() {
        if (this.isBillLockedForEditing()) {
            const lockDateStr = this.getFormattedEffectiveLockDate();
            this.messageService.add({
                severity: 'warn',
                summary: 'Không thể sửa bill',
                detail: `Hoá đơn đã bị khóa sửa do ngày mở bàn thuộc thời gian khóa (đến ngày ${lockDateStr}).`
            });
            return;
        }

        this.isBillEditUnlocked = true;
        this.openFoodDialog();
        if (this.isMobile()) {
            this.mobileFoodTab = 'menu';
        }
    }

    openFoodDialog() {
        if (!this.hasPermission(['SalesCatalog.DiningTable.Sell', 'SALEROOM_CASHIER_SELL', 'Sales.Sell'])) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Không có quyền',
                detail: 'Bạn không có quyền thực hiện bán hàng!'
            });
            return;
        }

        this.foodSearchQuery = '';
        this.level1Filter = 'Popular';
        this.level2Filter = '';
        this.loadMenuItems();
        this.refreshEmployeesAndCustomers();
        this.loadAvailableEmptyTables();
        // On mobile: go to order tab if order has existing items, otherwise menu tab
        this.mobileFoodTab = (this.isMobile() && this.orderItems.length > 0) ? 'order' : 'menu';
        this.foodDialogVisible = true;
    }

    refreshEmployeesAndCustomers() {
        if (!this.selectedBranchCode) return;
        this.salesService.getEmployees(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.waiters = (res.Data || []).map((w: any) => ({
                    ...w,
                    DisplayLabel: `${w.Code} - ${w.Name}`
                }));
            }
        });
        this.salesService.getCustomers(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const mapped = (res.Data || []).map((c: any) => ({
                    ...c,
                    DisplayLabel: `${c.Code} - ${c.Name}${c.PhoneNumber ? ' - ' + c.PhoneNumber : ''}`
                }));
                const selected = this.customers().find(c => c.Id === this.orderCustomerId);
                this.customers.set(selected && !mapped.some((c: any) => c.Id === selected.Id)
                    ? [...mapped, selected] : mapped);
            }
        });
    }

    onWaiterSearch(event: any) {
        const query = event.filter || '';
        if (this.waiterSearchTimeout) {
            clearTimeout(this.waiterSearchTimeout);
        }
        this.waiterSearchTimeout = setTimeout(() => {
            this.salesService.getEmployees(this.selectedBranchCode, query).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    this.waiters = (res.Data || []).map((w: any) => ({
                        ...w,
                        DisplayLabel: `${w.Code} - ${w.Name}`
                    }));
                }
            });
        }, 300);
    }

    onCustomerSearch(event: any) {
        const query = event.filter || '';
        if (this.customerSearchTimeout) {
            clearTimeout(this.customerSearchTimeout);
        }
        this.customerSearchTimeout = setTimeout(() => {
            this.salesService.getCustomers(this.selectedBranchCode, query).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const mapped = (res.Data || []).map((c: any) => ({
                        ...c,
                        DisplayLabel: `${c.Code} - ${c.Name}${c.PhoneNumber ? ' - ' + c.PhoneNumber : ''}`
                    }));
                    const selected = this.customers().find(c => c.Id === this.orderCustomerId);
                    this.customers.set(selected && !mapped.some((c: any) => c.Id === selected.Id)
                        ? [...mapped, selected] : mapped);
                }
            });
        }, 300);
    }

    loadMenuItems() {
        if (!this.selectedBranchCode) return;

        // If there's a search query, search across all items in the branch, ignoring Level 1 and Level 2 filters
        if (this.foodSearchQuery && this.foodSearchQuery.trim()) {
            this.salesService.getMenuItemsFilter(this.selectedBranchCode, this.foodSearchQuery.trim(), '', '').subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const content = (res.Data || []).filter((mi: any) => mi.IsAvailable !== false);
                    const mapped = content.map((mi: any) => ({
                        ItemId: mi.Id,
                        ItemCode: mi.Code,
                        ItemName: mi.Name,
                        MenuGroupId: mi.MenuGroupId,
                        CourseOrder: mi.CourseOrder,
                        DisplayPrice: mi.Amount || mi.SalePrice || 0,
                        Price: mi.SalePrice || 0,
                        VatRate: mi.TaxRatePercent || 0,
                        UnitName: mi.UnitName || mi.unitName || '',
                        Description: mi.Description,
                        ImageUrl: mi.ImageUrl,
                        OptionGroupsJson: mi.OptionGroupsJson,
                        IsAvailable: true
                    }));
                    this.foodFilteredMenuItems = this.sortMenuItems(mapped);
                }
            });
            return;
        }

        // If no search query, filter by Level 1 and Level 2
        if (this.level1Filter === 'Popular') {
            this.salesService.getTopUsedMenuItems(this.selectedBranchCode).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const content = (res.Data || []).filter((mi: any) => mi.IsAvailable !== false);
                    const mapped = content.map((mi: any) => ({
                        ItemId: mi.Id,
                        ItemCode: mi.Code,
                        ItemName: mi.Name,
                        MenuGroupId: mi.MenuGroupId,
                        CourseOrder: mi.CourseOrder,
                        UnitName: mi.UnitName || mi.unitName || '',
                        Quantity: 0,
                        DisplayPrice: mi.Amount || mi.SalePrice || 0,
                        Price: mi.SalePrice || 0,
                        FixedTaxAmount: mi.FixedTaxAmount || 0,
                        VatRate: mi.TaxRatePercent || 0,
                        Amount: 0,
                        ServedQuantity: 0,
                        RemainingQuantity: 0,
                        Note: null,
                        Description: mi.Description,
                        ImageUrl: mi.ImageUrl,
                        OptionGroupsJson: mi.OptionGroupsJson,
                        IsAvailable: true
                    }));
                    this.topUsedMenuItems = this.sortMenuItems(mapped);
                    this.foodFilteredMenuItems = this.topUsedMenuItems;
                }
            });
        } else {
            const typeParam = this.level1Filter;
            const groupParam = this.level2Filter || undefined;
            this.salesService.getMenuItemsFilter(this.selectedBranchCode, '', typeParam, groupParam).subscribe((res: any) => {
                if (res?.Code === 200 && res.Data) {
                    const content = (res.Data || []).filter((mi: any) => mi.IsAvailable !== false);
                    const mapped = content.map((mi: any) => ({
                        ItemId: mi.Id,
                        ItemCode: mi.Code,
                        ItemName: mi.Name,
                        MenuGroupId: mi.MenuGroupId,
                        CourseOrder: mi.CourseOrder,
                        DisplayPrice: mi.Amount || mi.SalePrice || 0,
                        Price: mi.SalePrice || 0,
                        VatRate: mi.TaxRatePercent || 0,
                        UnitName: mi.UnitName || mi.unitName || '',
                        Description: mi.Description,
                        ImageUrl: mi.ImageUrl,
                        OptionGroupsJson: mi.OptionGroupsJson,
                        IsAvailable: true
                    }));
                    this.foodFilteredMenuItems = this.sortMenuItems(mapped);
                }
            });
        }
    }

    sortMenuItems(items: any[]): any[] {
        return [...items].sort((a: any, b: any) => {
            const valA = Number(a.CourseOrder);
            const valB = Number(b.CourseOrder);
            const ordA = (!isNaN(valA) && valA > 0) ? valA : 999999;
            const ordB = (!isNaN(valB) && valB > 0) ? valB : 999999;
            if (ordA !== ordB) return ordA - ordB;

            return (a.ItemName || '').localeCompare(b.ItemName || '');
        });
    }

    filterFoodItems() {
        // Debounce 350ms to avoid API spam on every keystroke
        if (this.foodSearchDebounceTimer) {
            clearTimeout(this.foodSearchDebounceTimer);
        }
        this.foodSearchDebounceTimer = setTimeout(() => {
            this.foodSearchDebounceTimer = null;
            this.loadMenuItems();
        }, 350);
    }

    openSoldOutDialog() {
        if (!this.selectedBranchCode) return;
        this.salesService.getMenuItemsFilter(this.selectedBranchCode).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                const unavailable = (res.Data || []).filter((mi: any) => mi.IsAvailable === false);
                this.soldOutMenuItems.set(unavailable);
                this.showSoldOutDialog = true;
            }
        });
    }

    quickRestoreAvailable(item: any) {
        const itemId = item.Id || item.id || item.ItemId;
        this.menuItemService.toggleAvailability(itemId, true).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Đã bật lại hiển thị món ăn!'
                });
                this.soldOutMenuItems.update(list => list.filter(x => (x.Id || x.id || x.ItemId) !== itemId));
                this.loadMenuItems();
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: res?.Message || 'Cập nhật thất bại!'
                });
            }
        });
    }

    toggleDeliveryInfo() {
        this.isDeliveryInfoCollapsed = !this.isDeliveryInfoCollapsed;
    }

    onCustomerChange() {
        const customer = this.customers().find(c => c.Id === this.orderCustomerId);
        if (customer) {
            if (!this.phoneNumber || !this.phoneNumber.trim()) {
                this.phoneNumber = customer.MobilePhone || customer.PhoneNumber || '';
            }
            if (!this.deliveryAddress || !this.deliveryAddress.trim()) {
                this.deliveryAddress = customer.Address || '';
            }
        }
        // Giữ nguyên số điện thoại và địa chỉ giao hàng đã nhập khi chọn Khách lẻ (không bị xóa trắng)

        // Refresh against the unsaved customer selection (including no customer),
        // rather than the customer currently persisted on the order.
        const orderId = this.selectedOrderId || this.currentOrder?.BillId || this.currentOrder?.Id;
        if (!orderId) {
            this.promotionsList.set([]);
            return;
        }

        this.loadingPromotions.set(true);
        this.salesService.getAvailablePromotions(orderId, this.orderCustomerId).subscribe({
            next: (res: any) => {
                this.loadingPromotions.set(false);
                if (res?.Code !== 200) return;
                let list = res?.Data || res?.data || [];
                if (!this.therapyVoucherCode || !this.therapyVoucherCode.trim()) {
                    list = list.filter((p: any) => !p.RequireTherapyVoucher && !p.requireTherapyVoucher);
                }
                this.promotionsList.set(list);

                const appliedCode = this.selectedPromotionCode;
                const stillEligible = !appliedCode || list.some((p: any) =>
                    (p.Code || p.code) === appliedCode);
                if (!stillEligible) {
                    this.selectedPromotionCode = null;
                    this.appliedOrderLevelPromoDiscount = 0;
                    (this.orderItems || []).forEach((item: any) => {
                        if (item.PromotionCode === appliedCode) {
                            item.PromotionCode = null;
                            item.DiscountAmount = 0;
                            item.DiscountRate = 0;
                        }
                    });
                    this.recalculateAllItemTotals();
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Đã cập nhật tạm tính',
                        detail: `Khuyến mại '${appliedCode}' không còn đủ điều kiện với khách hàng hiện tại. Bill sẽ được cập nhật khi bạn nhấn Lưu.`
                    });
                }
            },
            error: () => {
                this.loadingPromotions.set(false);
                this.promotionsList.set([]);
            }
        });
    }

    getItemImageUrl(url: string | null | undefined): string {
        if (!url) {
            return '/food-placeholder.png';
        }
        if (url.startsWith('http://') || url.startsWith('https://')) {
            return url;
        }
        return `${this.apiUrl}minio/download?objectName=${encodeURIComponent(url)}`;
    }

    onItemImageError(event: Event): void {
        const image = event.target as HTMLImageElement;
        if (!image.src.endsWith('/food-placeholder.png')) {
            image.src = '/food-placeholder.png';
            image.style.width = '120%';
            image.style.height = '120%';
            image.style.objectFit = 'contain';
            image.style.backgroundColor = '#f4f7f0';
        }
    }

    getItemQuantityInOrder(item: any): number {
        const orderItem = this.orderItems.find(x => x.ItemId === item.ItemId);
        return orderItem ? orderItem.Quantity : 0;
    }

    orderDecreaseQtyById(itemId: string) {
        const existing = this.orderItems.find(x => x.ItemId === itemId);
        if (existing) {
            this.orderDecreaseQty(existing);
        }
    }

    orderChangeQtyById(itemId: string, event: any) {
        const existing = this.orderItems.find(x => x.ItemId === itemId);
        if (existing) {
            const rawVal = event?.target?.value;
            let qty = Math.floor(Number(rawVal));
            if (isNaN(qty) || qty < 1) {
                qty = 1;
            }
            this.orderChangeQty({ item: existing, quantity: qty });
            if (event?.target) {
                event.target.value = qty;
            }
        }
    }

    canDecreaseItemById(itemId: string): boolean {
        const existing = this.orderItems.find(x => x.ItemId === itemId);
        if (!existing) return false;
        return existing.Quantity > 0;
    }

    clearOrderItems() {
        this.orderItems = [];
        this.recalculateAllItemTotals();
    }

    orderSaveFromPopup() {
        this.orderSave();
        this.foodDialogVisible = false;
    }

    orderReserveFromPopup() {
        this.orderReserve();
        this.foodDialogVisible = false;
    }

    orderCheckInFromPopup() {
        this.orderCheckIn();
        this.foodDialogVisible = false;
    }

    orderSendKitchenFromPopup() {
        this.orderSendKitchen();
    }

    orderCheckItemsFromPopup() {
        this.orderCheckItems();
        this.foodDialogVisible = false;
    }

    orderPrintTemporaryFromPopup() {
        this.orderPrintTemporary();
        this.foodDialogVisible = false;
    }

    orderOpenPaymentFromPopup() {
        this.foodDialogVisible = false;
        this.orderOpenPayment();
    }

    printTemporaryBillWithQr(order: any, billId: string, callback: (qrUrl?: string) => void, forcedAmount?: number) {
        const totalAmt = forcedAmount !== undefined ? forcedAmount : this.getOrderTotalAmount();
        const transactionId = order.BillCode || `PAY-${billId}-${Date.now()}`;
        const qrPayload = {
            TransactionId: transactionId,
            BranchCode: this.selectedBranchCode,
            AmountDue: totalAmt,
            CustomerCode: this.orderCustomerId || '',
            IsFastCreate: false,
            CustomerName: '',
            CreatedByUserName: '',
            CreatedByCode: ''
        };

        this.paymentService.createTransaction(qrPayload).subscribe({
            next: (qrRes: any) => {
                const qrUrl = qrRes?.Data?.qr_image_url ?? qrRes?.Data?.qrImageUrl ?? qrRes?.Data?.QRCode ?? null;
                callback(qrUrl);
            },
            error: () => {
                callback();
            }
        });
    }

    getKitchenStatusText(item: any): string {
        if (!item.SentQty && !item.ServedQty) return 'Chưa gửi';
        const sentQty = item.SentQty || item.ServedQty || 0;
        let text = `Đã gửi: ${sentQty}`;
        if (item.SoldOutQty > 0) text += ` | Hết (${item.SoldOutQty})`;
        return text;
    }

    getKitchenStatusClass(item: any): string {
        if (!item.SentQty) return 'bg-surface-100 text-surface-600 border border-surface-200 dark:bg-surface-800 dark:text-surface-400 dark:border-surface-700';
        if (item.KitchenStatus === 'FullyServed') return 'bg-green-50 text-green-700 border border-green-200 dark:bg-green-950/20 dark:text-green-400 dark:border-green-800';
        if (item.KitchenStatus === 'FullySoldOut') return 'bg-red-50 text-red-700 border border-red-200 dark:bg-red-950/20 dark:text-red-400 dark:border-red-800';
        if (item.KitchenStatus === 'PartiallyServed' || item.KitchenStatus === 'PartiallySoldOut') return 'bg-amber-50 text-amber-700 border border-amber-200 dark:bg-amber-950/20 dark:text-amber-400 dark:border-amber-800';
        return 'bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/20 dark:text-blue-400 dark:border-blue-800';
    }

    /**
     * Handles incoming branch sales events from SignalR for realtime multi-device sync.
     * Uses delta/patch approach: update in-memory state first (0ms), reload from server only when necessary.
     */
    private _handleBranchSalesEvent(event: ISalesRealtimeEvent): void {
        const { eventType, orderId, newStatus, serviceType } = event;
        const targetId = orderId ? String(orderId).trim().toLowerCase() : '';

        switch (eventType) {
            case 'QrOrderCreated': {
                // Customer on mobile QR placed an order
                this.waitingConfirmCount.update(c => c + 1);
                this._tableReloadTrigger$.next();
                break;
            }

            case 'QrOrderMerged':
            case 'OrderConfirmed': {
                // DELTA: Remove processed WaitingConfirm order from in-memory list immediately (0 API calls)
                if (targetId) {
                    const prevLen = this.serviceTypeOrders.length;
                    this.serviceTypeOrders = this.serviceTypeOrders.filter((o: any) => {
                        const oId = String(o.OrderId || o.BillId || o.Id || '').trim().toLowerCase();
                        return oId !== targetId;
                    });
                    if (this.serviceTypeOrders.length < prevLen) {
                        this.waitingConfirmCount.update(c => Math.max(0, c - 1));
                    }
                }
                // Schedule debounced table map & order list reload
                this._tableReloadTrigger$.next();
                break;
            }

            case 'OrderPaid': {
                if (targetId && this.isEditingOrder) {
                    const isLocalPayment = Boolean(targetId && (this.localPaymentOrderIds.get(targetId) ?? 0) > Date.now());
                    const openId = String(this.currentOrder?.BillId || this.selectedOrderId || '').trim().toLowerCase();
                    const isCurrentOrder = Boolean(openId && (openId === targetId || this.currentOrder?.OrderCode === event.orderCode));
                    const isLocallyPaying = this.isPaying() || isLocalPayment || (this._locallyHandledPaidOrderId && this._locallyHandledPaidOrderId === targetId);
                    if (isCurrentOrder && !isLocallyPaying) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Đã thanh toán',
                            detail: `Hóa đơn ${event.orderCode || ''} vừa được thu tiền từ thiết bị khác!`,
                            life: 8000
                        });
                        this.isEditingOrder = false;
                        this.clearSelection();
                    }
                }
                // Debounced reload to update table colors
                this._tableReloadTrigger$.next();
                break;
            }

            case 'OrderCancelled': {
                // Remove from any service type order list
                if (targetId) {
                    this.serviceTypeOrders = this.serviceTypeOrders.filter((o: any) => {
                        const oId = String(o.OrderId || o.BillId || o.Id || '').trim().toLowerCase();
                        return oId !== targetId;
                    });
                    this.waitingConfirmCount.update(c => {
                        const newCount = this.serviceTypeOrders.filter(
                            (o: any) => o.Status === 'WaitingConfirm'
                        ).length;
                        return newCount;
                    });
                    // Warn if editing the cancelled order
                    const isLocalCancellation = (this.localCancellationOrderIds.get(targetId) ?? 0) > Date.now();
                    if (this.isEditingOrder && !isLocalCancellation) {
                        const openId = String(this.currentOrder?.BillId || this.selectedOrderId || '').trim().toLowerCase();
                        const isCurrentOrder = Boolean(openId && (openId === targetId || this.currentOrder?.OrderCode === event.orderCode));
                        const isLocallyCancelling = Boolean(this._locallyHandledCancelledOrderId && this._locallyHandledCancelledOrderId === targetId);
                        if (isCurrentOrder && !isLocallyCancelling) {
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Đơn hàng bị hủy',
                                detail: `Hóa đơn ${event.orderCode || ''} vừa bị hủy từ thiết bị khác!`,
                                life: 8000
                            });
                            this.isEditingOrder = false;
                            this.clearSelection();
                        }
                    }
                }
                this._tableReloadTrigger$.next();
                break;
            }

            case 'TableOpened':
            case 'TableTransferred':
            case 'TablesMerged':
            case 'TableStatusChanged':
            case 'OrderUpdated': {
                // These change floor plan — use debounced reload
                this._tableReloadTrigger$.next();
                break;
            }

            case 'Reconcile': {
                // Full sync after coming back from sleep/background
                this._tableReloadTrigger$.next();
                break;
            }
        }
    }

    private async startSignalR() {
        if (!this.selectedBranchCode) return;
        const token = localStorage.getItem('authToken');
        if (!token) return;

        const apiBaseUrl = environment.apiUrl.replace('/api/', '');
        const hubUrl = `${apiBaseUrl}/kitchenHub`;

        this.kitchenConnection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token || '',
                withCredentials: true,
                skipNegotiation: false,
                transport: HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([1000, 3000, 5000, 10000])
            .configureLogging(LogLevel.Warning)
            .build();

        const conn = this.kitchenConnection;
        if (!conn) return;

        conn.on('KitchenRequestsUpdated', () => {
            console.log('[Cashier Kitchen Connection] KitchenRequestsUpdated received. Reloading tables...');
            this.loadTables();

            // If the cashier currently has an order open, reload its details to show updated statuses in-place
            if (this.isEditingOrder && this.selectedOrderId) {
                this.salesService.getOrderById(this.selectedOrderId, this.selectedBranchCode).subscribe((orderRes: any) => {
                    if (orderRes?.Code === 200 && orderRes.Data) {
                        this.startOrderEditing(orderRes.Data);
                    }
                });
            }
        });

        try {
            await conn.start();
            await conn.invoke('JoinBranch', this.selectedBranchCode);
            console.log('[Cashier Kitchen Connection] SignalR connected and joined branch successfully');
        } catch (err) {
            console.error('[Cashier Kitchen Connection] SignalR connection failed:', err);
        }
    }

    private async stopSignalR() {
        const conn = this.kitchenConnection;
        if (conn) {
            try {
                if (conn.state === HubConnectionState.Connected && this.selectedBranchCode) {
                    await conn.invoke('LeaveBranch', this.selectedBranchCode);
                }
                await conn.stop();
            } catch (err) {
                console.warn('[Cashier Kitchen Connection] Error while stopping hub connection:', err);
            } finally {
                this.kitchenConnection = null;
            }
        }
    }

    openPromotionSelector() {
        const orderId = this.selectedOrderId || this.currentOrder?.BillId || this.currentOrder?.Id;
        if (!orderId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng gửi bếp/bar hoặc lưu đơn trước khi chọn khuyến mại!' });
            return;
        }
        this.loadingPromotions.set(true);
        this.promoDialogVisible.set(true);
        this.salesService.getAvailablePromotions(orderId, this.orderCustomerId).subscribe({
            next: (res: any) => {
                this.loadingPromotions.set(false);
                let list = res?.Data || res?.data || [];
                // Nếu hiện tại đơn hàng không có mã phiếu trị liệu thì loại bỏ các CTKM yêu cầu phiếu trị liệu
                if (!this.therapyVoucherCode || !this.therapyVoucherCode.trim()) {
                    list = list.filter((p: any) => !p.RequireTherapyVoucher && !p.requireTherapyVoucher);
                }
                this.promotionsList.set(list);
            },
            error: () => {
                this.loadingPromotions.set(false);
            }
        });
    }

    applyPromotionCode(code: string | null) {
        this.selectedPromotionCode = code;
        this.promoDialogVisible.set(false);

        const orderId = this.selectedOrderId || this.currentOrder?.BillId || this.currentOrder?.Id;
        const payload = this.getSavePayload(orderId);
        this.salesService.saveOrder(payload).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.messageService.add({ severity: 'success', summary: 'Khuyến mại', detail: code ? `Đã áp dụng CTKM '${code}'!` : 'Đã hủy áp dụng khuyến mại!' });
                if (orderId) {
                    this.salesService.getOrderById(orderId, this.selectedBranchCode).subscribe((orderRes: any) => {
                        if (orderRes?.Code === 200 && orderRes.Data) {
                            this.startOrderEditing(orderRes.Data);
                        }
                    });
                }
            } else {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Không thể áp dụng khuyến mại này' });
            }
        });
    }

}

