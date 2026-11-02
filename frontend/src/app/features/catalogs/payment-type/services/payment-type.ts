import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class PaymentTypeService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('paymenttype/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('paymenttype', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('paymenttype', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('paymenttype?id=' + id);
    }
}
