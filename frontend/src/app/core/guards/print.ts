import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
export const printGuard: CanActivateFn = () => {
    const printService: any = inject(Router);
    const router = inject(Router);

    if (!printService?.isPrinting) {
        router.navigate([{ outlets: { print: null } }]);
        return false;
    }
    return true;
};
