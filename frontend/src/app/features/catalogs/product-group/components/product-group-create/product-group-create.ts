import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Select } from 'primeng/select';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { DialogWrap } from "@/shared/ui/layout/dialog-wrap/dialog-wrap";
import { groupOptionConfig } from '../../contants/product-group';
import { ProductGroupService } from '../../services/product-group';

@Component({
    selector: 'app-product-group-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, ToggleSwitch, Select, DialogWrap],
    templateUrl: './product-group-create.html',
    styleUrl: './product-group-create.scss'
})
export class ProductGroupCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    groupOptionConfig = groupOptionConfig ?? [];
    private productGroupService = inject(ProductGroupService);

    constructor() {
        super();
        this.form = this.fb.group({
            Code: ['', [Validators.required, Validators.maxLength(20)]],
            Name: ['', [Validators.required, Validators.maxLength(255)]],
            Type: [1],
            Status: [1]
        });
    }

    ngOnInit(): void {
        console.log(groupOptionConfig);

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                Type: this.config?.data.data.Type,
                Status: this.config?.data.data.Status === 1 ? true : false
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.edition() ? { ...this.config?.data.data, ...this.form.value, Status: this.form.get('Status')?.value === true ? 1 : 0 } : this.form.value;
        const request$ = this.edition() ? this.productGroupService.update(payload) : this.productGroupService.create(payload);
        request$.subscribe((res) => {
            if (res.Code === 200) {
                this.onSuccess();
            }
            if (res.Code === 409) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Mã không được trùng!'
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
