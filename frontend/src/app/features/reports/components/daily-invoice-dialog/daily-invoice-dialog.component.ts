import {
    Component, Input, Output, EventEmitter,
    inject, OnChanges, SimpleChanges, signal
} from "@angular/core";
import { CommonModule, NgClass } from "@angular/common";
import { DialogModule } from "primeng/dialog";
import { TableModule } from "primeng/table";
import { ButtonModule } from "primeng/button";
import { ReportService } from "../../services/report.service";
import { InvoiceDetailPopupComponent } from "../invoice-detail-popup/invoice-detail-popup.component";

/**
 * DailyInvoiceDialogComponent
 * Popup hiển thị bảng kê chi tiết hóa đơn của 1 ngày cụ thể.
 * Mở bằng sự kiện double-click vào cột Ngày trên bảng báo cáo.
 */
@Component({
    selector: "app-daily-invoice-dialog",
    standalone: true,
    imports: [CommonModule, NgClass, DialogModule, TableModule, ButtonModule, InvoiceDetailPopupComponent],
    templateUrl: "./daily-invoice-dialog.component.html",
    styles: [`:host ::ng-deep .p-dialog-content { padding: 0; }`]
})
export class DailyInvoiceDialogComponent implements OnChanges {
    @Input() visible: boolean = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    /** Ngày báo cáo được truyền vào từ component cha */
    @Input() reportDate: Date | null = null;
    /** Mã chi nhánh để lọc hóa đơn */
    @Input() branchId: string | null = null;

    private reportService = inject(ReportService);

    invoices = signal<any[]>([]);
    isLoading = signal<boolean>(false);

    // Invoice Detail Popup state
    isDetailPopupVisible = signal<boolean>(false);
    selectedInvoiceNo = signal<string | null>(null);

    showInvoiceDetails(code: string): void {
        this.selectedInvoiceNo.set(code || null);
        this.isDetailPopupVisible.set(true);
    }

    /** Tổng doanh thu của tất cả hóa đơn trong popup */
    get totalRevenue(): number {
        return this.invoices().reduce((sum, inv) => sum + (inv.TotalAmount || 0), 0);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["visible"] && this.visible && this.reportDate) {
            this.loadInvoiceDetails();
        }
    }

    loadInvoiceDetails(): void {
        if (!this.reportDate) return;
        this.isLoading.set(true);
        const year = this.reportDate.getFullYear();
        const month = String(this.reportDate.getMonth() + 1).padStart(2, '0');
        const day = String(this.reportDate.getDate()).padStart(2, '0');
        const dateStr = `${year}-${month}-${day}`;
        this.reportService.getDailyInvoiceDetails(dateStr, { branchId: this.branchId }).subscribe({
            next: (res: any) => {
                this.invoices.set(res?.Data || res?.data || []);
                this.isLoading.set(false);
            },
            error: () => { this.isLoading.set(false); }
        });
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
