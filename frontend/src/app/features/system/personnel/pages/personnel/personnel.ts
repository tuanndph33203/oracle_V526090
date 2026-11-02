import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { ITableAction, ITableConfig } from '@/core/models/common';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputIcon } from 'primeng/inputicon';
import { IconField } from 'primeng/iconfield';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';;
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { PersonnelService } from '../../services/personnel';
import { PersonnelCreate } from '../../components/personnel-create/personnel-create';
import { colsTempPersonnel } from '../../constants/personnel';
@Component({
    selector: 'app-personnel',
    imports: [CommonModule, TableMeta, SelectModule, DatePickerModule, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './personnel.html',
    styleUrl: './personnel.scss'
})
export class Personnel extends BaseCrudPage<any> implements OnInit {
    colsTemp = signal<ITableConfig[]>(colsTempPersonnel);

    protected service = inject(PersonnelService);
    protected formComponent = PersonnelCreate;
    protected entityName = 'nhân viên';

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    ngOnInit() {
        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
                startWith(this.formSearch.value),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
            )
            .subscribe((filters) => {
                this.onSearch(filters);
            });
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }
}
