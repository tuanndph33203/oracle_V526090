import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class MenuGroupService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('menugroup/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('menugroup', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('menugroup', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('menugroup?id=' + id);
    }
}
