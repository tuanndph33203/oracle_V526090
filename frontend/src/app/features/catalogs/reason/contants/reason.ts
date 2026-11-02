import { ITableConfig } from '@/core/models/common';

export const colsTempReason: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã lý do', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên lý do', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Loại lý do', visible: true, field: 'Type', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.Reason.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.Reason.Delete'
            }
        ]
    }
];
