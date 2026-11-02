import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { Promotion } from '../models/promotion';

@Injectable({
    providedIn: 'root'
})
export class PromotionService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<any>> {
        return this.http.post('Promotion/getby-filter', query);
    }

    getById(id: string): Observable<ApiResponseDetail<Promotion>> {
        return this.http.get(`Promotion/getby-id?id=${id}`);
    }

    create(data: Promotion): Observable<ApiResponse<any>> {
        return this.http.post('Promotion', data);
    }

    update(data: Promotion): Observable<ApiResponse<any>> {
        return this.http.put('Promotion', data);
    }

    delete(id: string): Observable<ApiResponse<any>> {
        return this.http.delete(`Promotion?id=${id}`);
    }
}
