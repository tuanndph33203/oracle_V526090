import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';

@Component({
    selector: 'app-order-metadata-form',
    standalone: true,
    imports: [CommonModule, FormsModule, SelectModule, InputNumberModule, InputTextModule, DatePickerModule],
    templateUrl: './order-metadata-form.html',
    styleUrl: './order-metadata-form.scss'
})
export class OrderMetadataFormComponent {
    @Input() waiters: any[] = [];
    @Input() customers: any[] = [];
    @Input() isBillEditUnlocked: boolean = false;
    @Input() billId: string | null = null;
    @Input() tableName: string = '';
    @Input() availableTables: any[] = [];
    @Input() tableId: string | null = null;
    @Output() tableIdChange = new EventEmitter<string | null>();
    @Input() showTableSelector: boolean = true;
    @Input() showServiceTypeAndTable: boolean = false;
    @Input() isDeliveryInfoCollapsed: boolean = true;
    @Input() showMetadataHeader: boolean = false;
    @Input() showInvoiceDate: boolean = true;

    // Two-way bindings
    @Input() serviceType: string = '';
    @Output() serviceTypeChange = new EventEmitter<string>();

    private _openTime: Date | null = null;
    @Input()
    get openTime(): Date | null {
        return this._openTime;
    }
    set openTime(val: Date | string | null) {
        this._openTime = this.parseOpenTime(val);
    }
    @Output() openTimeChange = new EventEmitter<Date | null>();

    onOpenTimeChange(val: any) {
        this._openTime = this.parseOpenTime(val);
        this.openTimeChange.emit(this._openTime);
    }

    private parseOpenTime(val: Date | string | null): Date | null {
        if (!val) return null;
        if (val instanceof Date) return isNaN(val.getTime()) ? null : val;
        const match = val.trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/);
        if (match) {
            const date = new Date(Number(match[3]), Number(match[2]) - 1, Number(match[1]), Number(match[4] || 0), Number(match[5] || 0));
            return date.getFullYear() === Number(match[3]) && date.getMonth() === Number(match[2]) - 1 && date.getDate() === Number(match[1]) ? date : null;
        }
        const date = new Date(val);
        return isNaN(date.getTime()) ? null : date;
    }

    @Input() waiterId: any = null;
    @Output() waiterIdChange = new EventEmitter<any>();

    @Input() customerId: any = null;
    @Output() customerIdChange = new EventEmitter<any>();

    @Input() customerCount: number = 1;
    @Output() customerCountChange = new EventEmitter<number>();

    @Input() note: string = '';
    @Output() noteChange = new EventEmitter<string>();

    @Input() phoneNumber: string = '';
    @Output() phoneNumberChange = new EventEmitter<string>();

    @Input() deliveryAddress: string = '';
    @Output() deliveryAddressChange = new EventEmitter<string>();

    @Input() deliveryNote: string = '';
    @Output() deliveryNoteChange = new EventEmitter<string>();

    @Input() shippingFeeNote: string = '';
    @Output() shippingFeeNoteChange = new EventEmitter<string>();

    @Input() therapyVoucherCode: string = '';
    @Output() therapyVoucherCodeChange = new EventEmitter<string>();
    @Output() validateTherapyVoucher = new EventEmitter<string>();
    @Input() therapyServices: any[] = [];
    @Input() selectedTherapyServiceCode: string = '';
    @Output() selectedTherapyServiceCodeChange = new EventEmitter<string>();

    // Event emitters
    @Output() customerChange = new EventEmitter<void>();
    @Output() waiterSearch = new EventEmitter<any>();
    @Output() customerSearch = new EventEmitter<any>();
    @Output() toggleDeliveryInfo = new EventEmitter<void>();

    onCustomerCountInputChange(event: any) {
        const inputVal = event?.target?.value;
        const parsed = parseInt(inputVal, 10);
        const finalVal = isNaN(parsed) || parsed < 1 ? 1 : parsed;
        if (event?.target) {
            event.target.value = finalVal;
        }
        this.customerCount = finalVal;
        this.customerCountChange.emit(finalVal);
    }

    updateCustomerCount(val: number) {
        const finalVal = Math.max(1, val);
        this.customerCount = finalVal;
        this.customerCountChange.emit(finalVal);
    }

    onSelectTable(tblId: string | null) {
        this.tableId = tblId;
        this.tableIdChange.emit(tblId);
        const tbl = this.availableTables.find(t => (t.TableId === tblId || t.Id === tblId));
        this.tableName = tbl ? (tbl.TableCode || tbl.TableName || tbl.Name || '') : '';
    }

    clearTable() {
        this.tableId = null;
        this.tableName = '';
        this.tableIdChange.emit(null);
    }

    onTherapyCodeChange(value: string) {
        this.therapyVoucherCodeChange.emit(value);
    }

    onValidateTherapyCode() {
        if (this.therapyVoucherCode?.trim()) {
            this.validateTherapyVoucher.emit(this.therapyVoucherCode.trim());
        }
    }

    clearTherapyCode() {
        this.therapyVoucherCodeChange.emit('');
        this.validateTherapyVoucher.emit('');
        this.selectedTherapyServiceCode = '';
        this.selectedTherapyServiceCodeChange.emit('');
    }

    onSelectTherapyService(serviceCode: string) {
        this.selectedTherapyServiceCode = serviceCode;
        this.selectedTherapyServiceCodeChange.emit(serviceCode);
    }

    getServiceTypeLabel(type: string): string {
        switch (type) {
            case 'DineIn': return 'Tại chỗ';
            case 'TakeAway': return 'Mang về';
            case 'Delivery': return 'Giao hàng';
            default: return type || 'Chưa chọn';
        }
    }

    getNumericShippingFee(): number | null {
        if (!this.shippingFeeNote) return null;
        const val = parseFloat(this.shippingFeeNote.toString().replace(/[^0-9.]/g, ''));
        return isNaN(val) ? null : val;
    }

    onShippingFeeChange(val: number | null) {
        const feeStr = val !== null && val !== undefined ? val.toString() : '';
        this.shippingFeeNoteChange.emit(feeStr);
    }
}
