import { ITableConfig } from '@/core/models/common';

export enum GroupEnum {
    GROUP_1 = 1
}
export const GROUP_LABEL: Record<GroupEnum, string> = {
    [GroupEnum.GROUP_1]: 'Nhóm 1'
};
export const groupOptionConfig = [{ label: GROUP_LABEL[GroupEnum.GROUP_1], value: GroupEnum.GROUP_1 }];

export const colsTempProductGroup: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã nhóm sản phẩm', visible: true, field: 'Code', type: 'text', minWidth: '90px' },
    { label: 'Tên nhóm sản phẩm', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Loại', visible: true, field: 'Type', type: 'transform', minWidth: '130px', transformFn: (v) => GROUP_LABEL[v as GroupEnum] },
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
                permission: 'SERVICE_GROUP_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SERVICE_GROUP_DELETE'
            }
        ]
    }
];
