import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, signal } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ReportService } from '../../services/report.service';

/**
 * InvoiceDetailPopupComponent
 * Dialog chi tiết hóa đơn khi kích đúp vào số hóa đơn.
 */
@Component({
    selector: 'app-invoice-detail-popup',
    standalone: true,
    imports: [CommonModule, NgClass, DialogModule, TableModule, ButtonModule],
    templateUrl: './invoice-detail-popup.component.html',
    styles: [`
        :host ::ng-deep .p-dialog-content {
            padding: 0;
        }
        :host ::ng-deep .invoice-item-row > td {
            border-bottom: 1px solid #dce6e2;
            padding-top: 12px;
            padding-bottom: 12px;
        }
        :host ::ng-deep .invoice-item-name {
            color: #15253a;
            font-weight: 700;
            line-height: 1.45;
        }
        :host ::ng-deep .invoice-item-quantity {
            background: #f5f7f8;
            color: #15253a;
            font-weight: 700;
        }
        :host ::ng-deep .invoice-item-price {
            color: #15253a;
            font-weight: 700;
        }
        :host ::ng-deep .invoice-item-total {
            color: #008653;
            font-weight: 800;
        }
        :host ::ng-deep .invoice-option-row > td {
            background: #f1faf6;
            color: #075744;
            border-bottom: 0;
            line-height: 1.4;
        }
        :host ::ng-deep .invoice-option-row:last-child > td {
            border-bottom: 1px solid #dce6e2;
        }
    `]
})
export class InvoiceDetailPopupComponent implements OnChanges {
    @Input() visible: boolean = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Input() invoiceNo: string | null = null;

    private reportService = inject(ReportService);

    invoice = signal<any>(null);
    isLoading = signal<boolean>(false);
    private optionGroupCache = new WeakMap<object, { name: string; values: string[]; extraCharge: number }[]>();

    getSelectedOptionGroups(item: any): { name: string; values: string[]; extraCharge: number }[] {
        if (!item || typeof item !== 'object') return [];
        const cached = this.optionGroupCache.get(item);
        if (cached) return cached;
        const groups = new Map<string, { name: string; values: string[]; extraCharge: number }>();
        for (const option of item.SelectedOptions || []) {
            const name = String(option.GroupName || option.GroupCode || 'Tùy chọn');
            const key = String(option.GroupCode || name).toLowerCase();
            const group = groups.get(key) || { name, values: [], extraCharge: 0 };
            group.values.push(String(option.ValueName || option.ValueCode || ''));
            group.extraCharge += Number(option.ExtraCharge || 0);
            groups.set(key, group);
        }
        const result = Array.from(groups.values());
        this.optionGroupCache.set(item, result);
        return result;
    }

    getBaseUnitPrice(item: any): number {
        return Number(item?.UnitPrice || 0) - this.getSelectedOptionGroups(item)
            .reduce((sum, group) => sum + group.extraCharge, 0);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.invoiceNo) {
            this.loadInvoiceDetails();
        }
    }

    loadInvoiceDetails(): void {
        if (!this.invoiceNo) return;
        this.isLoading.set(true);
        this.reportService.getInvoiceDetails(this.invoiceNo).subscribe({
            next: (res: any) => {
                this.invoice.set(res?.Data || res?.data || null);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.isLoading.set(false);
            }
        });
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

    getShippingFee(inv: any): number {
        if (inv?.ServiceType === 'Delivery') {
            return this.parseShippingFee(inv.ShippingFeeNote);
        }
        return 0;
    }

    getInvoiceTotal(inv: any): number {
        if (!inv) return 0;
        return (inv.TotalAmount || 0) + this.getShippingFee(inv);
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
