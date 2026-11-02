import { ITableConfig } from '@/core/models/common';

export const colsTempProductType: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã loại sản phẩm', visible: true, field: 'Code', type: 'text', minWidth: '90px' },
    { label: 'Tên loại sản phẩm', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
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
                permission: 'SERVICE_TYPE_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SERVICE_TYPE_DELETE'
            }
        ]
    }
];
