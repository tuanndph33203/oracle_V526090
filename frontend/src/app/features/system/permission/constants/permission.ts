import { ITreeTableConfig } from '@/core/models/common';

export const colsTempPermission: ITreeTableConfig[] = [
        {
        label: 'STT',
        visible: true,
        field: 'rowIndex',
        type: 'index',
        width: '50px',
        align: 'right'
    },
    {
        label: 'Mã quyền',
        visible: true,
        field: 'code',
        type: 'text',
        minWidth: '120px',
        treeToggle: true
    },
    {
        label: 'Tên quyền',
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
                icon: 'pi pi-plus',
                tooltipLabel: 'Cấp quyền thao tác',
                type: 'plus',
                visible: (v: any) => !v.id,
                permission: 'SYSTEM_PERMISSION_CREATE'
            },
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SYSTEM_PERMISSION_UPDATE',
                visible: (v: any) => !!v.id
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SYSTEM_PERMISSION_DELETE',
                visible: (v: any) => !!v.id
            }
        ]
    }
];
