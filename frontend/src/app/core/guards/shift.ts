import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { firstValueFrom } from 'rxjs';
import { selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ShiftSessionService } from '@/features/catalogs/shift-session/services/shift-session';

import { selectPermissions } from '@/store/auth/auth.selectors';

export const shiftGuard: CanActivateFn = async () => {
    const store = inject(Store);
    const router = inject(Router);
    const shiftService = inject(ShiftSessionService);

    const permissions = (await firstValueFrom(store.select(selectPermissions))) || [];
    if (permissions.includes('SALEROOM_CASHIER_ADMIN')) return true;

    // Lấy branch đang chọn từ store
    const selectedBranches = await firstValueFrom(store.select(selectSelectedBranchCodes));
    const branchCode = selectedBranches?.[0]?.Code || selectedBranches?.[0];

    // Nếu chưa chọn chi nhánh thì cho qua (component sẽ tự xử lý)
    if (!branchCode) return true;

    try {
        const res = await firstValueFrom(shiftService.getCurrentShift(branchCode));

        if (res?.Code === 404 || !res?.Data || res?.Data?.Status !== 1) {
            return router.createUrlTree(['/catalogs/shift-session']);
        }

        if (res?.Data?.IsExpired) {
            return router.createUrlTree(['/catalogs/shift-session']);
        }

        return true;
    } catch {
        return router.createUrlTree(['/catalogs/shift-session']);
    }
};
