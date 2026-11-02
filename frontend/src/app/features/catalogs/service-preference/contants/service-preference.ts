import { ITableConfig } from '@/core/models/common';

export const colsTempServicePreference: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã sở thích', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên sở thích', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Phí phục vụ thêm', visible: true, field: 'ExtraCharge', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.ServicePreference.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.ServicePreference.Delete'
            }
        ]
    }
];
