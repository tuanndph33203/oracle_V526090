import { Routes } from '@angular/router';
import { shiftGuard } from '@/core/guards/shift';
import { menuVisibilityPermissionGuard } from '@/core/guards/menu-visibility-permission';

import { ProductGroup } from './product-group/pages/product-group/product-group';
import { Unit } from './unit/pages/unit/unit';

export default [
    {
        path: 'menu-visibility',
        title: 'Cấu hình hiển thị món - An FOOD',
        canActivate: [menuVisibilityPermissionGuard],
        loadComponent: () => import('./menu-visibility/menu-visibility').then(c => c.MenuVisibility)
    },
    {
        path: 'product-type',
        loadComponent: () => import('./product-type/pages/product-type/product-type').then((c) => c.ProductType)
    },
    { path: 'product-group', component: ProductGroup },
    { path: 'unit', component: Unit },
    {
        path: 'product',
        title: 'Sản phẩm - An FOOD',
        loadComponent: () => import('./product/pages/product/product').then((c) => c.Product)
    },
    {
        path: 'menu-groups',
        title: 'Nhóm thực đơn - An FOOD',
        loadComponent: () => import('./menu-group/pages/menu-group/menu-group').then((c) => c.MenuGroup)
    },
    {
        path: 'menu-items',
        title: 'Thực đơn - An FOOD',
        loadComponent: () => import('./menu-item/pages/menu-item/menu-item').then((c) => c.MenuItem)
    },
    {
        path: 'service-preferences',
        title: 'Sở thích phục vụ - An FOOD',
        loadComponent: () => import('./service-preference/pages/service-preference/service-preference').then((c) => c.ServicePreference)
    },
    {
        path: 'tax-rates',
        title: 'Thuế suất - An FOOD',
        loadComponent: () => import('./tax-rate/pages/tax-rate/tax-rate').then((c) => c.TaxRate)
    },
    {
        path: 'materials',
        title: 'Nguyên vật liệu - An FOOD',
        loadComponent: () => import('./material/pages/material/material').then((c) => c.Material)
    },
    {
        path: 'material-groups',
        title: 'Nhóm nguyên vật liệu - An FOOD',
        loadComponent: () => import('./material-group/pages/material-group/material-group').then((c) => c.MaterialGroup)
    },
    {
        path: 'warehouses',
        title: 'Nhà kho - An FOOD',
        loadComponent: () => import('./warehouse/pages/warehouse/warehouse').then((c) => c.Warehouse)
    },
    {
        path: 'work-shifts',
        title: 'Ca làm việc - An FOOD',
        loadComponent: () => import('./work-shift/pages/work-shift/work-shift').then((c) => c.WorkShift)
    },
    {
        path: 'shift-session',
        title: 'Bàn giao ca - An FOOD',
        loadComponent: () => import('./shift-session/pages/shift-session/shift-session').then((c) => c.ShiftSessionComponent)
    },
    {
        path: 'cash-drawers',
        title: 'Két đựng tiền - An FOOD',
        loadComponent: () => import('./cash-drawer/pages/cash-drawer/cash-drawer').then((c) => c.CashDrawer)
    },
    {
        path: 'preparation-areas',
        title: 'Bếp/Bar - An FOOD',
        loadComponent: () => import('./preparation-area/pages/preparation-area/preparation-area').then((c) => c.PreparationArea)
    },
    {
        path: 'reasons',
        title: 'Lý do - An FOOD',
        loadComponent: () => import('./reason/pages/reason/reason').then((c) => c.Reason)
    },
    {
        path: 'payment-types',
        title: 'Loại thanh toán - An FOOD',
        loadComponent: () => import('./payment-type/pages/payment-type/payment-type').then((c) => c.PaymentType)
    },
    {
        path: 'floor-plans',
        title: 'Sơ đồ phòng/bàn - An FOOD',
        loadComponent: () => import('./floor-plan/pages/floor-plan/floor-plan').then((c) => c.FloorPlan)
    },
    {
        path: 'dining-tables',
        title: 'Bàn ăn - An FOOD',
        canActivate: [shiftGuard],
        loadComponent: () => import('./dining-table/pages/dining-table/dining-table').then((c) => c.DiningTable)
    },
    {
        path: 'orders',
        title: 'Lịch sử đơn hàng - An FOOD',
        loadComponent: () => import('./orders/pages/orders/orders').then((c) => c.Orders)
    },
    {
        path: 'promotions',
        title: 'Chương trình ưu đãi - An FOOD',
        loadComponent: () => import('./promotions/pages/promotions/promotions').then((c) => c.Promotions)
    },
    {
        path: 'branch',
        title: 'Chi nhánh - An FOOD',
        loadComponent: () => import('./branch/pages/branch/branch').then((c) => c.Branch)
    },
    {
        path: 'employees',
        title: 'Nhân viên - An FOOD',
        loadComponent: () => import('./employee/pages/employee/employee').then((c) => c.Employee)
    },
    {
        path: 'business-partners',
        title: 'Khách hàng/Nhà cung cấp - An FOOD',
        loadComponent: () => import('./business-partner/pages/business-partner/business-partner').then((c) => c.BusinessPartner)
    },
    {
        path: 'business-partner-groups',
        title: 'Nhóm Khách hàng/Nhà cung cấp - An FOOD',
        loadComponent: () => import('./business-partner-group/pages/business-partner-group/business-partner-group').then((c) => c.BusinessPartnerGroup)
    },
    {
        path: 'kitchen',
        title: 'Nhận order Bếp/Bar - An FOOD',
        loadComponent: () => import('./kitchen/pages/kitchen/kitchen').then((c) => c.KitchenDashboard)
    }
] as Routes;
