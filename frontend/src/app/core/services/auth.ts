import { inject, Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Observable, of, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Store } from '@ngrx/store';
import AuthAction from '@/store/auth/auth.actions';
import { MessageService } from 'primeng/api';
import { MenuService } from './menu';

@Injectable({
    providedIn: 'root'
})
export class Auth {
    private authStore = inject(Store);
    private jwtHelper = inject(JwtHelperService);
    private httpClient = inject(HttpClient);
    private message = inject(MessageService);
    private menu = inject(MenuService);

    private authUrl = environment.authencationUrl;
    private authKey = environment.microsoftClientKey === '232FB9D1-D0B8-413C-871C-45C280BBF94F' ? 'C222016D-F533-4BEF-99AD-FBB08BE01F76' : environment.microsoftClientKey;

    login(email: string, password: string): Observable<any> {
        return this.httpClient.post(`${this.authUrl}authentication/login`, { email, password, applicationId: environment.applicationId }).pipe(
            tap((res: any) => {
                if (res.code !== 200) {
                    console.error('Auth Error:', res);
                    this.message.add({ severity: 'error', summary: 'Lỗi xác thực', detail: res.message || 'Đã xảy ra lỗi trong quá trình xác thực.' });
                }
            })
        );
    }

    loginBySectionId(sessionId: string): Observable<any> {
        return this.httpClient.get(`${this.authUrl}authentication/issue-token?sectionId=${sessionId}`);
    }
    getUserProfile(): Observable<any> {
        return this.httpClient.get(`${this.authUrl}account/get-info`, {});
    }

    redirectMicrosoft(): Observable<any> {
        window.location.href = `${this.authUrl}authentication/login-microsoft?clientKey=${this.authKey}`;
        return of(true);
    }

    isAuthenticated(): boolean {
        const token = localStorage.getItem('authToken');
        if (!token) return false;
        const isValidFormat = token.split('.').length === 3;
        if (!isValidFormat) {
            this.authStore.dispatch(AuthAction.logout());
            return false;
        }

        try {
            if (this.jwtHelper.isTokenExpired(token)) {
                this.authStore.dispatch(AuthAction.logout());
                return false;
            }
        } catch (error) {
            this.authStore.dispatch(AuthAction.logout());
            return false;
        }

        return true;
    }

    refreshToken(): Observable<any> {
        return this.httpClient.get(`${this.authUrl}authentication/refreshToken`);
    }

    logout(): Observable<any> {
        this.menu.clear();
        return of(true);
    }
}
