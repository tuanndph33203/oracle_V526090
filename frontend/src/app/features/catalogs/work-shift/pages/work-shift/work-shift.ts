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
import { WorkShiftService } from '../../services/work-shift';
import { BranchService } from '@/features/branch/services/branch';
import { colsTempWorkShift } from '../../contants/work-shift';
import { WorkShiftCreate } from '../../components/work-shift-create/work-shift-create';

@Component({
    selector: 'app-work-shift',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './work-shift.html',
    styleUrl: './work-shift.scss'
})
export class WorkShift extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempWorkShift);
    branchesList = signal<any[]>([]);

    protected service = inject(WorkShiftService);
    protected branchService = inject(BranchService);
    protected formComponent = WorkShiftCreate;
    protected entityName = 'Ca làm việc';

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    ngOnInit() {
        this.branchService.getByFilter({ Page: 1, Size: 100 }).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.branchesList.set(res?.Data?.Content || []);
            }
        });

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
                const branches = this.branchesList();
                const items = this.getResponseItems(res).map((x: any) => {
                    const matchedNames = x.BranchIds 
                        ? x.BranchIds.map((id: string) => branches.find(b => b.Id === id)?.Name).filter((n: any) => !!n).join(', ')
                        : '';

                    return {
                        ...x,
                        Type: x.ShiftType === 1 ? 'Ca thẳng' : 'Ca gãy',
                        BranchNames: matchedNames || 'Tất cả chi nhánh',
                        StatusText: x.Status === 1 ? 'Đang hoạt động' : (x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa')
                    };
                });
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
