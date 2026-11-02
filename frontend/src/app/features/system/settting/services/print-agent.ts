import { BaseHttpService } from '@/core/services/base-http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PrintAgentService {
    private readonly http = inject(BaseHttpService);

    getAgents(): Observable<any> {
        return this.http.get('print-agents');
    }

    printTest(agentId: string, printerId: string): Observable<any> {
        return this.http.post(`print-agents/${agentId}/printers/${printerId}/test`, {});
    }

    getAreaMappings(branchId: string): Observable<any> {
        return this.http.get('print-agents/area-mappings', { branchId });
    }

    saveAreaMappings(payload: any): Observable<any> {
        return this.http.put('print-agents/area-mappings', payload);
    }
}
