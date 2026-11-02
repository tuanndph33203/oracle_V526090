import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class WardService {
    private http = inject(BaseHttpService);

    getByFilter(query: any): Observable<any> {
        return this.http.post('ward/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('ward', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('ward', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('ward?id=' + id);
    }
}
