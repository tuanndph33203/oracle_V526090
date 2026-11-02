import { ITableConfig } from '@/core/models/common';
import { formatNumber } from '@angular/common';

const resolveLatestPrice = (value: any) => {
    const priceItems = value?.ProductMoneyTermRelas ?? [];
    if (!priceItems.length) {
        return {
            SellingPrice: value?.SellingPrice ?? 0
        };
    }

    return priceItems.reduce((latest: any, current: any) => {
        if (!latest) return current;
        return new Date(current.StartDate) > new Date(latest.StartDate) ? current : latest;
    }, null);
};

export const colsTempProduct: ITableConfig[] = [
    {
        label: '#',
        visible: true,
        field: 'Index',
        type: 'index',
        width: '50px',
        align: 'center'
    },
    {
        label: 'Ảnh',
        visible: true,
        field: 'AvatarUrl',
        type: 'image',
        width: '90px',
        align: 'center'
    },
    {
        label: 'Mã sản phẩm',
        visible: true,
        field: 'Code',
        type: 'text',
        minWidth: '150px'
    },
    {
        label: 'Tên sản phẩm',
        visible: true,
        field: 'Name',
        type: 'text',
        minWidth: '260px',
        tooltip: true,
        lineClamp: 2
    },
    {
        label: 'ĐVT',
        visible: true,
        field: 'Units',
        type: 'transform',
        width: '80px',
        align: 'center',
        transformFn: (v: any) => v?.Name ?? ''
    },
    {
        label: 'Loại sản phẩm',
        visible: true,
        field: 'ProductType',
        type: 'transform',
        minWidth: '100px',
        transformFn: (v: any) => v?.Name ?? ''
    },
    {
        label: 'Nhóm sản phẩm',
        visible: true,
        field: 'ProductGroup',
        type: 'transform',
        minWidth: '100px',
        transformFn: (v: any) => v?.Name ?? ''
    },
    {
        label: 'Ghi chú',
        visible: true,
        field: 'Notes',
        type: 'text',
        minWidth: '100px',
        maxWidth: '150px',
        lineClamp: 1,
        tooltip: true
    },
    {
        label: 'Giá bán',
        visible: true,
        field: 'SellingPrice',
        type: 'transform',
        minWidth: '90px',
        transformRowFn: (v: any) => formatNumber(resolveLatestPrice(v)?.SellingPrice ?? 0, 'vi-VN')
    },
    {
        label: 'Thuế suất',
        visible: true,
        field: 'ImportTax',
        type: 'transform',
        width: '110px',
        align: 'center',
        transformFn: (v: any) => `${v?.VatNumber ?? 0}%`
    },
    {
        label: 'Chi nhánh áp dụng',
        visible: true,
        field: 'ApplyListBranchCodes',
        type: 'transform',
        minWidth: '180px',
        truncate: 1,
        transformFn: (v: any) => `${v?.Code ?? ''}-${v?.Name ?? ''}`
    },
    {
        label: 'Chi nhánh',
        visible: true,
        field: 'Branch',
        type: 'transform',
        minWidth: '100px',
        transformFn: (v: any) => v?.Name ?? ''
    },
    {
        label: 'Thao tác',
        visible: true,
        field: 'Action',
        type: 'action',
        width: '110px',
        minWidth: '100px',
        align: 'center',
        actionButtons: [
            {
                icon: 'pi pi-file-edit',
                tooltipLabel: 'Chỉnh sửa',
                type: 'edit',
                permission: 'SERVICE_EDIT'
            },
            {
                icon: 'pi pi-trash',
                tooltipLabel: 'Xóa',
                type: 'delete',
                permission: 'SERVICE_DELETE'
            }
        ]
    }
];
