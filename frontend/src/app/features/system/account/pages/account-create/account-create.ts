import { Component, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { AutoCompleteModule, AutoCompleteSelectEvent } from 'primeng/autocomplete';
import { BranchService } from '@/features/branch/services/branch';
import { arrayToString } from '@/core/utils/arrayToString';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { AutoCompleteSearchDirective } from '@/shared/directive/autocompleteSearch';
import { AccountService } from '../../services/account';
import { RoleService } from '@/features/system/role/services/role';
import { EmployeeService } from '@/features/employee/services/empoyee';
import { CityService } from '@/features/location/services/city';
import { DistrictService } from '@/features/location/services/district';
import { WardService } from '@/features/location/services/ward';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { DatePickerModule } from 'primeng/datepicker';
import { RadioButton } from 'primeng/radiobutton';
import { MultiSelect } from 'primeng/multiselect';
import { Password } from 'primeng/password';
import { InputMask } from 'primeng/inputmask';
import { buildOptionsFromEnum } from '@/shared/helper/buildOptionsFromEnum';
import { ACTIVE_LABEL } from '../../constants/account';
import { ActivatedRoute, Router } from '@angular/router';
import { PersonnelService } from '@/features/system/personnel/services/personnel';
@Component({
    selector: 'app-account-create',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, AutoCompleteModule, AutoCompleteSearchDirective, SelectModule, InputTextModule, CheckboxModule, DatePickerModule, RadioButton, MultiSelect, Password, InputMask],
    templateUrl: './account-create.html',
    styleUrl: './account-create.scss'
})
export class AccountCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;

    data = signal<any>(null);
    roles = signal<any[]>([]);
    employees = signal<any[]>([]);
    personnels = signal<any[]>([]);
    cityOptions = signal<any[]>([]);
    districtOptions = signal<any[]>([]);
    wardOptions = signal<any[]>([]);
    statusOptions = buildOptionsFromEnum(ACTIVE_LABEL);
    branchMultiOptions = signal<any[]>([]);

    branchOptions = signal<any[]>([]);
    searchQuery = signal<any>({
        page: 1,
        size: 15,
        TextSearch: ''
    });

    accountService = inject(AccountService);
    roleService = inject(RoleService);
    branchService = inject(BranchService);
    employeeService = inject(EmployeeService);
    personnelService = inject(PersonnelService);
    cityService = inject(CityService);
    districtService = inject(DistrictService);
    wardService = inject(WardService);
    router = inject(Router);
    activate = inject(ActivatedRoute);

    constructor() {
        super();
        this.form = this.fb.group({
            UserCode: [''],
            UserName: [''],
            Email: ['', [Validators.email, Validators.required]],
            Password: ['', [Validators.minLength(6)]],
            PasswordConfirm: ['', [Validators.minLength(6)]],
            DateOfBirth: [],
            Gender: [true],
            PhoneNumber: [],
            NumberId: [],
            Address: [],
            CityCode: [],
            DistrictCode: [],
            WardCode: [],
            country: ['Việt Nam'],
            ApplyBranchCodes: [[], Validators.required],
            // BranchCode: [],
            Active: [0],
            RoleIds: [[], Validators.required],
            Employee: [null],
            IsDelivery: [false],
            IsTechnician: [false],
            IsWarehouse: [false],
            IsEmployee: [true]
        });
    }

    ngOnInit(): void {
        this.activate.params.subscribe((params) => {
            if (params['id']) {
                this.edition.set(true);
                this.form.disable();
                ['Email', 'RoleIds', 'ApplyBranchCodes'].forEach((key) => {
                    this.form.get(key)?.enable();
                });
                this.accountService.getById(params['id']).subscribe((res) => {
                    const data = res?.Data;
                    this.data.set(data);
                    this.form.patchValue({
                        UserCode: data?.Code,
                        UserName: data?.Name,
                        Email: data?.Email,
                        DateOfBirth: data?.DateOfBirth,
                        Gender: data?.Gender === 1,
                        PhoneNumber: data?.Phone,
                        NumberId: data?.CustomerCardId,
                        Address: data?.Address,
                        CityCode: data?.City?.Code,
                        DistrictCode: data?.District?.Code,
                        WardCode: data?.Ward?.Code,
                        Active: data?.Status,
                        // BranchCode: data?.Branch?.Code,
                        ApplyBranchCodes: data?.ApplyListBranchCodesString?.map((x: string) => x.trim()) ?? [],
                        RoleIds: data?.Roles?.map((x: any) => ({ name: x.Name, code: x.Code, id: x.Id })) ?? [],
                        IsDelivery: !!data?.IsDelivery,
                        IsWarehouse: !!data?.IsWarehouse,
                        IsTechnician: !!data?.IsTechnician,
                        Employee: data?.Code ? { CustomerCode: data.Code, CustomerName: data.Name } : null
                    });
                });
            }
        });

        this.roleService.getByFilter().subscribe((res) => {
            this.roles.set(this.getResponseItems(res));
        });
        this.form.get('IsEmployee')?.disable();
        this.form.get('CityCode')?.valueChanges.subscribe((value) => {
            if (!value) {
                this.form.get('DistrictCode')?.disable();
                this.form.get('WardCode')?.disable();
            } else {
                this.form.get('DistrictCode')?.enable();
                this.form.get('WardCode')?.enable();
            }
        });
    }

    getCustomerLabel(branch: any): string {
        return `${branch?.CustomerCode} - ${branch.CustomerName}`;
    }

    onEmployeeChange(event: AutoCompleteSelectEvent | null) {
        const employee = event?.value;
        if (employee) {
            this.form.get('Password')?.setValidators([]);
            this.form.get('PasswordConfirm')?.setValidators([]);
            this.form.get('Password')?.setValue('');
            this.form.get('PasswordConfirm')?.setValue('');
            this.form.get('Password')?.disable();
            this.form.get('PasswordConfirm')?.disable();
        } else {
            this.form.get('Password')?.setValidators([Validators.required, Validators.minLength(6)]);
            this.form.get('PasswordConfirm')?.setValidators([Validators.required, Validators.minLength(6)]);
            this.form.get('Password')?.enable();
            this.form.get('PasswordConfirm')?.enable();
        }
        this.form.patchValue({
            UserCode: employee?.CustomerCode || '',
            UserName: employee?.CustomerName || '',
            DateOfBirth: employee?.DateOfBirth ? new Date(employee.DateOfBirth) : null,
            Gender: employee?.Gender || null,
            PhoneNumber: employee?.Phone || '',
            Email: employee?.Email || '',
            NumberId: employee?.CustomerCardId || '',
            Address: employee?.Address || ''
        });
    }

    create(payload: any) {
        return this.accountService.create(payload);
    }

    update(payload: any) {
        return this.accountService.update(payload);
    }

    submit() {
        if (!this.validateForm()) return;
        const payload = this.edition()
            ? { FirstName: '', UserId: this.data().UserId, UserCode: this.data()?.Code, ApplyBranchCodes: arrayToString(this.form.value.ApplyBranchCodes), RoleIds: this.form.value.RoleIds?.map((r: any) => r.id), Email: this.form.value.Email }
            : {
                  UserCode: this.form.value.UserCode,
                  UserName: this.form.value.UserName,
                  Email: this.form.value.Email,
                  Password: this.form.value.Password,

                  DateOfBirth: this.form.value.DateOfBirth,
                  Gender: this.form.value.Gender ? 1 : 0,

                  PhoneNumber: this.form.value.PhoneNumber,
                  NumberId: this.form.value.NumberId,
                  Address: this.form.value.Address,

                  CityCode: this.form.value.CityCode?.Code ?? null,
                  DistrictCode: this.form.value.DistrictCode?.Code ?? null,
                  WardCode: this.form.value.WardCode?.Code ?? null,

                  ApplyBranchCodes: arrayToString(this.form.value.ApplyBranchCodes),

                  //   BranchCode: this.form.value.BranchCode?.Code ?? null,

                  Active: this.form.value.Active ?? 0,

                  RoleIds: this.form.value.RoleIds?.map((r: any) => r.id),

                  IsDelivery: this.form.value.IsDelivery ? 1 : 0,
                  IsWarehouse: this.form.value.IsWarehouse ? 1 : 0,
                  IsTechnician: this.form.value.IsTechnician ? 1 : 0,

                  IsEmployeeCodeEx: !!this.form.value.Employee
              };
        const request$ = this.edition() ? this.update(payload) : this.create(payload);
        request$.subscribe((res) => this.handleResponseAccount(res));
    }

    searchAutoCompleteAccount(event: any, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        const query = {
            page: 1,
            size: 15,
            TextSearch: event?.query || '',
            ...extraQuery
        };
        const request$ = service.getByFilter(query);
        request$.subscribe((res: any) => {
            target.set(this.getResponseItems(res));
        });
    }

    handleResponseAccount(res: any): void {
        this.isSubmitting.set(false);
        if (res?.Code === 200 || res?.code === 200) {
            this.messageService.add({
                severity: 'info',
                summary: 'Thành công!'
            });

            this.goBack();
            return;
        }

        let errorMsg = res?.Message || res?.message || 'Có lỗi xảy ra khi xử lý tài khoản';

        // Parse JSON error format if raw JSON string was returned
        if (typeof errorMsg === 'string' && errorMsg.includes('{') && errorMsg.includes('}')) {
            try {
                const jsonStart = errorMsg.indexOf('{');
                const jsonEnd = errorMsg.lastIndexOf('}');
                const jsonStr = errorMsg.substring(jsonStart, jsonEnd + 1);
                const parsed = JSON.parse(jsonStr);
                if (parsed.message) {
                    if (parsed.message === 'PhoneNumber exists') {
                        errorMsg = 'Số điện thoại đã tồn tại trên hệ thống!';
                    } else if (parsed.message === 'Email exists') {
                        errorMsg = 'Email đã tồn tại trên hệ thống!';
                    } else {
                        errorMsg = parsed.message;
                    }
                }
            } catch {
                // Ignore parse errors
            }
        }

        this.messageService.add({
            severity: 'warn',
            summary: errorMsg
        });
    }

    goBack() {
        this.router.navigate(['/systems/account']);
    }
}
