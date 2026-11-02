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
import { ReasonService } from '../../services/reason';
import { colsTempReason } from '../../contants/reason';
import { ReasonCreate } from '../../components/reason-create/reason-create';

@Component({
    selector: 'app-reason',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './reason.html',
    styleUrl: './reason.scss'
})
export class Reason extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempReason);

    protected service = inject(ReasonService);
    protected formComponent = ReasonCreate;
    protected entityName = 'Lý do hủy';

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
