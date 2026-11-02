import { ITableConfig } from '@/core/models/common';

export const colsTempPersonnel: ITableConfig[] = [
    {
        label: 'STT',
        visible: true,
        field: 'Index',
        type: 'index',
        width: '60px',
        align: 'center'
    },
    {
        label: 'Mã nhân sự',
        visible: true,
        field: 'CustomerCode',
        type: 'text',
        minWidth: '140px',
        tooltip: true
    },
    {
        label: 'Tên nhân sự',
        visible: true,
        field: 'CustomerName',
        type: 'text',
        minWidth: '220px',
        tooltip: true,
        lineClamp: 2
    },
    {
        label: 'Giới tính',
        visible: true,
        field: 'Gender',
        type: 'transform',
        minWidth: '80px',
        width: '100px',
        align: 'center',
        transformFn: (v: number) => (v === 1 ? 'Nam' : v === 2 ? 'Nữ' : '')
    },
    {
        label: 'Ngày sinh',
        visible: true,
        field: 'DateOfBirth',
        type: 'date',
        minWidth: '120px'
    },
    {
        label: 'SĐT',
        visible: true,
        field: 'PhoneNumber',
        type: 'text',
        minWidth: '140px'
    },
    {
        label: 'Email',
        visible: true,
        field: 'Email',
        type: 'text',
        minWidth: '200px',
        tooltip: true
    },
    {
        label: 'Địa chỉ',
        visible: true,
        field: 'Address',
        type: 'text',
        minWidth: '180px',
        tooltip: true,
        lineClamp: 2
    },
    {
        label: 'CCCD',
        visible: true,
        field: 'IdentityNumber',
        type: 'text',
        minWidth: '150px'
    },
    {
        label: 'Chi nhánh',
        visible: true,
        field: 'Branch',
        type: 'transform',
        minWidth: '160px',
        transformFn: (v: any) => (v?.Code ?? '') + ' - ' + (v?.Name ?? '')
    },
    {
        label: 'Người tạo',
        visible: true,
        field: 'CreatedByUserName',
        type: 'text',
        minWidth: '150px',
        tooltip: true
    },
    {
        label: 'Ngày tạo',
        visible: true,
        field: 'CreatedOnDate',
        type: 'dateHour',
        minWidth: '150px'
    },
    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '140px',
        minWidth: '120px',
        align: 'center',
        actionButtons: [
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SYSTEM_PERSONNEL_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SYSTEM_PERSONNEL_DELETE'
            }
        ]
    }
];
