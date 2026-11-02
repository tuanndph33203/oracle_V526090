import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class RoleService {
    private http = inject(BaseHttpService);
    authApiUrl = environment.authencationUrl;

    getByFilter(query?: any): Observable<any> {
        return this.http.get('role', query, this.authApiUrl);
    }

    getById(id?: string): Observable<any> {
        return this.http.get('role/getby-id?Id=' + id, {}, this.authApiUrl);
    }

    create(data: any): Observable<any> {
        return this.http.post('role', data, {}, {},  this.authApiUrl);
    }

    update(data: any): Observable<any> {
        return this.http.put('role', data, {}, {}, this.authApiUrl);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('role?id=' + id, {}, this.authApiUrl);
    }
}
