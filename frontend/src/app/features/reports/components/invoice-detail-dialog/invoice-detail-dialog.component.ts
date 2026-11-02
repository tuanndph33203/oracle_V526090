import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ReportService } from '../../services/report.service';
import { InvoiceDetailPopupComponent } from '../invoice-detail-popup/invoice-detail-popup.component';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

@Component({
    selector: 'app-invoice-detail-dialog',
    standalone: true,
    imports: [CommonModule, DialogModule, TableModule, ButtonModule, InvoiceDetailPopupComponent],
    templateUrl: './invoice-detail-dialog.component.html',
    styles: [`
        :host ::ng-deep .p-dialog-content {
            padding: 0;
        }
    `]
})
export class InvoiceDetailDialogComponent implements OnChanges {
    @Input() visible: boolean = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    @Input() itemId: string | null = null;
    @Input() itemName: string | null = null;
    @Input() branchId: string | null = null;
    @Input() fromDate: Date | null = null;
    @Input() toDate: Date | null = null;

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

    ngOnChanges(changes: SimpleChanges): void {
        if (this.visible && this.itemId) {
            if (changes['visible'] || changes['itemId'] || changes['fromDate'] || changes['toDate'] || changes['branchId']) {
                this.loadInvoiceDetails();
            }
        }
    }

    loadInvoiceDetails(): void {
        if (!this.itemId) return;

        this.isLoading.set(true);
        const query = {
            branchId: this.branchId,
            fromDate: this.fromDate ? (toLocalISOString(this.fromDate) ?? null) : null,
            toDate: this.toDate ? (toLocalISOString(this.toDate) ?? null) : null
        };

        this.reportService.getItemInvoiceDetails(this.itemId, query).subscribe({
            next: (res: any) => {
                this.invoices.set(res?.Data || res?.data || []);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.isLoading.set(false);
            }
        });
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
