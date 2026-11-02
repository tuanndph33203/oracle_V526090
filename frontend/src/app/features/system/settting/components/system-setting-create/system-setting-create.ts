import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { Select } from 'primeng/select';
import { Button } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { SystemSettingsService } from '../../services/system-settings';
import { finalize } from 'rxjs';
import { InputText } from 'primeng/inputtext';

@Component({
    selector: 'app-system-setting-create',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, Select, Button, InputText],
    templateUrl: './system-setting-create.html',
    styleUrl: './system-setting-create.scss'
})
export class SystemSettingCreate extends BaseFormPage<any, any> implements OnInit {
    form: FormGroup;

    dataTypeOptions = [
        { value: 'Boolean', label: 'Boolean' },
        { value: 'String', label: 'String' },
        { value: 'Number', label: 'Number' }
    ];

    moduleOptions = [
        { value: 'SaleBill', label: 'Bán hàng' },
        { value: 'customer', label: 'Khách hàng' },
        { value: 'invoice-auto', label: 'Hóa đơn tự động' }
    ];

    isEdit = false;
    private settingsService = inject(SystemSettingsService);

    constructor() {
        super();
        this.form = this.fb.group({
            Id: [null],
            Code: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_-]+$/)]],
            Name: ['', Validators.required],
            Value: ['true', Validators.required],
            DataType: ['Boolean', Validators.required],
            Module: ['SaleBill', Validators.required],
            Description: ['']
        });
    }

    ngOnInit(): void {
        if (this.config?.data?.data) {
            this.isEdit = true;
            const data = this.config.data.data;
            this.form.patchValue(data);
            this.form.get('Code')?.disable();
        }
    }

    submit() {
        if (!this.validateForm()) return;

        const payload = this.form.getRawValue();
        this.isSubmitting.set(true);

        const request$ = this.isEdit
            ? this.settingsService.update(payload)
            : this.settingsService.create(payload);

        request$
            .pipe(finalize(() => this.isSubmitting.set(false)))
            .subscribe({
                next: (res) => {
                    if (res?.Code === 200 || res?.code === 200) {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Thành công',
                            detail: this.isEdit ? 'Cập nhật cấu hình hệ thống thành công' : 'Thêm cấu hình hệ thống thành công'
                        });
                        this.dialogRef?.close(true);
                    } else {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Thất bại',
                            detail: res?.Message || (this.isEdit ? 'Không thể cập nhật cấu hình' : 'Không thể thêm cấu hình')
                        });
                    }
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: err?.error?.Message || err?.message || (this.isEdit ? 'Có lỗi xảy ra khi cập nhật cấu hình' : 'Có lỗi xảy ra khi tạo cấu hình')
                    });
                }
            });
    }

    cancel() {
        this.dialogRef?.close(false);
    }
}
