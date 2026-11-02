import { ITableConfig } from '@/core/models/common';

export const colsTempMaterial: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã nguyên vật liệu', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên nguyên vật liệu', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Nhóm NVL', visible: true, field: 'MaterialGroupCode', type: 'text', minWidth: '130px' },
    { label: 'Đơn vị tính', visible: true, field: 'UnitCode', type: 'text', minWidth: '130px' },
    { label: 'Giá mua', visible: true, field: 'PurchasePrice', type: 'text', minWidth: '130px' },
    { label: 'Giá bán', visible: true, field: 'SellingPrice', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.Material.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.Material.Delete'
            }
        ]
    }
];
