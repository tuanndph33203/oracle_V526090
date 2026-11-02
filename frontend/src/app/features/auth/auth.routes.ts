import { loggedGuard } from '@/core/guards/logged';
import { Routes } from '@angular/router';

export default [
    {
        path: 'access',
        title: 'Lỗi xác thực - An FOOD',
        loadComponent: () => import('./pages/access/access').then((m) => m.Access)
    },
    {
        path: 'login',
        canActivate: [loggedGuard],
        title: 'Đăng nhập - An FOOD',
        loadComponent: () => import('./pages/login/login').then((m) => m.Login)
    },
    {
        path: 'error',
        title: 'Lỗi - An FOOD',
        loadComponent: () => import('./pages/error/error').then((m) => m.Error)
    },
    {
        path: 'login-callback',
        loadComponent: () => import('./pages/login-callback/login-callback').then((m) => m.LoginCallback)
    }
] as Routes;
