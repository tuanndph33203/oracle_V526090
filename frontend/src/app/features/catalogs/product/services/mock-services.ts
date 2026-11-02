import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class VatService {
    getByFilter(query?: any): Observable<any> {
        return of({
            Code: 200,
            Data: {
                Content: [
                    { Code: 'VAT0', Name: 'VAT 0%', VatNumber: 0 },
                    { Code: 'VAT5', Name: 'VAT 5%', VatNumber: 5 },
                    { Code: 'VAT10', Name: 'VAT 10%', VatNumber: 10 }
                ]
            }
        });
    }
}

@Injectable({ providedIn: 'root' })
export class ProductTypeService {
    getByFilter(query?: any): Observable<any> {
        return of({
            Code: 200,
            Data: {
                Content: [
                    { Code: 'SP', Name: 'Sản phẩm' },
                    { Code: 'DV', Name: 'Dịch vụ' }
                ]
            }
        });
    }
}

@Injectable({ providedIn: 'root' })
export class BranchService {
    getByFilter(query?: any): Observable<any> {
        return of({
            Code: 200,
            Data: {
                Content: [
                    { Code: '00', Name: 'Chi nhánh mặc định' }
                ]
            }
        });
    }
}

@Injectable({ providedIn: 'root' })
export class MonneyTermService {
    getByFilter(query?: any): Observable<any> {
        return of({
            Code: 200,
            Data: [
                { Id: '1', Code: 'MT1', Name: 'Mặc định' }
            ]
        });
    }
}

export const MealLabel = {
    0: 'Không',
    1: 'Có'
};
