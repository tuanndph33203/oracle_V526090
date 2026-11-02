import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { PaginatorModule } from 'primeng/paginator';
import { MessageService } from 'primeng/api';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ReportService } from '../../services/report.service';
import { BranchService } from '@/features/branch/services/branch';
import { InvoiceDetailPopupComponent } from '../../components/invoice-detail-popup/invoice-detail-popup.component';
import { PageHeader } from '@/shared/ui/layout/page-header/page-header';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

/**
 * InvoiceListReportComponent
 * Màn hình báo cáo bảng kê hóa đơn.
 * Thiết kế bằng Angular 20 Standalone Components + Signals + Control Flow.
 */
@Component({
    selector: 'app-invoice-list-report',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        DatePickerModule,
        SelectModule,
        ButtonModule,
        ToastModule,
        PaginatorModule,
        InvoiceDetailPopupComponent,
        PageHeader
    ],
    providers: [MessageService],
    templateUrl: './invoice-list-report.component.html',
    styleUrls: ['./invoice-list-report.component.css']
})
export class InvoiceListReportComponent implements OnInit {
    private reportService = inject(ReportService);
    private branchService = inject(BranchService);
    private messageService = inject(MessageService);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);

    // Filters
    branches = signal<any[]>([]);
    selectedBranchId = signal<string | null>(null);
    rawBranchesLength = signal<number>(0);
    fromDate = signal<Date>(new Date(new Date().setHours(0, 0, 0, 0)));
    toDate = signal<Date>(new Date(new Date().setHours(23, 59, 59, 999)));

    // Data lists and summary
    items = signal<any[]>([]);
    summary = signal<any>({
        TotalRecords: 0,
        TotalPreTaxAmount: 0,
        TotalTaxAmount: 0,
        TotalDiscountAmount: 0,
        TotalTotalAmount: 0,
        TotalCashAmount: 0,
        TotalTransferAmount: 0,
        TotalVoucherAmount: 0,
        TotalCustomerCount: 0
    });

    // Pagination
    pageNumber = signal<number>(1);
    pageSize = signal<number>(10);
    isLoading = signal<boolean>(false);

    get totalRecords(): number {
        return this.summary().TotalRecords ?? 0;
    }

    get totalPages(): number {
        const size = this.pageSize();
        return size > 0 ? Math.ceil(this.totalRecords / size) : 0;
    }

    // Actions loadings
    isSyncing = signal<boolean>(false);
    isExporting = signal<boolean>(false);

    // Invoice Detail Popup state
    isDetailPopupVisible = signal<boolean>(false);
    selectedInvoiceNo = signal<string | null>(null);

    ngOnInit(): void {
        this.loadBranches();
    }

    loadBranches(): void {
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            const raw = branches || [];
            this.rawBranchesLength.set(raw.length);
            const list = raw.map((x: any) => ({ label: x.Name, value: x.Code }));
            this.branches.set([{ label: "Tất cả chi nhánh", value: null }, ...list]);
        });

        this.store.pipe(
            select(selectSelectedBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            if (codes && codes.length > 0) {
                const newCode = codes[0].Code || codes[0];
                if (this.selectedBranchId() !== newCode) {
                    this.selectedBranchId.set(newCode);
                    this.onFilterChange();
                }
            }
        });
    }

    loadReportData(): void {
        this.isLoading.set(true);
        const filter = {
            branchId: this.selectedBranchId(),
            fromDate: toLocalISOString(this.fromDate()) ?? null,
            toDate: toLocalISOString(this.toDate()) ?? null,
            pageNumber: this.pageNumber(),
            pageSize: this.pageSize()
        };

        this.reportService.getInvoiceList(filter).subscribe({
            next: (res: any) => {
                const data = res?.Data || res?.data;
                this.items.set(data?.Items || data?.items || []);
                this.summary.set(data?.Summary || data?.summary || this.summary());
                this.isLoading.set(false);
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải dữ liệu bảng kê hóa đơn!' });
                this.isLoading.set(false);
            }
        });
    }

    onFilterChange(): void {
        this.pageNumber.set(1);
        this.loadReportData();
    }

    onPageChange(event: any): void {
        const first = event.first ?? 0;
        const rows = event.rows ?? 10;
        this.pageNumber.set(Math.floor(first / rows) + 1);
        this.pageSize.set(rows);
        this.loadReportData();
    }

    onInvoiceDblClick(item: any): void {
        const code = item.OrderCode || item.orderCode;
        this.selectedInvoiceNo.set(code || null);
        this.isDetailPopupVisible.set(true);
    }

    triggerSync(): void {
        this.isSyncing.set(true);
        this.reportService.syncData().subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đồng bộ dữ liệu bảng kê hóa đơn thành công!' });
                this.isSyncing.set(false);
                this.loadReportData();
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Thất bại', detail: 'Đồng bộ dữ liệu bảng kê hóa đơn gặp lỗi!' });
                this.isSyncing.set(false);
            }
        });
    }

    exportExcel(): void {
        this.isExporting.set(true);
        const filter = {
            branchId: this.selectedBranchId(),
            reportType: 3,
            fromDate: this.fromDate() ? toLocalISOString(this.fromDate()) : null,
            toDate: this.toDate() ? toLocalISOString(this.toDate()) : null
        };
        this.reportService.exportExcel(filter).subscribe({
            next: (blob: Blob) => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `BangKeHoaDon_${new Date().toISOString().slice(0, 10)}.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Xuất Excel thành công!' });
                this.isExporting.set(false);
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Xuất Excel thất bại!' });
                this.isExporting.set(false);
            }
        });
    }
}