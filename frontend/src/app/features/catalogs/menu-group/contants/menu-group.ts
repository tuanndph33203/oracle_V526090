import { ITableConfig } from '@/core/models/common';

export const colsTempMenuGroup: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã nhóm', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên nhóm', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Loại nhóm', visible: true, field: 'TypeText', type: 'text', minWidth: '130px' },
    { label: 'Khu vực chế biến', visible: true, field: 'PreparationAreaName', type: 'text', minWidth: '130px' },
    { label: 'Mô tả', visible: true, field: 'Description', type: 'text', minWidth: '130px' },
    { label: 'Trạng thái', visible: true, field: 'StatusText', type: 'text', width: '130px' },
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
