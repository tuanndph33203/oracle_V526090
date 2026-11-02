import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class SalesService {
    private http = inject(BaseHttpService);

    // ── Table Map & Active Orders ────────────────────────
    getTableMap(branchCode: string, floorId: string, status: string): Observable<any> {
        return this.http.get(`sales/table-map?branchCode=${branchCode}&floorId=${floorId}&status=${status}&_t=${Date.now()}`);
    }

    getActiveOrdersByTable(tableId: string, branchCode: string): Observable<any> {
        return this.http.get(`sales/tables/${tableId}/active-orders?branchCode=${branchCode}&_t=${Date.now()}`);
    }

    getOrderById(orderId: string, branchCode: string): Observable<any> {
        return this.http.get(`sales/orders/${orderId}?branchCode=${branchCode}&_t=${Date.now()}`);
    }

    getAvailablePromotions(orderId: string, customerId?: string | null): Observable<any> {
        const customerQuery = customerId ? `&customerId=${encodeURIComponent(customerId)}` : '';
        return this.http.get(`sales/orders/${orderId}/available-promotions?overrideCustomer=true${customerQuery}&_t=${Date.now()}`);
    }

    getOrdersByServiceType(branchCode: string, serviceType: string, statuses?: string): Observable<any> {
        return this.http.get(`sales/orders/by-service-type?branchCode=${branchCode}&serviceType=${serviceType}&statuses=${statuses || ''}&_t=${Date.now()}`);
    }

    validateTherapyVoucher(code: string): Observable<any> {
        return this.http.get(`sales/validate-therapy-voucher?code=${encodeURIComponent(code)}`);
    }

    // ── Create & Update Order ─────────────────────────────
    openDineInTable(tableIds: string[], branchCode: string, customerCount: number = 1, waiterId: string | null = null, waiterName: string | null = null, force: boolean = false, openTime: string | null = null): Observable<any> {
        return this.http.post('sales/orders/open-dine-in', { tableIds, branchCode, customerCount, waiterId, waiterName, force, openTime, orderDate: openTime });
    }

    reserveOrder(tableIds: string[], branchCode: string, customerCount: number = 1, waiterId: string | null = null, waiterName: string | null = null, force: boolean = false, openTime: string | null = null): Observable<any> {
        return this.http.post('sales/orders/reserve', { tableIds, branchCode, customerCount, waiterId, waiterName, force, openTime, orderDate: openTime });
    }

    checkInOrder(orderId: string, branchCode: string): Observable<any> {
        return this.http.post('sales/orders/check-in', { orderId, branchCode });
    }

    createTakeAwayOrder(orderData: any): Observable<any> {
        return this.http.post('sales/orders/take-away', orderData);
    }

    createDeliveryOrder(orderData: any): Observable<any> {
        return this.http.post('sales/orders/delivery', orderData);
    }

    saveOrder(orderData: any): Observable<any> {
        return this.http.post('sales/orders/save', orderData);
    }

    cancelOrder(orderId: string, branchCode: string, reason: string): Observable<any> {
        return this.http.post('sales/orders/cancel', { orderId, branchCode, reason });
    }

    // ── Menu Catalog & Search ──────────────────────────────
    getTopUsedMenuItems(branchCode: string): Observable<any> {
        return this.http.get(`sales/menu/top-used?branchCode=${branchCode}`);
    }

    getMenuVisibilityState(branchCode: string): Observable<any> {
        return this.http.get('menu-visibility/state', { branchCode });
    }

    getMenuItemsFilter(branchCode: string, keyword: string = '', type: string = '', groupId: string = ''): Observable<any> {
        return this.http.get(`sales/menu/items?branchCode=${branchCode}&keyword=${keyword}&type=${type}&groupId=${groupId}`);
    }

    sendOrderToKitchenBar(orderId: string, branchCode: string): Observable<any> {
        return this.http.post(`sale-orders/${orderId}/send-kitchen`, {
            branchCode: branchCode,
            orderId: orderId
        });
    }

    dispatchKitchenCancellations(request: any): Observable<any> {
        return this.http.post('kitchen/dispatch-cancellations', request);
    }

    printKitchenBarTicket(orderId: string, branchCode: string): Observable<any> {
        return this.http.post(`sales/orders/print-kitchen-bar-ticket?orderId=${orderId}&branchCode=${branchCode}`, {});
    }

    getKitchenRequests(branchCode: string, kitchenAreaIds: string[], menuItemId?: string): Observable<any> {
        let url = `kitchen/requests?branchCode=${branchCode}`;
        if (kitchenAreaIds && kitchenAreaIds.length > 0) {
            url += `&kitchenAreaIds=${kitchenAreaIds.join(',')}`;
        }
        if (menuItemId) {
            url += `&menuItemId=${menuItemId}`;
        }
        return this.http.get(url);
    }

    getKitchenSummary(branchCode: string, kitchenAreaIds: string[]): Observable<any> {
        let url = `kitchen/summary?branchCode=${branchCode}`;
        if (kitchenAreaIds && kitchenAreaIds.length > 0) {
            url += `&kitchenAreaIds=${kitchenAreaIds.join(',')}`;
        }
        return this.http.get(url);
    }

    serveItems(payload: any): Observable<any> {
        return this.http.post('kitchen/serve-items', payload);
    }

    soldOutItems(payload: any): Observable<any> {
        return this.http.post('kitchen/sold-out-items', payload);
    }

    // ── Chuyển / Gộp / Tách Bàn ──────────────────────────
    transferTableOrder(orderId: string, targetTableIds: string[], branchCode: string, note?: string): Observable<any> {
        return this.http.post('sales/orders/transfer-table', { orderId, targetTableIds, branchCode, note });
    }

    mergeOrders(targetOrderId: string, sourceOrderIds: string[], tableIds: string[] | null, branchCode: string): Observable<any> {
        return this.http.post('sales/orders/merge', { targetOrderId, sourceOrderIds, tableIds, branchCode });
    }

    splitOrder(payload: any): Observable<any> {
        return this.http.post('sales/orders/split', payload);
    }

    updateItems(orderData: any): Observable<any> {
        return this.http.post('sales/orders/update-items', orderData);
    }

    // ── Kiểm Đồ ───────────────────────────────────────────
    getCheckItems(orderId: string, branchCode: string): Observable<any> {
        return this.http.get(`sales/orders/${orderId}/check-items?branchCode=${branchCode}`);
    }

    checkItems(orderId: string, items: any[], branchCode: string): Observable<any> {
        return this.http.post('sales/orders/check-items', { orderId, items, branchCode });
    }

    printCheckItems(orderId: string, branchCode: string): Observable<any> {
        return this.http.post(`sales/orders/print-check-items?orderId=${orderId}&branchCode=${branchCode}`, {});
    }

    // ── Thanh Toán ────────────────────────────────────────
    printTemporaryBill(orderId: string, branchCode: string): Observable<any> {
        return this.http.post(`sales/orders/print-temporary-bill?orderId=${orderId}&branchCode=${branchCode}`, {});
    }

    payOrder(payload: any): Observable<any> {
        return this.http.post('sales/orders/payment', payload);
    }

    payCash(payload: any): Observable<any> {
        return this.http.post('sales/orders/payment/cash', payload);
    }

    payBankTransfer(payload: any): Observable<any> {
        return this.http.post('sales/orders/payment/bank-transfer', payload);
    }

    payCard(payload: any): Observable<any> {
        return this.http.post('sales/orders/payment/card', payload);
    }

    // ── Lookups ───────────────────────────────────────────
    getCustomers(branchCode: string, keyword: string = ''): Observable<any> {
        return this.http.get(`sales/customers?branchCode=${branchCode}&keyword=${keyword}`);
    }

    getEmployees(branchCode: string, keyword: string = ''): Observable<any> {
        return this.http.get(`sales/employees?branchCode=${branchCode}&keyword=${keyword}`);
    }

    getAvailableTargetTables(branchCode: string, floorId?: string): Observable<any> {
        let url = `sales/tables/available-targets?branchCode=${branchCode}`;
        if (floorId) {
            url += `&floorId=${floorId}`;
        }
        return this.http.get(url);
    }

    getOrderHistory(payload: any): Observable<any> {
        return this.http.post('sales/orders/history', payload);
    }
}
