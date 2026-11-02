import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class TaxRateService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('taxrate/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('taxrate', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('taxrate', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('taxrate?id=' + id);
    }
}
