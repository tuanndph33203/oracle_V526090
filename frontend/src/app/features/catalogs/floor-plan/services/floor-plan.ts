import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class FloorPlanService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('floorplan/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('floorplan', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('floorplan', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('floorplan?id=' + id);
    }
}
