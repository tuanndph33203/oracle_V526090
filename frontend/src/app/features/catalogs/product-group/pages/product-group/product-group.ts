import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { ProductGroupService } from '../../services/product-group';
import { colsTempProductGroup } from '../../contants/product-group';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { ProductGroupCreate } from '../../components/product-group-create/product-group-create';
@Component({
    selector: 'app-product-group',
    imports: [CommonModule, TableMeta, PaginatorMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './product-group.html',
    styleUrl: './product-group.scss'
})
export class ProductGroup extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempProductGroup);

    protected service = inject(ProductGroupService);
    protected formComponent = ProductGroupCreate;
    protected entityName = 'nhóm sản phẩm';

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    private productGroupService = inject(ProductGroupService);

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

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }
}
