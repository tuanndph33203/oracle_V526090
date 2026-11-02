import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ITableAction, ITreeTableConfig } from '@/core/models/common';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize, forkJoin } from 'rxjs';
import { ConfirmationService, MessageService, TreeNode } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';

import { cleanObject } from '@/core/utils/cleanQuery';
import { colsTempPermission } from '../../constants/permission';
import { PermissionCreate } from '../../components/permission-create/permission-create';
import { PermissionService } from '../../services/permission';
import { TreeTableMeta } from '@/shared/ui/data-display/tree-table-meta/tree-table-meta';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { FloatLabel } from 'primeng/floatlabel';
import { InputIcon } from 'primeng/inputicon';
import { IconField } from 'primeng/iconfield';

@Component({
    selector: 'app-permission',
    standalone: true,
    imports: [CommonModule, SelectModule, DatePickerModule, ButtonModule, InputTextModule, ReactiveFormsModule, TreeTableMeta, HasPermissionDirective, FloatLabel, InputIcon, IconField],
    templateUrl: './permission.html',
    styleUrl: './permission.scss'
})
export class Permission implements OnInit {
    colsTemp = signal<ITreeTableConfig[]>(colsTempPermission);
    data = signal<any[]>([]);
    dataTree = signal<any[]>([]);
    loading = signal(true);
    itemViewport = signal(15);

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });
    protected service = inject(PermissionService);
    protected formComponent = PermissionCreate;
    protected entityName = 'quyền';

    private confirmationService = inject(ConfirmationService);
    private messageService = inject(MessageService);
    private dialogService = inject(DialogService);

    constructor() {
        if (window.innerHeight < 750) {
            this.itemViewport.set(10);
        }
    }

    ngOnInit() {
        this.onSearch();
        this.formSearch
            .get('TextSearch')
            ?.valueChanges.pipe(debounceTime(400), distinctUntilChanged())
            .subscribe((value) => {
                const keyword = (value ?? '').trim().toLowerCase();
                const source = this.data();
                const filtered = !keyword ? source : source.filter(({ name, code }) => (name ?? '').toLowerCase().includes(keyword) || (code ?? '').toLowerCase().includes(keyword));
                this.dataTree.set(this.mapToTreeNodes(filtered));
            });
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
        if (action.type === 'plus') this.onAddPermission(row);
    }

    onSearch() {
        this.loading.set(true);
        this.service
            .getByFilter(cleanObject({ isTree: true }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((res: any) => {
                this.data.set(res.data);
                this.dataTree.set(this.mapToTreeNodes(res.data));
            });
    }

    onCreate() {
        this.dialogService
            .open(this.formComponent, {
                showHeader: false
            })
            ?.onClose.subscribe((ok) => ok && this.onSearch());
    }

    onEdit(row: any) {
        this.dialogService
            .open(this.formComponent, {
                showHeader: false,
                data: { edition: true, data: row }
            })
            ?.onClose.subscribe((ok) => ok && this.onSearch());
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
                    this.onSearch();
                });
            }
        });
    }

    onAddPermission(row: any) {
        const basePayload = {
            categoryPermission: row.code,
            isCategory: false
        };
        const permissions = [
            { key: 'CREATE', name: 'Thêm', description: 'thêm', isView: false },
            { key: 'EDIT', name: 'Sửa', description: 'sửa', isView: false },
            { key: 'DELETE', name: 'Xóa', description: 'xóa', isView: false }
        ];
        const requests = permissions.map((p) => {
            return this.service.create({
                ...basePayload,
                code: `${row.code}_${p.key}`,
                name: p.name,
                description: p.description,
                isView: p.isView
            });
        });
        this.messageService.add({
            severity: 'info',
            summary: 'Thông báo',
            detail: 'Đang cấp quyền thao tác'
        });
        forkJoin(requests).subscribe(() => {
            this.messageService.clear();
            this.messageService.add({
                severity: 'success',
                summary: 'Thành công',
                detail: 'Đã cấp quyền thao tác'
            });
            this.onSearch();
        });
    }

    mapToTreeNodes(data: any[]): TreeNode[] {
        return data.map((group, groupIndex) => ({
            key: `GROUP_${group.name || 'EMPTY'}`,
            expanded: true,
            data: {
                code: group.name || '(Không phân loại)',
                isCategory: true,
                rowIndex: groupIndex + 1
            },
            children: (group.items || []).map((item: any, itemIndex: number) => ({
                key: item.id,
                data: {
                    ...item,
                    isCategory: false,
                    rowIndex: `${groupIndex + 1}.${itemIndex + 1}`
                },
                children: []
            }))
        }));
    }
}
