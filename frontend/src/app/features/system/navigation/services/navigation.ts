import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class NavigationService {
    private http = inject(BaseHttpService);
    authApiUrl = environment.authencationUrl;

    getByFilter(query?: { isTree?: boolean; isAll?: boolean }): Observable<any> {
        return this.http.get('navigation', query, this.authApiUrl);
    }

    getById(id?: string): Observable<ApiResponseDetail<any>> {
        return this.http.get('navigation/getby-id?Id=' + id, {}, this.authApiUrl);
    }

    create(data: any): Observable<any> {
        return this.http.post('navigation', data, {}, {}, this.authApiUrl);
    }

    update(data: any): Observable<any> {
        return this.http.put('navigation', data, {}, {}, this.authApiUrl);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('navigation?navId=' + id, {}, this.authApiUrl);
    }
}
