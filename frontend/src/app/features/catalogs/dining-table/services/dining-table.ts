import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class DiningTableService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('diningtable/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('diningtable', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('diningtable', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('diningtable?id=' + id);
    }

    saveLayout(data: any): Observable<any> {
        return this.http.post('diningtable/save-layout', data);
    }

    // Billing & Ordering Stub methods
    openTable(tableId: string, branchCode: string): Observable<any> {
        return this.http.post(`diningtable/open-table?tableId=${tableId}&branchCode=${branchCode}`, {});
    }

    createOrder(tableId: string, branchCode: string): Observable<any> {
        return this.http.post(`diningtable/create-order?tableId=${tableId}&branchCode=${branchCode}`, {});
    }

    addOrderItem(orderId: string, item: any): Observable<any> {
        return this.http.post(`diningtable/add-item?orderId=${orderId}`, item);
    }

    updateOrderItem(orderId: string, item: any): Observable<any> {
        return this.http.put(`diningtable/update-item?orderId=${orderId}`, item);
    }

    removeOrderItem(orderId: string, itemId: string): Observable<any> {
        return this.http.delete(`diningtable/remove-item?orderId=${orderId}&itemId=${itemId}`);
    }

    updateBill(tableId: string, branchCode: string): Observable<any> {
        return this.http.put(`diningtable/update-bill?tableId=${tableId}&branchCode=${branchCode}`, {});
    }

    getCurrentBill(tableId: string, branchCode: string): Observable<any> {
        return this.http.get(`diningtable/current-bill?tableId=${tableId}&branchCode=${branchCode}`);
    }

    closeBill(tableId: string, branchCode: string): Observable<any> {
        return this.http.post(`diningtable/close-bill?tableId=${tableId}&branchCode=${branchCode}`, {});
    }
}
