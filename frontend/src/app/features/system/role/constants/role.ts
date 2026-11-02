import { ITableConfig } from '@/core/models/common';

export const colsTempRole: ITableConfig[] = [
    {
        label: 'STT',
        visible: true,
        field: 'Index',
        type: 'index',
        width: '60px',
        align: 'center'
    },

    {
        label: 'Mã nhóm quyền',
        visible: true,
        field: 'code',
        type: 'text',
        minWidth: '120px'
    },

    {
        label: 'Tên nhóm quyền',
        visible: true,
        field: 'name',
        type: 'text',
        minWidth: '180px',
        tooltip: true
    },

    {
        label: 'Mô tả',
        visible: true,
        field: 'description',
        type: 'text',
        minWidth: '180px'
    },

    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '140px',
        minWidth: '100px',
        align: 'center',
        actionButtons: [
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SYSTEM_ROLE_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SYSTEM_ROLE_DELETE'
            }
        ]
    }
];
