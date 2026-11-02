import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ICity } from '../models/city';
import { ApiResponse } from '@/core/models/common';

@Injectable({
    providedIn: 'root'
})
export class CityService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<ICity>> {
        return this.http.post('city/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('city', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('city', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('city?id=' + id);
    }
}
