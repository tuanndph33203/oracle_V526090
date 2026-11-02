import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiResponse } from '@/core/models/common';

@Injectable({
    providedIn: 'root'
})
export class PersonnelService {
    private http = inject(BaseHttpService);

    getByFilter(query?: any): Observable<ApiResponse<any>> {
        return this.http.post('employee/getby-filter', query).pipe(
            map((res: any) => {
                if (res?.Data?.Content) {
                    res.Data.Content = res.Data.Content.map((x: any) => ({
                        ...x,
                        CustomerCode: x.Code,
                        CustomerName: x.Name,
                        PhoneNumber: x.Phone,
                        IdentityNumber: x.CustomerCardId,
                        BankName: x.BankAccount,
                        IntroduceEmployeeCode: x.Leader?.Code
                    }));
                }
                return res;
            })
        );
    }

    create(data: any): Observable<any> {
        const payload = {
            ...data,
            Phone: data.PhoneNumber,
            CustomerCardId: data.IdentityNumber,
            BankAccount: data.BankName
        };
        return this.http.post('employee', payload);
    }

    getById(id: string): Observable<any> {
        return this.http.get('employee/getby-id?id=' + id).pipe(
            map((res: any) => {
                if (res?.Data) {
                    res.Data = {
                        ...res.Data,
                        CustomerCode: res.Data.Code,
                        CustomerName: res.Data.Name,
                        PhoneNumber: res.Data.Phone,
                        IdentityNumber: res.Data.CustomerCardId,
                        BankName: res.Data.BankAccount,
                        IntroduceEmployeeCode: res.Data.Leader?.Code,
                        City: res.Data.City ? { Code: res.Data.City.Code, Name: res.Data.City.Name } : null,
                        District: res.Data.District ? { Code: res.Data.District.Code, Name: res.Data.District.Name } : null,
                        Ward: res.Data.Ward ? { Code: res.Data.Ward.Code, Name: res.Data.Ward.Name } : null
                    };
                }
                return res;
            })
        );
    }

    update(data: any): Observable<any> {
        const payload = {
            ...data,
            Phone: data.PhoneNumber,
            CustomerCardId: data.IdentityNumber,
            BankAccount: data.BankName
        };
        return this.http.put('employee', payload);
    }

    delete(id: string): Observable<any> {
        return this.http.delete('employee?id=' + id);
    }
}

