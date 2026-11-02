import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AutoCompleteResolverService {
    resolve(code: string | null, options: any[], service?: any): Observable<any | null> {
        if (!code) return of(null);

        const cached = options.find((x) => x.Code === code);
        if (cached) return of(cached);

        return service.getByFilter({ TextSearch: code });
    }
}