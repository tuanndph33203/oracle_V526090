import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class PermissionService {
    private http = inject(BaseHttpService);
    authApiUrl = environment.authencationUrl;

    getByFilter(query?: any): Observable<any> {
        return this.http.get('permission', query, this.authApiUrl);
    }

    create(data: any): Observable<any> {
        return this.http.post('permission', data, {}, {}, this.authApiUrl);
    }

    update(data: any): Observable<any> {
        return this.http.put('permission', data, {}, {}, this.authApiUrl);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('permission?id=' + id, {}, this.authApiUrl);
    }
}
