import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { BusinessPartnerService } from '../../services/business-partner';
import { BusinessPartnerGroupService } from '../../../business-partner-group/services/business-partner-group';
import { finalize } from 'rxjs';

@Component({
    selector: 'app-business-partner-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, DatePickerModule, ButtonModule, DialogWrap],
    templateUrl: './business-partner-create.html',
    styleUrl: './business-partner-create.scss'
})
export class BusinessPartnerCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(BusinessPartnerService);
    private groupService = inject(BusinessPartnerGroupService);
    
    PartnerGroupIdOptions = signal<any[]>([]);

    partnerTypeOptions = [
        { value: 1, label: 'Khách hàng' },
        { value: 2, label: 'Nhà cung cấp' },
        { value: 3, label: 'Cả hai' }
    ];

    genderOptions = [
        { value: 1, label: 'Nam' },
        { value: 2, label: 'Nữ' },
        { value: 3, label: 'Khác' }
    ];

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            PartnerTypes: [1, Validators.required],
            Code: [null],
            Name: [null],
            CompanyName: [null],
            PartnerGroupId: [null],
            IdentityNumber: [null],
            MobilePhone: [null],
            DateOfBirth: [null],
            Gender: [null],
            TaxCode: [null],
            Email: [null, Validators.email],
            Address: [null],
            Note: [null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        this.groupService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.PartnerGroupIdOptions.set(this.getResponseItems(res));
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            const rawData = this.config.data.data;
            this.form.patchValue({
                PartnerTypes: rawData.PartnerTypes,
                Code: rawData.Code,
                Name: rawData.Name,
                CompanyName: rawData.CompanyName,
                PartnerGroupId: rawData.PartnerGroupId,
                IdentityNumber: rawData.IdentityNumber,
                MobilePhone: rawData.MobilePhone,
                DateOfBirth: rawData.DateOfBirth ? new Date(rawData.DateOfBirth) : null,
                Gender: rawData.Gender,
                TaxCode: rawData.TaxCode,
                Email: rawData.Email,
                Address: rawData.Address,
                Note: rawData.Note,
                Status: rawData.Status
            });
        } else {
            this.loadNextCode(this.form.get('PartnerTypes')?.value || 1);
            this.form.get('PartnerTypes')?.valueChanges.subscribe((val) => {
                if (val) {
                    this.loadNextCode(val);
                }
            });
        }
    }

    private loadNextCode(partnerType: number) {
        if (this.edition()) return;
        this.mainService.getNextCode(partnerType).subscribe((res: any) => {
            if (res?.Code === 200 && res.Data) {
                this.form.patchValue({ Code: res.Data });
            }
        });
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

        // Custom validation: Họ tên hoặc Tên công ty không được trống
        const nameVal = this.form.get('Name')?.value;
        const compVal = this.form.get('CompanyName')?.value;
        if (!nameVal && !compVal) {
            this.messageService.add({
                severity: 'error',
                summary: 'Họ tên hoặc Tên công ty không được để trống.'
            });
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
            return 'Mã hoặc Số điện thoại đối tác đã tồn tại trong hệ thống!';
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
