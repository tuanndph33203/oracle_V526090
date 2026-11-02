import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, ApiResponseDetail } from '@/core/models/common';

@Injectable({
    providedIn: 'root'
})
export class BranchService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<any>> {
        return this.http.post('branch/getby-filter', query);
    }

    getById(id?: string): Observable<ApiResponseDetail<any>> {
        return this.http.get('branch/getby-id?Id=' + id);
    }

    create(data: any): Observable<any> {
        return this.http.post('branch', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('branch', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('branch?id=' + id);
    }
}
