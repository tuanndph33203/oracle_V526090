import { Routes } from '@angular/router';

export default [
    {
        path: 'branches',
        title: 'Quản lý Chi nhánh - Quán Café',
        loadComponent: () => import('./branch/pages/branch/branch').then((c) => c.Branch)
    },
    {
        path: 'dining-tables',
        title: 'Bàn ăn & Bán hàng - Quán Café',
        loadComponent: () => import('./dining-table/pages/dining-table/dining-table').then((c) => c.DiningTable)
    },
    {
        path: 'floor-plans',
        title: 'Khu vực & Sơ đồ bàn - Quán Café',
        loadComponent: () => import('./floor-plan/pages/floor-plan/floor-plan').then((c) => c.FloorPlan)
    },
    {
        path: 'menu-groups',
        title: 'Nhóm đồ uống - Quán Café',
        loadComponent: () => import('./menu-group/pages/menu-group/menu-group').then((c) => c.MenuGroup)
    },
    {
        path: 'menu-items',
        title: 'Thực đơn đồ uống - Quán Café',
        loadComponent: () => import('./menu-item/pages/menu-item/menu-item').then((c) => c.MenuItem)
    },
    {
        path: 'orders',
        title: 'Lịch sử hóa đơn - Quán Café',
        loadComponent: () => import('./orders/pages/orders/orders').then((c) => c.Orders)
    },
    {
        path: 'employees',
        title: 'Quản lý nhân viên - Quán Café',
        loadComponent: () => import('./employee/pages/employee/employee').then((c) => c.Employee)
    },
    {
        path: 'business-partners',
        title: 'Quản lý khách hàng - Quán Café',
        loadComponent: () => import('./business-partner/pages/business-partner/business-partner').then((c) => c.BusinessPartner)
    },
    {
        path: 'materials',
        title: 'Nguyên vật liệu pha chế - Quán Café',
        loadComponent: () => import('./material/pages/material/material').then((c) => c.Material)
    },
    {
        path: 'payment-types',
        title: 'Hình thức thanh toán - Quán Café',
        loadComponent: () => import('./payment-type/pages/payment-type/payment-type').then((c) => c.PaymentType)
    }
] as Routes;
