import { Routes } from '@angular/router';

export default [
    {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile)
    }
] as Routes;
