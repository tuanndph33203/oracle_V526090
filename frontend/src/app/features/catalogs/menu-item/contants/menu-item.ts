import { ITableConfig } from '@/core/models/common';

export const colsTempMenuItem: ITableConfig[] = [
    { label: 'STT', visible: true, field: 'Index', type: 'index', width: '60px', align: 'center' },
    { label: 'Ảnh', visible: true, field: 'ImageUrl', type: 'image', width: '90px', align: 'center' },
    { label: 'Mã thực đơn', visible: true, field: 'Code', type: 'text', minWidth: '130px' },
    { label: 'Tên món', visible: true, field: 'Name', type: 'text', minWidth: '130px' },
    { label: 'STT hiển thị', visible: true, field: 'CourseOrder', type: 'text', width: '105px', align: 'center' },
    { label: 'Nhóm thực đơn', visible: true, field: 'MenuGroupDisplay', type: 'text', minWidth: '130px' },
    { label: 'Chi nhánh áp dụng', visible: true, field: 'BranchDisplay', type: 'text', minWidth: '150px' },
    { label: 'Giá bán', visible: true, field: 'SalePrice', type: 'text', minWidth: '130px' },
    { label: 'Thành tiền', visible: true, field: 'TotalPriceDisplay', type: 'text', minWidth: '130px' },
    { label: 'Giá mua', visible: true, field: 'CostPrice', type: 'text', minWidth: '130px' },
    { label: 'Đơn vị tính', visible: true, field: 'UnitDisplay', type: 'text', minWidth: '130px' },
    { label: 'Thuế suất', visible: true, field: 'TaxRateDisplay', type: 'text', minWidth: '130px' },
    { label: 'Mô tả', visible: true, field: 'Description', type: 'text', minWidth: '130px' },
    { label: 'Trạng thái', visible: true, field: 'StatusText', type: 'text', width: '130px' },
    { label: 'Hiển thị (Còn món)', visible: true, field: 'IsAvailableDisplay', type: 'text', minWidth: '150px' },
    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '160px',
        minWidth: '120px',
        actionButtons: [
            {
                icon: (row: any) => (row?.IsAvailable === false ? 'pi pi-eye-slash' : 'pi pi-eye'),
                tooltipLabel: (row: any) => (row?.IsAvailable === false ? 'Bật hiển thị món' : 'Tắt hiển thị món (Báo hết)'),
                type: 'toggleAvailable',
                permission: 'SOLD_OUT'
            },
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SalesCatalog.MenuItem.Update'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SalesCatalog.MenuItem.Delete'
            }
        ]
    }
];
