import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ShiftSessionService {
    private http = inject(BaseHttpService);

    openShift(data: any): Observable<any> {
        return this.http.post('shifts/open', data);
    }

    getCurrentShift(branchCode: string, machineCode?: string): Observable<any> {
        let url = `shifts/current?branchCode=${branchCode}`;
        if (machineCode) {
            url += `&machineCode=${machineCode}`;
        }
        return this.http.get(url);
    }

    extendShift(data: any): Observable<any> {
        return this.http.post('shifts/extend', data);
    }

    getShiftSummary(id: string): Observable<any> {
        return this.http.get(`shifts/${id}/summary`);
    }

    submitCashCount(id: string, items: any[]): Observable<any> {
        return this.http.post(`shifts/${id}/cash-count`, items);
    }

    closeShift(data: any): Observable<any> {
        return this.http.post('shifts/close', data);
    }

    getShiftHistory(filter: any): Observable<any> {
        return this.http.post('shifts/history', filter);
    }

    getShiftById(id: string): Observable<any> {
        return this.http.get(`shifts/${id}`);
    }
}
