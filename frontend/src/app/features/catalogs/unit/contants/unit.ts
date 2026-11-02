import { ITableConfig } from '@/core/models/common';

export const colsTempUnit: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã đơn vị tính', visible: true, field: 'Code', type: 'text', width: '170px' },
    { label: 'Tên đơn vị tính', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
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
                permission: 'SERVICE_UNIT_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SERVICE_UNIT_DELETE'
            }
        ]
    }
];
