import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '@/core/models/common';
import { IUser } from '@/core/models/auth';

@Injectable({
    providedIn: 'root'
})
export class EmployeeService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<any>> {
        return this.http.post('employee/getby-filter', { ...query, IsEmployee: 1 });
    }

    create(data: any): Observable<any> {
        return this.http.post('employee', data);
    }

    getByCode(code: string): Observable<any> {
        return this.http.get('employee/getby-code?Code=' +code);
    }

    update(data: any): Observable<any> {
        return this.http.put('employee', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('employee?id=' + id);
    }
}
