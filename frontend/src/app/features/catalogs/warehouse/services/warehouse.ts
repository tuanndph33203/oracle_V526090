import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class WarehouseService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('warehouse/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('warehouse', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('warehouse', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('warehouse?id=' + id);
    }
}
