import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { debounceTime, distinctUntilChanged, startWith, map } from 'rxjs';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";
import { TaxRateService } from '../../services/tax-rate';
import { colsTempTaxRate } from '../../contants/tax-rate';
import { TaxRateCreate } from '../../components/tax-rate-create/tax-rate-create';

@Component({
    selector: 'app-tax-rate',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './tax-rate.html',
    styleUrl: './tax-rate.scss'
})
export class TaxRate extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempTaxRate);

    protected service = inject(TaxRateService);
    protected formComponent = TaxRateCreate;
    protected entityName = 'Thuế suất';

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    ngOnInit() {
        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
                startWith(this.formSearch.value),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
            )
            .subscribe((filters) => {
                this.onSearch(filters);
            });
    }

    override onSearch(filters: any) {
        this.loading.set(true);
        this.service.getByFilter({ ...this.searchQuery(), ...filters }).subscribe((res: any) => {
            this.loading.set(false);
            if (res?.Code === 200) {
                const items = this.getResponseItems(res).map((x: any) => ({
                    ...x,
                    StatusText: x.Status === 1 ? 'Đang hoạt động' : (x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa')
                }));
                this.data.set(items);
                this.total.set(this.getResponseTotal(res, items.length));
            }
        });
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }
}
