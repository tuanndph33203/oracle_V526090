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
import { BusinessPartnerService } from '../../services/business-partner';
import { colsTempBusinessPartner } from '../../contants/business-partner';
import { BusinessPartnerCreate } from '../../components/business-partner-create/business-partner-create';

import { SelectModule } from 'primeng/select';

@Component({
    selector: 'app-business-partner',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, SelectModule, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './business-partner.html',
    styleUrl: './business-partner.scss'
})
export class BusinessPartner extends BaseCrudPage<any> implements OnInit {
    importingHo = signal(false);

    colsTemp = signal<ITableConfig[]>(colsTempBusinessPartner);

    protected service = inject(BusinessPartnerService);
    protected formComponent = BusinessPartnerCreate;
    protected entityName = 'Khách hàng/Nhà cung cấp';

    partnerTypeFilterOptions = [
        { value: null, label: 'Tất cả' },
        { value: 1, label: 'Khách hàng' },
        { value: 2, label: 'Nhà cung cấp' },
        { value: 3, label: 'Cả hai' }
    ];

    formSearch = new FormGroup({
        TextSearch: new FormControl(''),
        PartnerTypes: new FormControl<number | null>(null)
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
                            const mapped = content.map((x: any) => {
                                let displayName = x.Name;
                                if (x.CompanyName && x.Name) {
                                    displayName = `${x.Name} (${x.CompanyName})`;
                                } else if (x.CompanyName) {
                                    displayName = x.CompanyName;
                                }
                                return {
                                    ...x,
                                    DisplayName: displayName,
                                    PartnerTypeText: x.PartnerTypes === 1 ? 'Khách hàng' : (x.PartnerTypes === 2 ? 'Nhà cung cấp' : 'Cả hai'),
                                    StatusText: x.Status === 1 ? 'Đang hoạt động' : (x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa')
                                };
                            });
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

    importHoFile(event: Event) {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        input.value = '';
        if (!file) return;
        if (!/\.(xlsx|csv)$/i.test(file.name)) {
            this.messageService.add({ severity: 'warn', summary: 'Vui lòng chọn file .xlsx hoặc .csv.' });
            return;
        }
        this.importingHo.set(true);
        this.service.importHo(file).subscribe({
            next: (res: any) => {
                this.importingHo.set(false);
                if (res?.Code === 200) {
                    const result = res.Data || {};
                    this.messageService.add({ severity: 'success', summary: `Đã nhập ${result.Imported ?? 0} khách hàng; bỏ qua ${result.Skipped ?? 0} dòng.` });
                    this.onSearch(this.formSearch.getRawValue());
                } else this.messageService.add({ severity: 'error', summary: res?.Message || 'Nhập file thất bại.' });
            },
            error: (err: any) => {
                this.importingHo.set(false);
                this.messageService.add({ severity: 'error', summary: err?.error?.Message || 'Nhập file thất bại.' });
            }
        });
    }
}
