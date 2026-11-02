import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelect } from 'primeng/multiselect';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { EmployeeService } from '../../../../employee/services/empoyee';
import { BranchService } from '../../../../branch/services/branch';
import { AccountService } from '../../../../system/account/services/account';

@Component({
    selector: 'app-employee-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, CheckboxModule, MultiSelect, ButtonModule, DialogWrap],
    templateUrl: './employee-create.html',
    styleUrl: './employee-create.scss'
})
export class EmployeeCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(EmployeeService);
    protected branchService = inject(BranchService);
    protected accountService = inject(AccountService);

    branchMultiOptions = signal<any[]>([]);
    ssoUserOptions = signal<any[]>([]);

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            Phone: [null],
            Email: [null, Validators.email],
            UserId: [null],
            ApplyBranchCodes: [[]],
            IsSales: [0],
            IsCashier: [0],
            IsDelivery: [0],
            IsWarehouse: [0],
            IsTechnician: [0],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        this.branchService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.branchMultiOptions.set(this.getResponseItems(res));
        });

        this.accountService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.ssoUserOptions.set(this.getResponseItems(res));
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            const rawData = this.config.data.data;
            
            // Map ApplyBranchCodes to array of string codes for MultiSelect
            let branches: string[] = [];
            if (rawData.ApplyBranchCodes) {
                if (Array.isArray(rawData.ApplyBranchCodes)) {
                    branches = rawData.ApplyBranchCodes.map((b: any) => typeof b === 'string' ? b : b.Code);
                } else if (typeof rawData.ApplyBranchCodes === 'string') {
                    branches = rawData.ApplyBranchCodes.split(',').map((b: string) => b.trim()).filter((b: string) => b.length > 0);
                }
            } else if (rawData.ApplyListBranchCodesString && typeof rawData.ApplyListBranchCodesString === 'string') {
                branches = rawData.ApplyListBranchCodesString.split(',').map((b: string) => b.trim()).filter((b: string) => b.length > 0);
            }

            this.form.patchValue({
                Code: rawData.Code,
                Name: rawData.Name,
                Phone: rawData.Phone,
                Email: rawData.Email,
                UserId: rawData.UserId,
                ApplyBranchCodes: branches,
                IsSales: rawData.IsSales || 0,
                IsCashier: rawData.IsCashier || 0,
                IsDelivery: rawData.IsDelivery || 0,
                IsWarehouse: rawData.IsWarehouse || 0,
                IsTechnician: rawData.IsTechnician || 0,
                Status: rawData.Status
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        // Map array of branch codes back to comma-separated string
        const branches = this.form.get('ApplyBranchCodes')?.value || [];
        const applyBranchCodesStr = branches.join(',');

        const formData = {
            ...this.form.value,
            ApplyBranchCodes: applyBranchCodesStr
        };

        const payload = this.edition() ? { ...this.config?.data.data, ...formData } : formData;
        const request$ = this.edition() ? this.mainService.update(payload) : this.mainService.create(payload);
        request$.subscribe((res: any) => {
            if (res.Code === 200) {
                this.onSuccess();
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: res.Message || 'Có lỗi xảy ra!'
                });
            }
        });
    }

    private onSuccess() {
        this.messageService.add({
            severity: 'info',
            summary: 'Thành công!'
        });

        setTimeout(() => {
            this.dialogRef?.close(true);
        }, 500);
    }
}
