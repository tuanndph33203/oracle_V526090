import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { ITableAction, ITableConfig } from '@/core/models/common';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { AccountService } from '../../services/account';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { colsTempAccount } from '../../constants/account';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { toLocalISOString } from '@/core/utils/toLocalISOString';
import { parseDate } from '@/core/utils/parseDate';
import { Checkbox } from 'primeng/checkbox';
import { FloatLabel } from 'primeng/floatlabel';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputDatePicker } from '@/shared/ui/form/input-date-picker/input-date-picker';
@Component({
    selector: 'app-account',
    standalone: true,
    imports: [CommonModule, TableMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, ReactiveFormsModule, HasPermissionDirective, Checkbox, FloatLabel, IconField, InputIcon, InputDatePicker],
    templateUrl: './account.html',
    styleUrl: './account.scss'
})
export class Account extends BaseCrudPage<any> implements OnInit {
    colsTemp = signal<ITableConfig[]>(colsTempAccount);

    formSearch = new FormGroup({
        TextSearch: new FormControl(''),
        IsGetDeleted: new FormControl(false),
        date: new FormControl(null)
    });

    protected service = inject(AccountService);
    protected formComponent = null;
    protected entityName = 'tài khoản';

    constructor() {
        super();
        const p = this.route.snapshot.queryParams;
        if (!Object.keys(p).length) return;
        const fromDate = parseDate(p['FromDate']);
        const toDate = parseDate(p['ToDate']);
        const filters = {
            ...(p['TextSearch'] && { TextSearch: p['TextSearch'] }),
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
                    FromDate: toLocalISOString(filters.date?.[0]),
                    ToDate: toLocalISOString(filters.date?.[1])
                };
                this.onSearch(payload);
            });
    }
    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEditRow(row);
        if (action.type === 'delete') this.onDeleteRow(row);
    }

    onCreateRow() {
        this.router.navigate(['/systems/account/create']);
    }

    onEditRow(row: any) {
        this.router.navigate(['/systems/account/', row?.UserId]);
    }

    onDeleteRow(row: any) {
        this.confirmationService.confirm({
            key: 'delete-data',
            message: 'Bạn có chắc chắn muốn xóa vĩnh viễn?',
            header: 'Xác nhận',
            icon: 'pi pi-info-circle',
            acceptButtonProps: { label: 'Đồng ý', severity: 'danger' },
            rejectButtonProps: { label: 'Hủy bỏ', severity: 'secondary', outlined: true },
            accept: () => {
                this.service.delete(row.UserId).subscribe(() => {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Thành công',
                        detail: 'Bản ghi đã bị xóa'
                    });
                    this.search(this.searchQuery());
                });
            }
        });
    }
}
