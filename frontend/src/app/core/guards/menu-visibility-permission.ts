import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { firstValueFrom, filter, take, timeout } from 'rxjs';
import { selectPermissions, selectUser } from '@/store/auth/auth.selectors';

const permission = 'SALESCATALOG_MENU_VISIBILITY_VIEW';

function isAdminRole(value: any): boolean {
    if (typeof value === 'string') return value.toLowerCase() === 'admin';
    if (Array.isArray(value)) return value.some(isAdminRole);
    return !!value && [value.name, value.Name, value.code, value.Code, value.role, value.Role].some(isAdminRole);
}

export const menuVisibilityPermissionGuard: CanActivateFn = async () => {
    const store = inject(Store);
    const router = inject(Router);
    try {
        const user = await firstValueFrom(store.select(selectUser).pipe(filter(Boolean), take(1), timeout(10000)));
        const permissions = await firstValueFrom(store.select(selectPermissions).pipe(take(1)));
        const allowed = isAdminRole(user.roles) || permissions.some(code => code.replace(/\./g, '_').toUpperCase() === permission);
        return allowed ? true : router.createUrlTree(['/access']);
    } catch {
        return router.createUrlTree(['/access']);
    }
};
