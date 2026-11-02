import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProductTypeService } from '../../services/product-type';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';

@Component({
    selector: 'app-product-type-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, DialogWrap],
    templateUrl: './product-type-create.html',
    styleUrl: './product-type-create.scss'
})
export class ProductTypeCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    private productTypeService = inject(ProductTypeService);
    constructor() {
        super();
        this.form = this.fb.group({
            Code: ['', [Validators.required, Validators.maxLength(20)]],
            Name: ['', [Validators.required, Validators.maxLength(255)]]
        });
    }

    ngOnInit(): void {
        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.edition() ? { ...this.config?.data.data, ...this.form.value } : this.form.value;
        const request$ = this.edition() ? this.productTypeService.update(payload) : this.productTypeService.create(payload);
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
