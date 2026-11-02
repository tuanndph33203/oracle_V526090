import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { ToastModule } from 'primeng/toast';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { SalesService } from '../../../dining-table/services/sales';
import { MessageService } from 'primeng/api';
import { PaymentRealtimeService } from '@/core/services/payment-realtime';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

import { InputDatePicker } from '@/shared/ui/form/input-date-picker/input-date-picker';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { PageHeader } from '@/shared/ui/layout/page-header/page-header';
import { ITableAction, ITableConfig } from '@/core/models/common';
import { PaginatorState } from 'primeng/paginator';

import { DialogService } from 'primeng/dynamicdialog';
import { OrderDetailsDialog } from '../../components/order-details-dialog/order-details-dialog';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

@Component({
    selector: 'app-orders',
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ButtonModule,
        SelectModule,
        DialogModule,
        TableModule,
        InputTextModule,
        DatePickerModule,
        ToastModule,
        InputDatePicker,
        TableMeta,
        PageHeader
    ],
    providers: [DialogService],
    templateUrl: './orders.html',
    styleUrl: './orders.scss'
})
export class Orders implements OnInit {
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    private salesService = inject(SalesService);
    private messageService = inject(MessageService);
    private dialogService = inject(DialogService);
    private paymentRealtimeService = inject(PaymentRealtimeService);
    private readonly _ordersReloadTrigger$ = new Subject<void>();

    branchUser = signal<any[]>([]);
    selectedBranchCode = '';
    ordersList = signal<any[]>([]);
    selectedOrder: any = null;
    detailDialogVisible = false;
    loading = signal<boolean>(false);

    // Filter properties
    searchQuery = '';
    selectedStatus = 'All';
    dateRange: [Date | null, Date | null] = [new Date(), new Date()];

    // Table Meta Column Configurations
    colsTemp = signal<ITableConfig[]>([
        {
            label: 'Mã đơn',
            visible: true,
            field: 'OrderCode',
            type: 'text',
            minWidth: '150px'
        },
        {
            label: 'Giờ vào',
            visible: true,
            field: 'OpenTime',
            type: 'dateHour',
            minWidth: '160px'
        },
        {
            label: 'Bàn / Loại đơn',
            visible: true,
            field: 'ServiceType',
            type: 'transform',
            transformRowFn: (row: any) => {
                if (row.ServiceType === 'TakeAway') {
                    return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-green-50 text-green-700 dark:bg-green-950/30 dark:text-green-300 border border-green-200 dark:border-green-800 text-xs font-bold"><i class="pi pi-shopping-bag text-xs"></i> Mang về</span>';
                }
                if (row.ServiceType === 'Delivery') {
                    return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-blue-50 text-blue-700 dark:bg-blue-950/30 dark:text-blue-300 border border-blue-200 dark:border-blue-800 text-xs font-bold"><i class="pi pi-truck text-xs"></i> Giao hàng</span>';
                }
                return `<span class="font-bold text-surface-900 dark:text-surface-50">${row.TableNames || '-'}</span>`;
            },
            minWidth: '160px'
        },
        {
            label: 'Phục vụ',
            visible: true,
            field: 'WaiterName',
            type: 'text',
            minWidth: '140px'
        },
        {
            label: 'Số khách',
            visible: true,
            field: 'CustomerCount',
            type: 'number',
            align: 'center',
            minWidth: '100px'
        },
        {
            label: 'Tổng tiền',
            visible: true,
            field: 'TotalAmount',
            type: 'currency',
            align: 'right',
            minWidth: '150px'
        },
        {
            label: 'Trạng thái',
            visible: true,
            field: 'Status',
            type: 'severity',
            align: 'center',
            minWidth: '150px',
            severity: {
                Serving: { label: 'Đang phục vụ', severity: 'info' },
                Checked: { label: 'Đã kiểm đồ', severity: 'info' },
                WaitingPayment: { label: 'Chờ thanh toán', severity: 'warn' },
                WaitingConfirm: { label: 'Chờ xác nhận', severity: 'warn' },
                Reserved: { label: 'Đặt trước', severity: 'info' },
                Paid: { label: 'Đã thanh toán', severity: 'success' },
                Completed: { label: 'Đã thanh toán', severity: 'success' },
                Cancelled: { label: 'Đã hủy', severity: 'danger' },
                ApprovedCancelled: { label: 'Đã duyệt hủy', severity: 'danger' },
                Merged: { label: 'Đã gộp', severity: 'secondary' }
            }
        },
        {
            label: 'Hành động',
            visible: true,
            field: 'action',
            type: 'action',
            width: '120px',
            actionButtons: [
                {
                    label: 'Xem',
                    icon: 'pi pi-eye',
                    type: 'edit',
                }
            ]
        }
    ]);

    // Pagination properties
    page = signal<number>(1);
    size = signal<number>(10);
    totalElements = signal<number>(0);

    statusFilters = [
        { label: 'Tất cả', value: 'All' },
        { label: 'Đang phục vụ', value: 'Serving' },
        { label: 'Đã kiểm đồ', value: 'Checked' },
        { label: 'Chờ thanh toán', value: 'WaitingPayment' },
        { label: 'Đã thanh toán', value: 'Paid' },
        { label: 'Đã hủy', value: 'Cancelled' },
        { label: 'Đã duyệt hủy', value: 'ApprovedCancelled' }
    ];

    ngOnInit() {
        // Load user branches from store
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            this.branchUser.set(branches || []);
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
                } else {
                    this.loadOrders();
                }
            }
        });

        // Debounced reload for realtime multi-device sync
        this._ordersReloadTrigger$.pipe(
            debounceTime(500),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe(() => this.loadOrders());

        this.paymentRealtimeService.salesEvent$.pipe(
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((event) => {
            const reloadTriggers = ['OrderPaid', 'OrderCancelled', 'OrderConfirmed', 'QrOrderMerged'];
            if (reloadTriggers.includes(event.eventType)) {
                this._ordersReloadTrigger$.next();
            }
        });
    }

    onBranchChange() {
        this.selectedOrder = null;
        this.page.set(1);
        if (this.selectedBranchCode) {
            void this.paymentRealtimeService.updateBranchSubscription([this.selectedBranchCode]);
        }
        this.loadOrders();
    }

    loadOrders() {
        if (!this.selectedBranchCode) return;

        this.loading.set(true);

        const payload: any = {
            BranchCode: this.selectedBranchCode,
            Status: this.selectedStatus === 'All' ? null : this.selectedStatus,
            TextSearch: this.searchQuery.trim() || null,
            FromDate: toLocalISOString(this.dateRange?.[0]) ?? null,
            ToDate: toLocalISOString(this.dateRange?.[1]) ?? null,
            Page: this.page(),
            Size: this.size()
        };

        this.salesService.getOrderHistory(payload).subscribe({
            next: (res: any) => {
                this.loading.set(false);
                if (res?.Code === 200 && res.Data) {
                    this.ordersList.set(res.Data.Content || []);
                    this.totalElements.set(res.Data.TotalElements || 0);

                    // Keep selected order in sync if it's still in the list
                    if (this.selectedOrder) {
                        const updated = (res.Data.Content || []).find((x: any) => x.OrderId === this.selectedOrder.OrderId);
                        this.selectedOrder = updated || null;
                    }
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message || 'Lấy lịch sử đơn hàng thất bại!' });
                }
            },
            error: (err: any) => {
                this.loading.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err.message || 'Lỗi kết nối máy chủ!' });
            }
        });
    }

    selectOrder(order: any) {
        this.selectedOrder = order;
        const branchName = this.branchUser().find(b => b.Code === this.selectedBranchCode)?.Name || '';
        this.dialogService.open(OrderDetailsDialog, {
            data: { 
                order,
                branchCode: this.selectedBranchCode,
                branchName: branchName
            },
            closable: true,
            header: `Chi tiết hóa đơn - ${order.OrderCode}`,
            width: '840px',
            modal: true,
            dismissableMask: true
        });
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        this.selectOrder(row);
    }

    onPageChange(event: PaginatorState | any) {
        if (event.page !== undefined) {
            this.page.set(event.page + 1);
            if (event.rows) this.size.set(event.rows);
        } else if (event.first !== undefined && event.rows) {
            const newPage = (event.first / event.rows) + 1;
            this.page.set(newPage);
            this.size.set(event.rows);
        }
        this.loadOrders();
    }

    resetFilters() {
        this.searchQuery = '';
        this.selectedStatus = 'All';
        this.dateRange = [new Date(), new Date()];
        this.page.set(1);
        this.loadOrders();
    }

    getStatusText(status: string): string {
        if (!status) return '';
        const upper = status.toUpperCase();
        switch (upper) {
            case 'SERVING': return 'Đang phục vụ';
            case 'CHECKED': return 'Đã kiểm đồ';
            case 'WAITINGPAYMENT': return 'Chờ thanh toán';
            case 'RESERVED': return 'Đặt trước';
            case 'PAID':
            case 'COMPLETED': return 'Đã thanh toán';
            case 'CANCELLED': return 'Đã hủy';
            case 'APPROVEDCANCELLED': return 'Đã duyệt hủy';
            case 'MERGED': return 'Đã gộp';
            case 'WAITINGCONFIRM': return 'Chờ xác nhận';
            case 'DRAFT': return 'Bản nháp';
            default: return status;
        }
    }

    getStatusBadgeClass(status: string): string {
        if (!status) return 'bg-surface-100 text-surface-700 border border-surface-200';
        const upper = status.toUpperCase();
        switch (upper) {
            case 'SERVING':
            case 'ĐANG PHỤC VỤ':
                return 'bg-primary/10 text-primary border border-primary/20 dark:bg-primary/20 dark:text-primary-400 dark:border-primary/40';
            case 'CHECKED':
            case 'ĐÃ KIỂM ĐỒ':
                return 'bg-blue-50 text-blue-700 border border-blue-300 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800';
            case 'WAITINGPAYMENT':
            case 'CHỜ THANH TOÁN':
                return 'bg-amber-50 text-amber-700 border border-amber-300 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800';
            case 'WAITINGCONFIRM':
            case 'CHỜ XÁC NHẬN':
                return 'bg-orange-50 text-orange-700 border border-orange-300 dark:bg-orange-950/40 dark:text-orange-300 dark:border-orange-800';
            case 'RESERVED':
            case 'ĐẶT TRƯỚC':
                return 'bg-purple-50 text-purple-700 border border-purple-300 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800';
            case 'PAID':
            case 'COMPLETED':
            case 'ĐÃ THANH TOÁN':
                return 'bg-green-50 text-green-700 border border-green-300 dark:bg-green-950/40 dark:text-green-300 dark:border-green-800';
            case 'CANCELLED':
            case 'ĐÃ HỦY':
            case 'APPROVEDCANCELLED':
            case 'ĐÃ DUYỆT HỦY':
                return 'bg-red-50 text-red-700 border border-red-300 dark:bg-red-950/40 dark:text-red-300 dark:border-red-800';
            case 'MERGED':
            case 'ĐÃ GỘP':
                return 'bg-purple-50 text-purple-700 border border-purple-300 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800';
            default:
                return 'bg-surface-100 text-surface-700 border border-surface-200 dark:bg-surface-800 dark:text-surface-300 dark:border-surface-700';
        }
    }

    getStatusSeverity(status: string): string {
        if (!status) return 'secondary';
        const upper = status.toUpperCase();
        switch (upper) {
            case 'SERVING': return 'primary';
            case 'PAID':
            case 'COMPLETED': return 'success';
            case 'CHECKED': return 'info';
            case 'WAITINGPAYMENT': return 'warn';
            case 'WAITINGCONFIRM': return 'warn';
            case 'CANCELLED':
            case 'APPROVEDCANCELLED': return 'danger';
            default: return 'secondary';
        }
    }
}
