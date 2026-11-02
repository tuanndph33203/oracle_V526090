import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { debounceTime, distinctUntilChanged, startWith, map } from 'rxjs';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";
import { MenuGroupService } from '../../services/menu-group';
import { PreparationAreaService } from '../../../preparation-area/services/preparation-area';
import { MenuGroupCreate } from '../../components/menu-group-create/menu-group-create';

@Component({
    selector: 'app-menu-group',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './menu-group.html',
    styleUrl: './menu-group.scss'
})
export class MenuGroup extends BaseCrudPage<any> implements OnInit {

    colsTemp = computed<ITableConfig[]>(() => {
        const prepAreas = this.preparationAreas();
        return [
            { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
            { label: 'Mã nhóm', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
            { label: 'Tên nhóm', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
            {
                label: 'Ưu tiên',
                visible: true,
                field: 'Priority',
                type: 'transform',
                minWidth: '90px',
                align: 'center',
                transformFn: (v) => v !== null && v !== undefined ? v : '-'
            },
            {
                label: 'Loại nhóm',
                visible: true,
                field: 'Type',
                type: 'transform',
                minWidth: '130px',
                transformFn: (v) => {
                    const typeVal = Number(v);
                    return typeVal === 1 ? 'Món ăn' : (typeVal === 2 ? 'Đồ uống' : (typeVal === 3 ? 'Combo' : (typeVal === 4 ? 'Khác' : 'Chưa xác định')));
                }
            },
            {
                label: 'Khu vực chế biến',
                visible: true,
                field: 'PreparationAreaId',
                type: 'transform',
                minWidth: '130px',
                transformRowFn: (row) => {
                    const prepArea = prepAreas.find(p => p.Id === row.PreparationAreaId);
                    return row.PreparationAreaName || (prepArea ? prepArea.Name : (row.PreparationAreaId ? 'Không xác định' : 'Chưa thiết lập'));
                }
            },
            {
                label: 'Mô tả',
                visible: true,
                field: 'Description',
                type: 'transform',
                minWidth: '130px',
                transformFn: (v) => v?.trim() ? v : '—'
            },
            {
                label: 'Trạng thái',
                visible: true,
                field: 'Status',
                type: 'transform',
                width: '130px',
                transformFn: (v) => {
                    const statusVal = Number(v);
                    return statusVal === 1 ? 'Đang hoạt động' : (statusVal === 2 ? 'Ngừng sử dụng' : 'Đã xóa');
                }
            },
            {
                label: 'Thao tác',
                visible: true,
                field: 'Action',
                type: 'action',
                width: '140px',
                minWidth: '100px',
                actionButtons: [
                    {
                        icon: 'pi pi-file-edit',
                        tooltipLabel: 'Chỉnh sửa',
                        type: 'edit',
                        permission: 'SalesCatalog.MenuGroup.Update'
                    },
                    {
                        icon: 'pi pi-trash',
                        tooltipLabel: 'Xóa',
                        type: 'delete',
                        permission: 'SalesCatalog.MenuGroup.Delete'
                    }
                ]
            }
        ];
    });

    protected service = inject(MenuGroupService);
    private preparationAreaService = inject(PreparationAreaService);
    protected formComponent = MenuGroupCreate;
    protected entityName = 'Nhóm thực đơn';

    preparationAreas = signal<any[]>([]);

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    ngOnInit() {
        this.preparationAreaService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.preparationAreas.set(this.getResponseItems(res) || []);
            this.triggerSearch();
        });

        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
            )
            .subscribe((filters) => {
                this.onSearch(filters);
            });
    }

    triggerSearch() {
        this.onSearch(this.formSearch.value);
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }
}
