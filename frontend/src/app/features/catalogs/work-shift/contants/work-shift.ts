import { ITableConfig } from '@/core/models/common';

export const colsTempWorkShift: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã ca', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên ca', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Giờ bắt đầu (HH:mm)', visible: true, field: 'StartTime', type: 'text', minWidth: '130px' },
    { label: 'Giờ kết thúc (HH:mm)', visible: true, field: 'EndTime', type: 'text', minWidth: '130px' },
    { label: 'Loại ca', visible: true, field: 'Type', type: 'text', minWidth: '130px' },
    { label: 'Chi nhánh áp dụng', visible: true, field: 'BranchNames', type: 'text', minWidth: '180px' },
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
                permission: 'SalesCatalog.WorkShift.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.WorkShift.Delete'
            }
        ]
    }
];
