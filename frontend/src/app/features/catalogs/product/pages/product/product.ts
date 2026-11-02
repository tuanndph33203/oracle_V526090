import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { debounceTime, distinctUntilChanged, finalize, startWith } from 'rxjs';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { FloatLabel } from 'primeng/floatlabel';
import { ProductGroupService } from '../../../product-group/services/product-group';
import { ProductTypeService } from '../../../product-type/services/product-type';
import { ProductService } from '../../services/product';
import { colsTempProduct } from '../../contants/product';
import { SplitButton } from 'primeng/splitbutton';
import { toLocalISOString } from '@/core/utils/toLocalISOString';
import { Checkbox } from 'primeng/checkbox';
import { parseDate } from '@/core/utils/parseDate';
import { InputDatePicker } from "@/shared/ui/form/input-date-picker/input-date-picker";
import { ProductCreate } from '../../components/product-create/product-create';
@Component({
    selector: 'app-product',
    imports: [CommonModule, TableMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective, FloatLabel, SplitButton, Checkbox, InputDatePicker],
    templateUrl: './product.html',
    styleUrl: './product.scss'
})
export class Product extends BaseCrudPage<any> implements OnInit {
    @ViewChild('table') table!: TableMeta;
    colsTemp = signal<ITableConfig[]>(colsTempProduct);
    actionOptions = [
        {
            label: 'Xuất Excel',
            icon: 'pi pi-file-excel',
            command: () => this.exportExcel()
        }
    ];
     loadingExport = signal(false);
    protected service = inject(ProductService);
    protected formComponent = ProductCreate;
    protected entityName = 'sản phẩm';
    formSearch = new FormGroup({
        TextSearch: new FormControl(''),
        Type: new FormControl(''),
        IsGetDeleted: new FormControl(false),
        date: new FormControl(null)
    });
    productTypeOptions = signal<any[]>([]);
    productGroupOptions = signal<any[]>([]);
    protected serviceProductGroup = inject(ProductGroupService);
    protected serviceProductType = inject(ProductTypeService);
    checkedItems = signal<any[]>([]);
    constructor() {
            super();
            const p = this.route.snapshot.queryParams;
            if (!Object.keys(p).length) return;
            const fromDate = parseDate(p['FromDate']);
            const toDate = parseDate(p['ToDate']);
    
            const filters = {
                ...(p['TextSearch'] && { TextSearch: p['TextSearch'] }),
                ...(p['Type'] && { Type: p['Type'] }),
                ...(p['IsGetDeleted'] !== undefined && {
                    IsGetDeleted: p['IsGetDeleted'] === 'true'
                }),
                ...(fromDate || toDate ? { date: [fromDate ?? null, toDate ?? null] } : {})
            };
            this.formSearch.patchValue(filters);
        }

    ngOnInit() {
        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
                startWith(this.formSearch.value),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
            )
            .subscribe((filters) => {
                const payload = {
                    TextSearch: filters.TextSearch,
                    Type : Number(filters.Type),
                    IsGetDeleted: filters.IsGetDeleted ? true : false,
                    FromDate: toLocalISOString(filters.date?.[0]),
                    ToDate: toLocalISOString(filters.date?.[1])
                };
                this.onSearch(payload);
            });
        this.searchSelect(this.searchQuery(), this.serviceProductType, this.productTypeOptions);
        this.searchSelect(this.searchQuery(), this.serviceProductGroup, this.productGroupOptions);
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }

    onCheckboxChange({ selectedRows }: any) {
            if (!selectedRows?.length) return;
            this.checkedItems.set(selectedRows ?? []);
        }
        exportExcel (){
          
            const ids = (this.checkedItems() || [])
                .map((x: any) => x?.Id ?? x?.id)
                .filter((x: any) => x !== null && x !== undefined && String(x).trim() !== '')
                .map((x: any) => String(x));
    
            if (!ids.length) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Thông báo',
                    detail: 'Vui lòng chọn ít nhất 1 dịch vụ để xuất Excel'
                });
                return;
            }
    
            // API `product/export-excel` nhận body là mảng string id
            const payload = ids;
    
            this.loadingExport.set(true);
            this.service
                .exportExcel(payload)
                .pipe(
                    finalize(() => {
                        this.loadingExport.set(false);
                    })
                )
                .subscribe((response) => {
                    const blob = response.body;
                    if (!blob || blob.size === 0) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Thông báo',
                            detail: 'Không có dữ liệu để tải xuống'
                        });
                        return;
                    }
    
                    const downloadName = this.resolveDownloadFileName(response, 'service-export', 'xlsx');
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = downloadName;
                    a.style.display = 'none';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    window.setTimeout(() => window.URL.revokeObjectURL(url), 1000);
                });
        }
    
}
