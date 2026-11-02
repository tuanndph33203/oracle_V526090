import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { PaginatorModule } from 'primeng/paginator';
import { MessageService } from 'primeng/api';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ReportService } from '../../services/report.service';
import { BranchService } from '@/features/branch/services/branch';
import { InvoiceDetailDialogComponent } from '../../components/invoice-detail-dialog/invoice-detail-dialog.component';
import { PageHeader } from '@/shared/ui/layout/page-header/page-header';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

@Component({
    selector: 'app-revenue-report',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TableModule,
        DatePickerModule,
        SelectModule,
        ButtonModule,
        InputTextModule,
        ToastModule,
        PaginatorModule,
        InvoiceDetailDialogComponent,
        PageHeader
    ],
    providers: [MessageService],
    templateUrl: './revenue-report.component.html',
    styleUrls: ['./revenue-report.component.css']
})
export class RevenueReportComponent implements OnInit {
    private reportService = inject(ReportService);
    private branchService = inject(BranchService);
    private messageService = inject(MessageService);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);

    // Filters
    branches = signal<any[]>([]);
    selectedBranchId = signal<string | null>(null);
    rawBranchesLength = signal<number>(0);

    reportTypes = [
        { label: 'Báo cáo doanh thu theo ngày', value: 1 },
        { label: 'Báo cáo doanh thu theo mặt hàng', value: 2 },
        { label: 'Bảng kê hóa đơn', value: 3 },
        { label: 'Báo cáo chi tiết doanh thu theo mặt hàng', value: 4 }
    ];
    selectedReportType = signal<number>(2); // Default to: Báo cáo doanh thu theo mặt hàng

    fromDate = signal<Date>(new Date(new Date().setHours(0, 0, 0, 0)));
    toDate = signal<Date>(new Date(new Date().setHours(23, 59, 59, 999)));

    items = signal<any[]>([]);
    summary = signal<any>({
        TotalRecords: 0,
        TotalQuantity: 0,
        TotalTotal: 0,
        TotalRevenue: 0,
        TotalItemAmount: 0,
        TotalDiscountAmount: 0
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

    // Dialog state
    isInvoiceDialogVisible = signal<boolean>(false);
    selectedItemId = signal<string | null>(null);
    selectedItemName = signal<string | null>(null);

    // Action button loadings
    isSyncing = signal<boolean>(false);
    isExporting = signal<boolean>(false);

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
            const formattedBranches = raw.map((x: any) => ({
                label: x.Name,
                value: x.Code
            }));
            this.branches.set([{ label: 'Tất cả chi nhánh', value: null }, ...formattedBranches]);
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
            reportType: this.selectedReportType(),
            fromDate: toLocalISOString(this.fromDate()) ?? null,
            toDate: toLocalISOString(this.toDate()) ?? null,
            pageNumber: this.pageNumber(),
            pageSize: this.pageSize()
        };

        this.reportService.getRevenueReportByItem(filter).subscribe({
            next: (res: any) => {
                const reportData = res?.Data || res?.data;
                this.items.set(reportData?.Items || reportData?.items || []);
                this.summary.set(reportData?.Summary || reportData?.summary || {
                    TotalRecords: 0,
                    TotalQuantity: 0,
                    TotalTotal: 0,
                    TotalRevenue: 0,
                    TotalItemAmount: 0,
                    TotalDiscountAmount: 0
                });
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải dữ liệu báo cáo!' });
                this.isLoading.set(false);
            }
        });
    }

    onFilterChange(): void {
        this.pageNumber.set(1);
        this.loadReportData();
    }

    onPageChange(event: any): void {
        // PrimeNG Lazy Table pagination event contains first and rows
        const first = event.first || 0;
        const rows = event.rows || 10;
        const page = Math.floor(first / rows) + 1;
        
        this.pageNumber.set(page);
        this.pageSize.set(rows);
        this.loadReportData();
    }

    onItemNameDblClick(item: any): void {
        this.selectedItemId.set(item.ItemId || item.itemId);
        this.selectedItemName.set(item.ItemName || item.itemName);
        this.isInvoiceDialogVisible.set(true);
    }

    triggerSync(): void {
        this.isSyncing.set(true);
        this.reportService.syncData().subscribe({
            next: (res: any) => {
                this.messageService.add({ 
                    severity: 'success', 
                    summary: 'Thành công', 
                    detail: res?.message || 'Đồng bộ dữ liệu từ máy bán hàng thành công!' 
                });
                this.isSyncing.set(false);
                this.loadReportData();
            },
            error: (err) => {
                console.error(err);
                this.messageService.add({ severity: 'error', summary: 'Thất bại', detail: 'Đồng bộ dữ liệu gặp lỗi!' });
                this.isSyncing.set(false);
            }
        });
    }

    exportExcel(): void {
        this.isExporting.set(true);
        const filter = {
            branchId: this.selectedBranchId(),
            reportType: this.selectedReportType(),
            fromDate: this.fromDate() ? (toLocalISOString(this.fromDate()) ?? null) : null,
            toDate: this.toDate() ? (toLocalISOString(this.toDate()) ?? null) : null
        };

        this.reportService.exportExcel(filter).subscribe({
            next: (blob: Blob) => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `BaoCaoDoanhThuMatHang_${new Date().toISOString().slice(0, 10)}.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Xuất file excel báo cáo thành công!' });
                this.isExporting.set(false);
            },
            error: (err) => {
                console.error(err);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Xuất Excel thất bại!' });
                this.isExporting.set(false);
            }
        });
    }
}
