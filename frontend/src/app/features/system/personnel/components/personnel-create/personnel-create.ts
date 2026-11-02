import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { AutoComplete } from 'primeng/autocomplete';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectButton } from 'primeng/selectbutton';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BranchService } from '@/features/branch/services/branch';
import { CityService } from '@/features/location/services/city';
import { DistrictService } from '@/features/location/services/district';
import { WardService } from '@/features/location/services/ward';
import { TabsModule } from 'primeng/tabs';
import { InputMask } from 'primeng/inputmask';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { AutoCompleteSearchDirective } from '@/shared/directive/autocompleteSearch';
import { EmployeeService } from '@/features/employee/services/empoyee';
import { PersonnelService } from '../../services/personnel';

type OptionItem = { Code: string; Name: string };
@Component({
    selector: 'app-personnel-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, AutoComplete, DatePickerModule, SelectButton, ToggleSwitchModule, TextareaModule, TabsModule, InputMask, DialogWrap, AutoCompleteSearchDirective],
    templateUrl: './personnel-create.html',
    styleUrl: './personnel-create.scss'
})
export class PersonnelCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    firstLoad = true;
    branchOptions = signal<OptionItem[]>([]);
    branchMultiOptions = signal<any[]>([]);
    leaderOptions = signal<OptionItem[]>([]);
    cityOptions = signal<OptionItem[]>([]);
    districtOptions = signal<OptionItem[]>([]);
    wardOptions = signal<OptionItem[]>([]); /*  */

    genderOptions = [
        { label: 'Nam', value: 1 },
        { label: 'Nữ', value: 2 }
    ];
    searchQuery = signal<any>({
        page: 1,
        size: 15,
        TextSearch: ''
    });

    service = inject(PersonnelService);
    employeeService = inject(EmployeeService);
    branchService = inject(BranchService);
    cityService = inject(CityService);
    districtService = inject(DistrictService);
    wardService = inject(WardService);

    constructor() {
        super();
        this.form = this.fb.group({
            Code: ['', [Validators.required, Validators.maxLength(20)]],
            Name: ['', [Validators.required, Validators.maxLength(255)]],

            BranchCode: [null, Validators.required],
            LeaderCode: [null],

            Gender: [1, Validators.required],
            DateOfBirth: [null],
            PhoneNumber: ['', [Validators.pattern(/^\+?\d[\d\s-]{9,14}$/)]],
            Address: [''],

            IdentityNumber: [''],
            TaxCode: [''],

            Email: ['', [Validators.email, Validators.maxLength(255)]],
            BankName: [''],

            Notes: [''],

            CityCode: [null],
            DistrictCode: [null],
            WardCode: [null],

            IsCustomer: [true],
            IsSupplier: [false],
            IsEmployee: [true],
            IsCollaborator: [false]
        });
    }
    ngOnInit(): void {
        if (this.config?.data?.edition) {
            this.edition.set(true);
            const d = this.config?.data.data;
            this.getPersonnelById(d.Id);

            this.form.get('Code')?.disable();
        } else {
            this.form.get('BranchCode')?.setValue(this.branchUser()[0] || null);
        }
    }

    getPersonnelById(id: string) {
        this.service.getById(id).subscribe((res) => {
            if (res.Code === 200) {
                console.log(res);

                this.patchFormByData(res.Data);
            }
        });
    }

    getBranchLabel(branch: any): string {
        return `${branch?.Code} - ${branch.Name}`;
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const raw = this.form.getRawValue();
        console.log(raw);

        const payload = {
            ...(this.edition() ? { Id: this.config?.data?.data?.Id } : {}),

            Code: raw.Code,
            Name: raw.Name,

            BranchCode: raw.BranchCode?.Code,

            Gender: raw.Gender,
            DateOfBirth: raw.DateOfBirth,

            LeaderCode: raw.LeaderCode?.Code,

            Address: raw.Address,
            PhoneNumber: raw.PhoneNumber,

            TaxCode: raw.TaxCode ? raw.TaxCode : null,
            IdentityNumber: raw.IdentityNumber,

            Email: raw.Email,
            BankName: raw.BankName,

            Notes: raw.Notes,

            CityCode: raw.CityCode?.Code,
            DistrictCode: raw.DistrictCode?.Code,
            WardCode: raw.WardCode?.Code,

            IsCustomer: raw.IsCustomer ? 1 : 0,
            IsSupplier: raw.IsSupplier ? 1 : 0,
            IsEmployee: raw.IsEmployee ? 1 : 0,
            IsCollaborator: raw.IsCollaborator ? 1 : 0
        };

        const request$ = this.edition() ? this.service.update(payload) : this.service.create(payload);

        request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
            if (res.Code === 200) this.onSuccess();
            if (res.Code === 409) {
                this.messageService.add({ severity: 'warn', summary: 'Mã không được trùng!' });
            }
        });
    }

    private onSuccess() {
        this.messageService.add({ severity: 'info', summary: 'Thành công!' });
        setTimeout(() => this.dialogRef?.close(true), 500);
    }

    private patchFormByData(d: any) {
        this.form.patchValue({
            Code: d.CustomerCode,
            Name: d.CustomerName,
            BranchCode: d.Branch ? { Code: d.Branch.Code, Name: d.Branch.Name } : null,
            Gender: d.Gender,
            DateOfBirth: d.DateOfBirth ? new Date(d.DateOfBirth) : null,
            LeaderCode: d.IntroduceEmployeeCode ? { Code: d.IntroduceEmployeeCode, Name: d.IntroduceEmployeeCode } : null,
            Address: d.Address,
            PhoneNumber: d.PhoneNumber,
            TaxCode: d.TaxCode ? d.TaxCode : null,
            IdentityNumber: d.IdentityNumber,
            Email: d.Email,
            BankName: d.BankName,
            Notes: d.Notes,
            CityCode: d.City ? { Code: d.City.Code, Name: d.City.Name } : null,
            DistrictCode: d.District ? { Code: d.District.Code, Name: d.District.Name } : null,
            WardCode: d.Ward ? { Code: d.Ward.Code, Name: d.Ward.Name } : null,
            IsCustomer: d.IsLoyalCustomer === 1,
            IsSupplier: d.IsSupplier === 1,
            IsEmployee: d.IsEmployee === 1,
            IsCollaborator: d.IsCollaborator === 1
        });
    }
}
