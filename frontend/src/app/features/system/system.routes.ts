import { Routes } from '@angular/router';
import { Permission } from './permission/pages/permission/permission';
import { Navigation } from './navigation/pages/navigation/navigation';

export default [
    {
        path: 'role',
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
    { path: 'permission', component: Permission },
    {
        path: 'account',
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
    },
    { path: 'navigation', component: Navigation },
    {
        path: 'personnel',
        loadComponent: () => import('./personnel/pages/personnel/personnel').then((c) => c.Personnel)
    },
    {
        path: 'settings',
        loadComponent: () => import('./settting/pages/system-settings/system-settings').then((c) => c.SystemSettings)
    },
    {
        path: 'print-settings',
        loadComponent: () => import('./settting/pages/print-settings/print-settings').then((c) => c.PrintSettings)
    }
] as Routes;
