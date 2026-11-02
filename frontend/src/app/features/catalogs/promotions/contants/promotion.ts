import { ITableConfig } from '@/core/models/common';

export const colsTempPromotion: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã chương trình', visible: true, field: 'Code', type: 'text', width: '150px' },
    { label: 'Tên chương trình', visible: true, field: 'Name', type: 'text', minWidth: '180px' },
    { 
        label: 'Hình thức ưu đãi', 
        visible: true, 
        field: 'PromotionType', 
        type: 'transform', 
        width: '150px',
        transformFn: (val: number) => val === 1 ? 'Giảm giá' : 'Tặng món'
    },
    { 
        label: 'Thời gian bắt đầu', 
        visible: true, 
        field: 'StartDate', 
        type: 'date', 
        width: '150px' 
    },
    { 
        label: 'Thời gian kết thúc', 
        visible: true, 
        field: 'EndDate', 
        type: 'date', 
        width: '150px' 
    },
    { 
        label: 'Chi nhánh áp dụng', 
        visible: true, 
        field: 'BranchNames', 
        type: 'text', 
        minWidth: '200px' 
    },
    {
        label: 'Trạng thái',
        visible: true,
        field: 'IsActive',
        type: 'severity',
        width: '120px',
        align: 'center',
        severity: {
            1: { label: 'Hoạt động', severity: 'success' },
            0: { label: 'Ngừng hoạt động', severity: 'danger' },
            'true': { label: 'Hoạt động', severity: 'success' },
            'false': { label: 'Ngừng hoạt động', severity: 'danger' }
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
                permission: 'PROMOTION_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'PROMOTION_DELETE'
            }
        ]
    }
];
