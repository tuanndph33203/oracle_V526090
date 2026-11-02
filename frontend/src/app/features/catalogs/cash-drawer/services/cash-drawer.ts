import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class CashDrawerService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('cashdrawer/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('cashdrawer', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('cashdrawer', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('cashdrawer?id=' + id);
    }
}
