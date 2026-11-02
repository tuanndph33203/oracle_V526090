import { IAuthState } from '@/core/models/auth';
import { createFeatureSelector, createSelector } from '@ngrx/store';

export const selectAuthState = createFeatureSelector<IAuthState>('auth');
export const selectUser = createSelector(selectAuthState, (s) => s.user);

function normalizePermissionCode(permission: any): string | null {
    if (!permission) return null;
    if (typeof permission === 'string') return permission;

    return permission.code || permission.Code || permission.name || permission.Name || permission.permission || permission.Permission || null;
}

function collectPermissionCodes(source: any, bucket: Set<string>) {
    if (!source) return;

    if (Array.isArray(source)) {
        source.forEach((item) => collectPermissionCodes(item, bucket));
        return;
    }

    const normalized = normalizePermissionCode(source);
    if (normalized) {
        bucket.add(normalized);
    }

    const nestedKeys = ['permissions', 'Permissions', 'listPermission', 'listPermissions', 'permissionIds', 'roles', 'role', 'items'];
    nestedKeys.forEach((key) => {
        if (source?.[key]) {
            collectPermissionCodes(source[key], bucket);
        }
    });
}

export const selectPermissions = createSelector(selectAuthState, (s) => {
    const permissionSet = new Set<string>();
    collectPermissionCodes(s.user, permissionSet);
    return Array.from(permissionSet);
});

export const selectEmployee = createSelector(selectAuthState, (s) => s?.employee);

export const selectAccessToken = createSelector(selectAuthState, (s) => s.accessToken);

export const selectIsLoggedIn = createSelector(selectAccessToken, (token) => !!token);
