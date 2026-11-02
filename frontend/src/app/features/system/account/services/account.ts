import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class AccountService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('system/account/getby-filter', query);
    }

    getById(id?: string): Observable<any> {
        return this.http.get('system/account/getby-id?Id=' + id);
    }

    getUserProfile(id?: string): Observable<any> {
        return this.http.get('system/account/get-info?Id=' + id);
    }

    getRolesById(id?: string): Observable<any> {
        return this.http.get('system/account/get-roles?userId=' + id);
    }

    create(data: any): Observable<any> {
        return this.http.post('system/account', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('system/account', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('system/account?id=' + id);
    }
}
