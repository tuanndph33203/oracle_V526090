import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissionService } from '../../services/permission';
import { CommonModule } from '@angular/common';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { SelectButton } from 'primeng/selectbutton';

@Component({
    selector: 'app-permission-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, Textarea, DialogWrap, SelectButton],
    templateUrl: './permission-create.html',
    styleUrl: './permission-create.scss'
})
export class PermissionCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    stateOptions: any[] = [
        { label: 'Có', value: true },
        { label: 'Không', value: false }
    ];
    private permissionService = inject(PermissionService);

    constructor() {
        super();
        this.form = this.fb.group({
            name: ['', [Validators.required, Validators.maxLength(255)]],
            code: ['', [Validators.required, Validators.maxLength(100)]],
            description: [''],
            categoryPermission: [''],
            isView: [false]
        });
    }

    ngOnInit(): void {
        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                name: this.config?.data.data.name,
                code: this.config?.data.data.code,
                description: this.config?.data.data.description,
                isView: this.config?.data.data.isView,
                categoryPermission: this.config?.data.data.categoryPermission
            });
        }
        this.form.get('code')?.valueChanges.subscribe((value: string) => {
            if (!value) {
                this.form.get('categoryPermission')?.setValue('');
                return;
            }
            const lastIndex = value.lastIndexOf('_');
            const category = lastIndex > 0 ? value.substring(0, lastIndex) : value;
            this.form.get('categoryPermission')?.setValue(category, { emitEvent: false });
        });
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.edition() ? { ...this.config?.data.data, ...this.form.value } : this.form.value;
        const request$ = this.edition() ? this.permissionService.update(payload) : this.permissionService.create(payload);
        request$.subscribe((res) => {
            if (res.code === 200) {
                this.onSuccess();
            }
            if (res.code === 409) {
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
        ``;

        setTimeout(() => {
            this.dialogRef?.close(true);
        }, 500);
    }
}
