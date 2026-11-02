import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { BranchService } from '../../services/branch';

@Component({
    selector: 'app-branch-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, ButtonModule, DialogWrap],
    templateUrl: './branch-create.html',
    styleUrl: './branch-create.scss'
})
export class BranchCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(BranchService);
    

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            Address: [null, null],
            MapUrl: [null, null],
            Phone: [null, null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                Address: this.config?.data.data.Address,
                MapUrl: this.config?.data.data.MapUrl,
                Phone: this.config?.data.data.Phone,
                Status: this.config?.data.data.Status
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.edition() ? { ...this.config?.data.data, ...this.form.value } : this.form.value;
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
