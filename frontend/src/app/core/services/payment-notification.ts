import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseHttpService } from '@/core/services/base-http';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';

@Injectable({
    providedIn: 'root'
})
export class PaymentNotificationService {
    private http = inject(BaseHttpService);

    getNotifications(branchCodes: string[], limit: number = 50): Observable<ApiResponseDetail<any[]>> {
        let url = 'paymentnotifications?';
        branchCodes.forEach(code => {
            url += `branchCodes=${encodeURIComponent(code)}&`;
        });
        url += `limit=${limit}`;
        return this.http.get(url);
    }

    getUnreadCount(branchCodes: string[]): Observable<ApiResponseDetail<number>> {
        let url = 'paymentnotifications/unread-count?';
        branchCodes.forEach(code => {
            url += `branchCodes=${encodeURIComponent(code)}&`;
        });
        if (url.endsWith('&')) url = url.slice(0, -1);
        return this.http.get(url);
    }

    getQrStaffCalls(branchCodes: string[], since: string, skip: number): Observable<ApiResponseDetail<any[]>> {
        const params = branchCodes.map(code => `branchCodes=${encodeURIComponent(code)}`).join('&');
        return this.http.get(`qrorder/staff-calls?${params}&since=${encodeURIComponent(since)}&skip=${skip}`);
    }

    markAsRead(id: string): Observable<ApiResponse<any>> {
        return this.http.put(`paymentnotifications/${id}/read`, {});
    }

    markAllAsRead(branchCodes: string[]): Observable<ApiResponse<any>> {
        return this.http.put('paymentnotifications/mark-all-read', branchCodes);
    }
}
