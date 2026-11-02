import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ProductGroupService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('productgroup/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('productgroup', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('productgroup', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('productgroup?id=' + id);
    }
}
