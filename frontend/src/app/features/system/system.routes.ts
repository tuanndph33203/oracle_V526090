import { Routes } from '@angular/router';

export default [
    {
        path: 'role',
        title: 'Vai trò & Quyền hạn - Quán Café',
        children: [
            {
                path: '',
                loadComponent: () => import('./role/pages/role/role').then((c) => c.Role)
            },
            {
                path: 'create',
                loadComponent: () => import('./role/pages/role-create/role-create').then((c) => c.RoleCreate)
            },
            {
                path: ':id',
                loadComponent: () => import('./role/pages/role-create/role-create').then((c) => c.RoleCreate)
            }
        ]
    },
    {
        path: 'account',
        title: 'Tài khoản người dùng - Quán Café',
        children: [
            {
                path: '',
                loadComponent: () => import('./account/pages/account/account').then((c) => c.Account)
            },
            {
                path: 'create',
                loadComponent: () => import('./account/pages/account-create/account-create').then((c) => c.AccountCreate)
            },
            {
                path: ':id',
                loadComponent: () => import('./account/pages/account-create/account-create').then((c) => c.AccountCreate)
            }
        ]
    }
] as Routes;
