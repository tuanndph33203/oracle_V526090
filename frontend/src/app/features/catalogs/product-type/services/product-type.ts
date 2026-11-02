import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ProductTypeService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('producttype/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('producttype', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('producttype', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('producttype?id=' + id);
    }
}
