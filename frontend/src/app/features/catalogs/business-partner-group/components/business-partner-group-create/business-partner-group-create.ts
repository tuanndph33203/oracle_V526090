import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { BusinessPartnerGroupService } from '../../services/business-partner-group';
import { finalize } from 'rxjs';

@Component({
    selector: 'app-business-partner-group-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, ButtonModule, DialogWrap],
    templateUrl: './business-partner-group-create.html',
    styleUrl: './business-partner-group-create.scss'
})
export class BusinessPartnerGroupCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(BusinessPartnerGroupService);

    groupTypeOptions = [
        { value: 1, label: 'Nhóm khách hàng' },
        { value: 2, label: 'Nhóm nhà cung cấp' },
        { value: 3, label: 'Cả hai' }
    ];

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            GroupTypes: [null, Validators.required],
            Description: [null, null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                GroupTypes: this.config?.data.data.GroupTypes,
                Description: this.config?.data.data.Description,
                Status: this.config?.data.data.Status
            });
        }
    }

    private lastSubmitTime = 0;

    submit() {
        const now = Date.now();
        if (now - this.lastSubmitTime < 2000 || this.isSubmitting()) {
            return;
        }

        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.lastSubmitTime = now;
        this.isSubmitting.set(true);
        this.form.disable();

        const rawFormValue = this.form.getRawValue();
        const payload = this.edition() ? { ...this.config?.data.data, ...rawFormValue } : rawFormValue;
        const request$ = this.edition() ? this.mainService.update(payload) : this.mainService.create(payload);

        request$.subscribe({
            next: (res: any) => {
                if (res?.Code === 200 || res?.code === 200) {
                    this.onSuccess();
                } else {
                    this.lastSubmitTime = 0;
                    this.isSubmitting.set(false);
                    this.form.enable();
                    const rawMsg = res?.Message || res?.message || 'Có lỗi xảy ra!';
                    this.messageService.add({
                        severity: 'error',
                        summary: this.cleanErrorMessage(rawMsg)
                    });
                }
            },
            error: (err: any) => {
                this.lastSubmitTime = 0;
                this.isSubmitting.set(false);
                this.form.enable();
                const rawMsg = err?.error?.Message || err?.error?.message || err?.message || 'Có lỗi hệ thống xảy ra!';
                this.messageService.add({
                    severity: 'error',
                    summary: this.cleanErrorMessage(rawMsg)
                });
            }
        });
    }

    private fixMojibake(text: string): string {
        if (!text) return '';
        try {
            if (/[ÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ]/.test(text)) {
                const bytes = new Uint8Array(text.length);
                let isLatin1 = true;
                for (let i = 0; i < text.length; i++) {
                    const code = text.charCodeAt(i);
                    if (code > 255) {
                        isLatin1 = false;
                        break;
                    }
                    bytes[i] = code;
                }
                if (isLatin1) {
                    const decoded = new TextDecoder('utf-8').decode(bytes);
                    if (decoded && !decoded.includes('')) {
                        return decoded;
                    }
                }
            }
        } catch {
            // Ignore
        }
        return text;
    }

    private cleanErrorMessage(msg: string): string {
        if (!msg) return 'Có lỗi xảy ra!';
        let result = String(msg);
        try {
            if (result.includes('\\u')) {
                result = result.replace(/\\u([0-9a-fA-F]{4})/g, (_, hex) =>
                    String.fromCharCode(parseInt(hex, 16))
                );
            }
        } catch {
            // Ignore parse errors
        }

        result = this.fixMojibake(result);

        if (result.includes('IX_') || result.includes('Duplicate') || result.includes('UNIQUE') || result.includes('tồn tại')) {
            return 'Mã nhóm đối tác đã tồn tại trong hệ thống!';
        }

        return result;
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
