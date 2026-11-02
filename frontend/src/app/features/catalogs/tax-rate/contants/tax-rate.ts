import { ITableConfig } from '@/core/models/common';

export const colsTempTaxRate: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã thuế', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên thuế', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'Thuế suất (%)', visible: true, field: 'Rate', type: 'text', minWidth: '130px' },
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
                permission: 'SalesCatalog.TaxRate.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.TaxRate.Delete'
            }
        ]
    }
];
