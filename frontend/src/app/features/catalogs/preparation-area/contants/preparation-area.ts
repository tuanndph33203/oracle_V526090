import { ITableConfig } from '@/core/models/common';

export const colsTempPreparationArea: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã khu vực', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên khu vực', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Loại khu vực', visible: true, field: 'TypeText', type: 'text', minWidth: '130px' },
    { label: 'Loại thiết bị', visible: true, field: 'DeviceTypeText', type: 'text', minWidth: '150px' },
    { label: 'Thời điểm in nhãn', visible: true, field: 'LabelPrintTimingText', type: 'text', minWidth: '150px' },
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
                permission: 'SalesCatalog.PreparationArea.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.PreparationArea.Delete'
            }
        ]
    }
];
