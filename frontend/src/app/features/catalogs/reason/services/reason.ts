import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ReasonService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('reason/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('reason', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('reason', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('reason?id=' + id);
    }
}
