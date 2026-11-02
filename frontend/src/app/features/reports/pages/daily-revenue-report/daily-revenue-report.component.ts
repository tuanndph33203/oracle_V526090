import { Component, OnInit, inject, signal, computed, DestroyRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { TableModule } from "primeng/table";
import { DatePickerModule } from "primeng/datepicker";
import { SelectModule } from "primeng/select";
import { ButtonModule } from "primeng/button";
import { ToastModule } from "primeng/toast";
import { PaginatorModule } from "primeng/paginator";
import { MessageService } from "primeng/api";
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ReportService } from "../../services/report.service";
import { BranchService } from "@/features/branch/services/branch";
import { DailyInvoiceDialogComponent } from "../../components/daily-invoice-dialog/daily-invoice-dialog.component";
import { PageHeader } from "@/shared/ui/layout/page-header/page-header";
import { toLocalISOString } from '@/core/utils/toLocalISOString';

/**
 * DailyRevenueReportComponent
 * Màn hình báo cáo doanh thu theo ngày.
 * Sử dụng Angular 20 Standalone Components + Signals + Control Flow.
 */
@Component({
    selector: "app-daily-revenue-report",
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
        DailyInvoiceDialogComponent,
        PageHeader
    ],
    providers: [MessageService],
    templateUrl: "./daily-revenue-report.component.html",
    styleUrls: ["./daily-revenue-report.component.css"]
})
export class DailyRevenueReportComponent implements OnInit {
    private reportService = inject(ReportService);
    private branchService = inject(BranchService);
    private messageService = inject(MessageService);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);

    // ──────────────────────────────────────────────────────────
    // Filters
    // ──────────────────────────────────────────────────────────
    branches = signal<any[]>([]);
    selectedBranchId = signal<string | null>(null);
    rawBranchesLength = signal<number>(0);

    fromDate = signal<Date>(new Date(new Date().setHours(0, 0, 0, 0)));
    toDate = signal<Date>(new Date(new Date().setHours(23, 59, 59, 999)));

    // ──────────────────────────────────────────────────────────
    // Table data (Server-side pagination)
    // ──────────────────────────────────────────────────────────
    items = signal<any[]>([]);

    /** Dòng tổng toàn cục (tính trên TOÀN BỘ dữ liệu, không chỉ trang hiện tại) */
    summary = signal<any>({
        TotalRecords: 0,
        TotalPreTaxAmount: 0,
        TotalTaxAmount: 0,
        TotalDiscountAmount: 0,
        TotalTotalAmount: 0,
        TotalCashAmount: 0,
        TotalTransferAmount: 0,
        TotalVoucherAmount: 0,
        TotalActualReceipt: 0,
        TotalCustomerCount: 0
    });

    // ──────────────────────────────────────────────────────────
    // Pagination
    // ──────────────────────────────────────────────────────────
    pageNumber = signal<number>(1);
    pageSize = signal<number>(10);
    isLoading = signal<boolean>(false);

    /** Tổng số bản ghi để PrimeNG p-paginator biết tổng trang */
    get totalRecords(): number {
        return this.summary().TotalRecords ?? 0;
    }

    /** Tổng số trang (dùng trong template thay cho Math.ceil) */
    get totalPages(): number {
        const size = this.pageSize();
        return size > 0 ? Math.ceil(this.totalRecords / size) : 0;
    }

    // ──────────────────────────────────────────────────────────
    // Action button loading states
    // ──────────────────────────────────────────────────────────
    isSyncing = signal<boolean>(false);
    isExporting = signal<boolean>(false);

    // ──────────────────────────────────────────────────────────
    // Invoice Dialog (double-click on Ngày cell)
    // ──────────────────────────────────────────────────────────
    isInvoiceDialogVisible = signal<boolean>(false);
    selectedReportDate = signal<Date | null>(null);

    ngOnInit(): void {
        this.loadBranches();
    }

    // ────────────────────────────────────────────────────────────
    // Data loading
    // ────────────────────────────────────────────────────────────

    loadBranches(): void {
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            const raw = branches || [];
            this.rawBranchesLength.set(raw.length);
            const formatted = raw.map((x: any) => ({ label: x.Name, value: x.Code }));
            this.branches.set([{ label: "Tất cả chi nhánh", value: null }, ...formatted]);
        });

        this.store.pipe(
            select(selectSelectedBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            const codeStr = codes?.length ? codes.map((c: any) => c?.Code || c).filter((c: any) => !!c).join(',') : null;
            if (this.selectedBranchId() !== codeStr) {
                this.selectedBranchId.set(codeStr);
                this.onFilterChange();
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

        this.reportService.getDailyRevenue(filter).subscribe({
            next: (res: any) => {
                const data = res?.Data || res?.data;
                this.items.set(data?.Items || data?.items || []);
                this.summary.set(data?.Summary || data?.summary || this.summary());
                this.isLoading.set(false);
            },
            error: () => {
                this.messageService.add({ severity: "error", summary: "Lỗi", detail: "Không thể tải dữ liệu báo cáo!" });
                this.isLoading.set(false);
            }
        });
    }

    // ────────────────────────────────────────────────────────────
    // Event handlers
    // ────────────────────────────────────────────────────────────

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

    /** Double-click vào ô Ngày → mở popup Bảng kê hóa đơn */
    onDateDblClick(item: any): void {
        const raw = item.ReportDate || item.reportDate;
        this.selectedReportDate.set(raw ? new Date(raw) : null);
        this.isInvoiceDialogVisible.set(true);
    }

    triggerSync(): void {
        this.isSyncing.set(true);
        this.reportService.syncData().subscribe({
            next: () => {
                this.messageService.add({ severity: "success", summary: "Thành công", detail: "Đồng bộ dữ liệu thành công!" });
                this.isSyncing.set(false);
                this.loadReportData();
            },
            error: () => {
                this.messageService.add({ severity: "error", summary: "Thất bại", detail: "Đồng bộ dữ liệu gặp lỗi!" });
                this.isSyncing.set(false);
            }
        });
    }

    exportExcel(): void {
        this.isExporting.set(true);
        const filter = {
            branchId: this.selectedBranchId(),
            reportType: 1,
            fromDate: this.fromDate() ? (toLocalISOString(this.fromDate()) ?? null) : null,
            toDate: this.toDate() ? (toLocalISOString(this.toDate()) ?? null) : null
        };
        this.reportService.exportExcel(filter).subscribe({
            next: (blob: Blob) => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = `BaoCaoDoanhThuTheoNgay_${new Date().toISOString().slice(0, 10)}.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                this.messageService.add({ severity: "success", summary: "Thành công", detail: "Xuất Excel thành công!" });
                this.isExporting.set(false);
            },
            error: () => {
                this.messageService.add({ severity: "error", summary: "Lỗi", detail: "Xuất Excel thất bại!" });
                this.isExporting.set(false);
            }
        });
    }
}
