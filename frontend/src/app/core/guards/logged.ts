import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
export const loggedGuard: CanActivateChildFn = () => {
    const router = inject(Router);
    const token = localStorage.getItem('authToken');

    if (token) {
        router.navigate(['/']);
        return false;
    }

    return true;
};
