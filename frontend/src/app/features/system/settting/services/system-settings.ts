import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ISystemSettingBulkItem {
    Id: string;
    Name: string;
    Value: string;
    DataType: string;
    Module: string;
    Description: string;
    IsActive: boolean;
}

export interface ISystemSettingBulkPayload {
    Items: ISystemSettingBulkItem[];
}

@Injectable({
    providedIn: 'root'
})
export class SystemSettingsService {
    private http = inject(BaseHttpService);

    create(data: any): Observable<any> {
        return this.http.post('systemsetting', data);
    }

    getAll(): Observable<any> {
        return this.http.get('systemsetting/getall');
    }

    updateBulk(data: ISystemSettingBulkPayload): Observable<any> {
        return this.http.put('systemsetting/bulk', data);
    }

    update(data: any): Observable<any> {
        return this.http.put('systemsetting', data);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('systemsetting?id=' + id);
    }
}
