import { Routes } from '@angular/router';
import { Empty } from './pages/empty/empty';
import { Notfound } from './pages/notfound/notfound';

export default [
    { path: 'empty', component: Empty, title: 'Trang rỗng - UTC Coffee' },
    { path: 'notfound', component: Notfound, title: 'Trang không tồn tại' },
    { path: '**', redirectTo: '/notfound' }
] as Routes;
