import { ITableConfig } from '@/core/models/common';

export const colsTempWarehouse: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã kho', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên kho', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Kho cha', visible: true, field: 'ParentId', type: 'text', minWidth: '130px' },
    { label: 'Địa chỉ', visible: true, field: 'Address', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.Warehouse.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.Warehouse.Delete'
            }
        ]
    }
];
