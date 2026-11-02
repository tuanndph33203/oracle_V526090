import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toLocalISOString } from '@/core/utils/toLocalISOString';

@Injectable({
    providedIn: 'root'
})
export class ReportService {
    private http = inject(BaseHttpService);
    private httpDirect = inject(HttpClient);

    getRevenueReportByItem(query: any): Observable<any> {
        return this.http.get('reports/revenue-by-item', query);
    }
    getDetailedRevenueReport(query: any): Observable<any> {
        return this.http.get('reports/detailed-revenue', query);
    }

    getItemInvoiceDetails(itemId: string, query: any): Observable<any> {
        return this.http.get(`reports/revenue-by-item/${itemId}/invoices`, query);
    }

    getDailyRevenue(query: any): Observable<any> {
        return this.http.get('reports/daily-revenue', query);
    }

    getDailyInvoiceDetails(dateStr: string, query: any): Observable<any> {
        return this.http.get(`reports/daily-revenue/${dateStr}/invoices`, query);
    }

    getInvoiceList(query: any): Observable<any> {
        return this.http.get('reports/invoices', query);
    }

    getInvoiceDetails(invoiceNo: string): Observable<any> {
        return this.http.get(`invoices/${invoiceNo}/details`, {});
    }

    syncData(): Observable<any> {
        return this.http.post('reports/sync-data', {});
    }

    exportExcel(query: any): Observable<Blob> {
        let params = new HttpParams();
        if (query) {
            Object.entries(query).forEach(([key, value]) => {
                if (value !== null && value !== undefined && value !== '') {
                    if (value instanceof Date) {
                        params = params.set(key, toLocalISOString(value) || '');
                    } else {
                        params = params.set(key, String(value));
                    }
                }
            });
        }
        return this.httpDirect.get(`${environment.apiUrl}reports/export-excel`, {
            params,
            responseType: 'blob'
        });
    }
}
