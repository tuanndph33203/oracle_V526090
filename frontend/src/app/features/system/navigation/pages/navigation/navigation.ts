import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ITableAction, ITreeTableConfig } from '@/core/models/common';
import { ButtonModule } from 'primeng/button';
import { finalize, Subject } from 'rxjs';
import { ConfirmationService, MenuItem, MessageService, TreeNode } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';

import { cleanObject } from '@/core/utils/cleanQuery';
import { NavigationService } from '../../services/navigation';
import { NavigationCreate } from '../../components/navigation-create/navigation-create';
import { colsTempNavigation } from '../../constants/navigation';
import { TreeTableMeta } from '@/shared/ui/data-display/tree-table-meta/tree-table-meta';

@Component({
    selector: 'app-navigation',
    imports: [CommonModule, ButtonModule, TreeTableMeta],
    templateUrl: './navigation.html',
    styleUrl: './navigation.scss'
})
export class Navigation {
    colsTemp = signal<ITreeTableConfig[]>(colsTempNavigation);
    data = signal<any[]>([]);
    loading = signal(true);
    itemViewport = signal(15);

    protected service = inject(NavigationService);
    protected formComponent = NavigationCreate;
    protected entityName = 'điều hướng';

    private dialogService = inject(DialogService);
    private messageService = inject(MessageService);
    private confirmationService = inject(ConfirmationService);

    constructor() {
        if (window.innerHeight < 750) {
            this.itemViewport.set(10);
        }
    }

    ngOnInit() {
        this.onSearch();
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'add') this.onAddChild(row);
        if (action.type === 'delete') this.onDelete(row);
    }

    onSearch() {
        this.loading.set(true);
        this.service
            .getByFilter(cleanObject({ isTree: true, isAll: true }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((res: any) => {
                const data = this.mapToTreeNodes(this.getResponseItems(res));

                this.data.set(data);
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

    onAddChild(row: any) {
        this.dialogService
            .open(this.formComponent, {
                showHeader: false,
                data: { data: { parentId: row } }
            })
            ?.onClose.subscribe((ok) => ok && this.onSearch());
    }
    mapToTreeNodes(items: any[]): TreeNode[] {
        return items.map((item) => ({
            key: item.id,
            expanded: true,
            data: item,
            children: Array.isArray(item.items) && item.items.length ? this.mapToTreeNodes(item.items) : []
        }));
    }

    private getResponseItems(res: any): any[] {
        const candidates = [res?.Data?.Content, res?.data?.content, res?.Data?.Items, res?.data?.items, res?.Data, res?.data, res];

        for (const candidate of candidates) {
            if (Array.isArray(candidate)) {
                return candidate;
            }
        }

        return [];
    }
}
