import { ITreeTableConfig, SeverityConfig } from '@/core/models/common';

export enum Status {
    INACTIVE,
    ACTIVE
}

export const navStatusSeverity: Record<Status, SeverityConfig> = {
    [Status.ACTIVE]: {
        label: 'Hiện',
        severity: 'success'
    },
    [Status.INACTIVE]: {
        label: 'Ẩn',
        severity: 'warn'
    }
};

export const colsTempNavigation: ITreeTableConfig[] = [
    {
        label: 'Tên điều hướng',
        visible: true,
        field: 'lable',
        type: 'transform',
        minWidth: '180px',
        align: 'left',
        tooltip: true,
        treeToggle: true,
        transformRowFn: (row: any) => `<i class="${row.icon}"></i> ${row.lable} `
    },
    {
        label: 'Thú tự ưu tiên',
        visible: true,
        field: 'order',
        type: 'number',
        width: '120px'
    },

    {
        label: 'Link',
        visible: true,
        field: 'routerLink',
        type: 'text',
        minWidth: '180px'
    },
    {
        label: 'Trạng thái',
        visible: true,
        field: 'status',
        type: 'severity',
        width: '100px',
        severity: navStatusSeverity
    },
    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '140px',
        minWidth: '140px',
        align: 'center',
        actionButtons: [
            {
                icon: 'pi pi-plus',
                tooltipLabel: 'Thêm điều hướng con',
                type: 'add',
                permission: 'SYSTEM_NAVIGATION_CREATE'
            },
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SYSTEM_NAVIGATION_UPDATE'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SYSTEM_NAVIGATION_DELETE'
            }
        ]
    }
];
