import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class MaterialGroupService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('materialgroup/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('materialgroup', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('materialgroup', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('materialgroup?id=' + id);
    }
}
