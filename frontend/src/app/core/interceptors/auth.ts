import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const token = localStorage.getItem('authToken');
    const xkey = environment.XKey;
    const modifiedReq = req.clone({
        setHeaders: {
            'x-key': xkey,
            ...(token && { Authorization: `Bearer ${token}` })
        }
    });

    return next(modifiedReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
                router.navigateByUrl('/access');
            }
            return throwError(() => error);
        })
    );
};
