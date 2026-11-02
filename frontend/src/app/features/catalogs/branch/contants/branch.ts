import { ITableConfig } from '@/core/models/common';

export const colsTempBranch: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã chi nhánh', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên chi nhánh', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Địa chỉ', visible: true, field: 'Address', type: 'text', minWidth: '130px' },
    { label: 'Số điện thoại', visible: true, field: 'Phone', type: 'text', minWidth: '130px' },
    {
        label: 'Trạng thái',
        visible: true,
        field: 'Status',
        type: 'transform',
        width: '130px',
        transformFn: (v) => {
            const statusVal = Number(v);
            return statusVal === 1 ? 'Đang hoạt động' : (statusVal === 2 ? 'Ngừng sử dụng' : 'Đã xóa');
        }
    },
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
                permission: 'SalesCatalog.Branch.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.Branch.Delete'
            }
        ]
    }
];
