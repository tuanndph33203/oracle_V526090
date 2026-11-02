import { ITableConfig } from '@/core/models/common';

export const colsTempEmployee: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã NV', visible: true, field: 'Code', type: 'text', minWidth: '110px' },
    { label: 'Họ tên', visible: true, field: 'Name', type: 'text', minWidth: '150px' },
    { label: 'Số điện thoại', visible: true, field: 'Phone', type: 'text', minWidth: '120px' },
    { label: 'Email', visible: true, field: 'Email', type: 'text', minWidth: '150px' },
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
                permission: 'SalesCatalog.Employee.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.Employee.Delete'
            }
        ]
    }
];
