import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { HttpResponse } from '@angular/common/http';
@Injectable({
    providedIn: 'root'
})
export class ProductService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<any>> {
        return this.http.post('product/getby-filter', query);
    }

    getById(id?: string): Observable<ApiResponseDetail<any>> {
        return this.http.get('product/getby-id?Id=' + id);
    }

    create(data: any): Observable<any> {
        return this.http.post('product', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('product', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('product?id=' + id);
    }
    exportExcel(query?: any): Observable<HttpResponse<Blob>> {
        return this.http.post('product/export-excel', query, undefined, {
            responseType: 'blob',
            observe: 'response'
        });
    }
    
}
