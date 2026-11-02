import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class PreparationAreaService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('preparationarea/getby-filter', query);
    }

    getById(id: string): Observable<any> {
        return this.http.get('preparationarea/getby-id?id=' + id);
    }

    create(data: any): Observable<any> {
        return this.http.post('preparationarea', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('preparationarea', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('preparationarea?id=' + id);
    }
}
