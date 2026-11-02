import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class DistrictService {
    private http = inject(BaseHttpService);

    getByFilter(query: any): Observable<any> {
        return this.http.post('district/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('district', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('district', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('district?id=' + id);
    }
}
