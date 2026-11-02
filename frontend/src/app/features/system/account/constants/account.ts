import { ITableConfig } from '@/core/models/common';

export enum Gender {
    MALE = 1,
    FEMALE = 2
}
enum ActiveEnum {
    Active,
    Canceled
}
export const ACTIVE_LABEL: Record<ActiveEnum, string> = {
    [ActiveEnum.Active]: 'Hoạt động',
    [ActiveEnum.Canceled]: 'Hủy kích hoạt'
};

export const GENDER_LABEL: Record<Gender, string> = {
    [Gender.MALE]: 'Nam',
    [Gender.FEMALE]: 'Nữ'
};
export const colsTempAccount: ITableConfig[] = [
    {
        label: 'STT',
        visible: true,
        field: 'Index',
        type: 'index',
        width: '60px',
        align: 'center'
    },
    {
        label: 'Tên',
        visible: true,
        field: 'Name',
        type: 'text',
        minWidth: '120px'
    },
    {
        label: 'Email',
        visible: true,
        field: 'Email',
        type: 'text',
        minWidth: '200px'
    },
    {
        label: 'Số điện thoại',
        visible: true,
        field: 'Phone',
        type: 'text',
        minWidth: '140px'
    },
    {
        label: 'Mã nhân viên',
        visible: true,
        field: 'Code',
        type: 'text',
        minWidth: '120px'   
    },
    {
        label: 'Chức vụ',
        visible: true,
        field: 'Roles',
        type: 'transform',
        width: '200px',
        transformFn: (v: any[]) => v?.map((x) => x.Name).join(', ') ?? ''
    },
    {
        label: 'Chi nhánh',
        visible: true,
        field: 'Branch',
        type: 'transform',
        width: '180px',
        transformFn: (v: any) => v?.Name ?? ''
    },
    {
        label: 'Trạng thái',
        visible: true,
        field: 'Status',
        type: 'transform',
        width: '120px',
        transformFn: (v: number) => (v === 0 ? 'Hoạt động' : 'Hủy kích hoạt')
    },
    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '140px',
        align: 'center',
        actionButtons: [
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete'            }
        ]
    }
];
