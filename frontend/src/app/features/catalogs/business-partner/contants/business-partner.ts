import { ITableConfig } from '@/core/models/common';

export const colsTempBusinessPartner: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Mã đối tác', visible: true, field: 'Code', type: 'text', minWidth: '110px' },
    { label: 'Họ và tên / Tên công ty', visible: true, field: 'DisplayName', type: 'text', minWidth: '200px' },
    { label: 'Loại đối tác', visible: true, field: 'PartnerTypeText', type: 'text', minWidth: '130px' },
    { label: 'Nhóm đối tác', visible: true, field: 'PartnerGroupName', type: 'text', minWidth: '130px' },
    { label: 'Số điện thoại', visible: true, field: 'MobilePhone', type: 'text', minWidth: '120px' },
    { label: 'Mã số thuế', visible: true, field: 'TaxCode', type: 'text', minWidth: '120px' },
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
                permission: 'SalesCatalog.BusinessPartner.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.BusinessPartner.Delete'
            }
        ]
    }
];
