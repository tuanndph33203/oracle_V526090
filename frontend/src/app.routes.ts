import { Routes } from '@angular/router';
import { Layout } from '@/core/layout/layout/layout';
import { authGuard } from '@/core/guards/auth';

export const appRoutes: Routes = [
    {
        path: '',
        component: Layout,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'catalogs/dining-tables', pathMatch: 'full' },
            { path: 'catalogs', title: 'Danh mục - Quán Café', loadChildren: () => import('./app/features/catalogs/catalogs.routes') },
            { path: 'systems', title: 'Hệ thống - Quán Café', loadChildren: () => import('./app/features/system/system.routes') },
            { path: 'user', title: 'Hồ sơ - Quán Café', loadChildren: () => import('./app/features/profile/profile.routes') },
            { path: 'reports', title: 'Báo cáo - Quán Café', loadChildren: () => import('./app/features/reports/reports.routes') }
        ]
    },
    { path: '', loadChildren: () => import('./app/features/auth/auth.routes') },
    { path: '', loadChildren: () => import('./app/features/error/error.routes') }
];
