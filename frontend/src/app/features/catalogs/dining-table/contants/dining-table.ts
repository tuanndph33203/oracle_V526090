import { ITableConfig } from '@/core/models/common';

export const colsTempDiningTable: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã bàn', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên bàn', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Sơ đồ phòng', visible: true, field: 'FloorPlanId', type: 'text', minWidth: '130px' },
    { label: 'Chi nhánh', visible: true, field: 'BranchId', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.DiningTable.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.DiningTable.Delete'
            }
        ]
    }
];
