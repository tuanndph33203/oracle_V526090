import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class BusinessPartnerService {
    private http = inject(BaseHttpService);
    private rawHttp = inject(HttpClient);

    getByFilter(query?: any): Observable<any> {
        return this.http.post('businesspartner/getby-filter', query);
    }

    create(data: any): Observable<any> {
        return this.http.post('businesspartner', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('businesspartner', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('businesspartner?id=' + id);
    }

    getNextCode(partnerType: number = 1): Observable<any> {
        return this.http.get(`businesspartner/get-next-code?partnerType=${partnerType}`);
    }

    importHo(file: File): Observable<any> {
        const formData = new FormData();
        formData.append('File', file);
        return this.rawHttp.post<any>(`${environment.apiUrl}businesspartner/import-ho`, formData);
    }
}
