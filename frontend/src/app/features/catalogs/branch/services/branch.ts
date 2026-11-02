import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class BranchService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('branch/getby-filter', query);
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
