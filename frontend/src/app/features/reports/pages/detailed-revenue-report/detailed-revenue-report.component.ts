import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Paginator } from 'primeng/paginator';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ReportService } from '../../services/report.service';
import { BranchService } from '@/features/branch/services/branch';
import { InvoiceDetailPopupComponent } from '../../components/invoice-detail-popup/invoice-detail-popup.component';
import { PageHeader } from '@/shared/ui/layout/page-header/page-header';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

@Component({
    selector: 'app-detailed-revenue-report',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        Paginator,
        DatePickerModule,
        SelectModule,
        ButtonModule,
        ToastModule,
        InvoiceDetailPopupComponent,
        PageHeader
    ],
    providers: [MessageService],
    templateUrl: './detailed-revenue-report.component.html',
    styleUrls: ['./detailed-revenue-report.component.css']
})
export class DetailedRevenueReportComponent implements OnInit {
    protected Math = Math;
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
    selectedReportType = signal<number>(4); // Detailed report

    fromDate = signal<Date>(new Date(new Date().setHours(0, 0, 0, 0)));
    toDate = signal<Date>(new Date(new Date().setHours(23, 59, 59, 999)));

    // Report data (Master-Detail)
    invoices = signal<any[]>([]);
    summary = signal<any>({
        TotalRecords: 0,
        TotalTotalAmount: 0,
        TotalPreTaxAmount: 0,
        TotalTaxAmount: 0,
        TotalDiscountPreTaxAmount: 0,
        TotalQuantity: 0
    });

    // Pagination
    pageNumber = signal<number>(1);
    pageSize = signal<number>(10);
    isLoading = signal<boolean>(false);

    // Invoice Detail Popup state
    isDetailPopupVisible = signal<boolean>(false);
    selectedInvoiceNo = signal<string | null>(null);

    // Loadings
    isSyncing = signal<boolean>(false);
    isExporting = signal<boolean>(false);

    showInvoiceDetails(code: string): void {
        this.selectedInvoiceNo.set(code || null);
        this.isDetailPopupVisible.set(true);
    }

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

        this.reportService.getDetailedRevenueReport(filter).subscribe({
            next: (res: any) => {
                const reportData = res?.Data || res?.data;
                this.invoices.set(reportData?.Invoices || reportData?.invoices || []);
                this.summary.set(reportData?.Summary || reportData?.summary || {
                    TotalRecords: 0,
                    TotalTotalAmount: 0,
                    TotalPreTaxAmount: 0,
                    TotalTaxAmount: 0,
                    TotalDiscountPreTaxAmount: 0,
                    TotalQuantity: 0
                });
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải báo cáo chi tiết!' });
                this.isLoading.set(false);
            }
        });
    }

    onFilterChange(): void {
        this.pageNumber.set(1);
        this.loadReportData();
    }

    onPageChange(event: any): void {
        // PrimeNG paginator event structure: { first: 0, rows: 10, page: 0, pageCount: 1 }
        const page = (event.page || 0) + 1;
        const rows = event.rows || 10;
        
        this.pageNumber.set(page);
        this.pageSize.set(rows);
        this.loadReportData();
    }

    triggerSync(): void {
        this.isSyncing.set(true);
        this.reportService.syncData().subscribe({
            next: (res: any) => {
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đồng bộ thành công!' });
                this.isSyncing.set(false);
                this.loadReportData();
            },
            error: (err) => {
                console.error(err);
                this.messageService.add({ severity: 'error', summary: 'Thất bại', detail: 'Lỗi đồng bộ!' });
                this.isSyncing.set(false);
            }
        });
    }

    exportExcel(): void {
        this.isExporting.set(true);
        const filter = {
            branchId: this.selectedBranchId(),
            reportType: this.selectedReportType(),
            fromDate: this.fromDate() ? toLocalISOString(this.fromDate()) : null,
            toDate: this.toDate() ? toLocalISOString(this.toDate()) : null
        };

        this.reportService.exportExcel(filter).subscribe({
            next: (blob: Blob) => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `BaoCaoChiTietDoanhThu_${new Date().toISOString().slice(0, 10)}.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Xuất excel thành công!' });
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
