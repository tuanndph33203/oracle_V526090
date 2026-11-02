import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ServicePreferenceService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('servicepreference/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('servicepreference', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('servicepreference', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('servicepreference?id=' + id);
    }
}
