import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class BaseSMSService {
    private http = inject(HttpClient);
    private baseUrl = environment.smsUrl;

    post<T>(url: string, body: any, params?: Record<string, any>, baseUrl = this.baseUrl): Observable<T> {
        const httpParams = this.buildParams(params);
        return this.http.post<T>(`${baseUrl}${url}`, body, { params: httpParams }).pipe(catchError(this.handleError));
    }

    private buildParams(params?: Record<string, any>): HttpParams {
        let httpParams = new HttpParams();
        if (!params) return httpParams;

        Object.entries(params).forEach(([key, value]) => {
            if (value !== null && value !== undefined && value !== '') {
                httpParams = httpParams.set(key, String(value));
            }
        });
        return httpParams;
    }

    private handleError(error: any) {
        console.error('HTTP Error:', error);
        return throwError(() => error);
    }
}
