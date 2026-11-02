import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { debounceTime, distinctUntilChanged, map, startWith } from 'rxjs';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";
import { BusinessPartnerGroupService } from '../../services/business-partner-group';
import { colsTempBusinessPartnerGroup } from '../../contants/business-partner-group';
import { BusinessPartnerGroupCreate } from '../../components/business-partner-group-create/business-partner-group-create';

@Component({
    selector: 'app-business-partner-group',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './business-partner-group.html',
    styleUrl: './business-partner-group.scss'
})
export class BusinessPartnerGroup extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempBusinessPartnerGroup);

    protected service = inject(BusinessPartnerGroupService);
    protected formComponent = BusinessPartnerGroupCreate;
    protected entityName = 'Nhóm Khách hàng/Nhà cung cấp';

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    constructor() {
        super();
        const originalGetByFilter = this.service.getByFilter.bind(this.service);
        (this.service as any).getByFilter = (query: any) => {
            return originalGetByFilter(query).pipe(
                map((res: any) => {
                    if (res?.Code === 200) {
                        const content = res.Data?.Content || res.Data?.Items || res.Data;
                        if (Array.isArray(content)) {
                            const mapped = content.map((x: any) => ({
                                ...x,
                                GroupTypeText: x.GroupTypes === 1 ? 'Nhóm khách hàng' : (x.GroupTypes === 2 ? 'Nhóm nhà cung cấp' : 'Cả hai'),
                                StatusText: x.Status === 1 ? 'Đang hoạt động' : (x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa')
                            }));
                            if (res.Data?.Content) res.Data.Content = mapped;
                            else if (res.Data?.Items) res.Data.Items = mapped;
                            else res.Data = mapped;
                        }
                    }
                    return res;
                })
            );
        };
    }

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
