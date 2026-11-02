import { BaseHttpService } from '@/core/services/base-http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpResponse } from '@angular/common/http';

@Injectable({
    providedIn: 'root'
})
export class MenuItemService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('menuitem/getby-filter', query);
    }

    getById(id: string): Observable<any> {
        return this.http.get('menuitem/getby-id?id=' + id);
    }

    create(data: any): Observable<any> {
        return this.http.post('menuitem', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('menuitem', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('menuitem?id=' + id);
    }

    toggleAvailability(id: string, isAvailable: boolean): Observable<any> {
        return this.http.patch('menuitem/toggle-availability', { id, isAvailable });
    }

    exportExcel(query?: any): Observable<HttpResponse<Blob>> {
        return this.http.post('menuitem/export-excel', query || {}, undefined, {
            responseType: 'blob',
            observe: 'response'
        });
    }

    resetAvailability(): Observable<any> {
        return this.http.post('menuitem/reset-availability', {});
    }
}
