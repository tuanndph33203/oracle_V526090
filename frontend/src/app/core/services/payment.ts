import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseHttpService } from '@/core/services/base-http';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { ICreatePaymentTransactionRequest, IPaymentWebhookRequest } from '../models/payment-realtime';

@Injectable({
    providedIn: 'root'
})
export class PaymentService {
    private http = inject(BaseHttpService);

    createTransaction(data: ICreatePaymentTransactionRequest): Observable<ApiResponse<any>> {
        return this.http.post('paymenttransactions/create-transaction', data);
    }

    webhook(data: IPaymentWebhookRequest): Observable<ApiResponse<any>> {
        return this.http.post('paymenttransactions/webhook', data);
    }

    getStatus(transactionId: string): Observable<ApiResponseDetail<any>> {
        return this.http.get('paymenttransactions/status', { transactionId });
    }

    manualApprove(transactionId: string): Observable<ApiResponse<any>> {
        return this.http.put('paymenttransactions/manual-approve', {}, { transactionId });
    }

    fastCreateHistories(branchCode: string): Observable<ApiResponse<any>> {
        return this.http.get(`/paymenttransactions/fast-create-histories?BranchCode=${branchCode}`);
    }
}
