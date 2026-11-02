import { Routes } from '@angular/router';
import { Layout } from '@/core/layout/layout/layout';
import { authGuard } from '@/core/guards/auth';

export const appRoutes: Routes = [
    {
        path: '',
        component: Layout,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'catalogs/shift-session', pathMatch: 'full' },
            { path: 'catalogs', title: 'Danh mục - An FOOD', loadChildren: () => import('./app/features/catalogs/catalogs.routes') },
            { path: 'branch', title: 'Chi nhánh - An FOOD', loadComponent: () => import('./app/features/catalogs/branch/pages/branch/branch').then((c) => c.Branch) },
            { path: 'systems', title: 'Hệ thống - An FOOD', loadChildren: () => import('./app/features/system/system.routes') },
            { path: 'user', title: 'Hệ thống - An FOOD', loadChildren: () => import('./app/features/profile/profile.routes') },
            { path: 'reports', title: 'Báo cáo - An FOOD', loadChildren: () => import('./app/features/reports/reports.routes') }
        ]
    },
    { path: '', loadChildren: () => import('./app/features/auth/auth.routes') },
    { path: '', loadChildren: () => import('./app/features/error/error.routes') }
];
