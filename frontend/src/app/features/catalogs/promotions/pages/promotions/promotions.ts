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
import { PromotionService } from '../../services/promotion';
import { colsTempPromotion } from '../../contants/promotion';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";
import { PromotionCreate } from '../../components/promotion-create/promotion-create';

@Component({
    selector: 'app-promotions',
    imports: [CommonModule, TableMeta, PaginatorMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './promotions.html',
    styleUrl: './promotions.scss'
})
export class Promotions extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempPromotion);

    protected service = inject(PromotionService);
    protected formComponent = PromotionCreate;
    protected entityName = 'chương trình ưu đãi';

    formSearch = new FormGroup({
        TextSearch: new FormControl(''),
        PromotionType: new FormControl<number | null>(null)
    });

    promotionTypes = [
        { label: 'Tất cả hình thức', value: null },
        { label: 'Giảm giá', value: 1 },
        { label: 'Tặng món', value: 2 }
    ];

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
