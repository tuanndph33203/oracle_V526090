import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { ITableAction, ITableConfig } from '@/core/models/common';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize, Subject, switchMap } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { cleanObject } from '@/core/utils/cleanQuery';
import { colsTempRole } from '../../constants/role';
import { RoleService } from '../../services/role';
import { Router } from '@angular/router';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";

@Component({
    selector: 'app-role',
    imports: [CommonModule, TableMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './role.html',
    styleUrl: './role.scss'
})
export class Role {
    colsTemp = signal<ITableConfig[]>(colsTempRole);
    data = signal<any[]>([]);
    loading = signal(true);
    itemViewport = signal(15);

    searchQuery = signal({
        keyword: ''
    });

    private search$ = new Subject<any>();

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    protected service = inject(RoleService);
    protected formComponent = null;
    protected entityName = 'nhóm quyền';

    private confirmationService = inject(ConfirmationService);
    private messageService = inject(MessageService);
    private router = inject(Router);

    constructor() {
        if (window.innerHeight < 750) {
            this.itemViewport.set(10);
            this.searchQuery.update((prev) => ({
                ...prev,
                size: 10
            }));
        }

        this.search$
            .pipe(
                debounceTime(500),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
                switchMap((query) => this.service.getByFilter(cleanObject(query)).pipe(finalize(() => this.loading.set(false))))
            )
            .subscribe((res: any) => {
                this.data.set(res.data);
            });

        this.search$.next(this.searchQuery());
    }

    ngOnInit() {
        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
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

    onSearch(filters: any) {
        this.searchQuery.update((q) => ({
            ...q,
            page: 1,
            TextSearch: filters
        }));
        this.loading.set(true);
        this.search$.next(this.searchQuery());
    }
    search(query: any) {
        this.loading.set(true);
        this.service
            .getByFilter(cleanObject(query))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((res: any) => {
                this.data.set(res.data);
            });
    }
    onCreate() {
        this.router.navigate(['/systems/role/create']);
    }

    onEdit(row: any) {
        console.log(row);
        
        this.router.navigate(['/systems/role/', row.id]);
    }

    onDelete(row: any) {
        this.confirmationService.confirm({
            key: 'delete-data',
            message: 'Bạn có chắc chắn muốn xóa vĩnh viễn?',
            header: 'Xác nhận',
            icon: 'pi pi-info-circle',
            acceptButtonProps: { label: 'Đồng ý', severity: 'danger' },
            rejectButtonProps: { label: 'Hủy bỏ', severity: 'secondary', outlined: true },
            accept: () => {
                this.service.delete(row.id).subscribe(() => {
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
