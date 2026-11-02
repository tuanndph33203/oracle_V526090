import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { WorkShiftService } from '../../services/work-shift';
import { BranchService } from '@/features/branch/services/branch';

@Component({
    selector: 'app-work-shift-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, MultiSelect, ButtonModule, DialogWrap],
    templateUrl: './work-shift-create.html',
    styleUrl: './work-shift-create.scss'
})
export class WorkShiftCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    branches = signal<any[]>([]);
    
    private mainService = inject(WorkShiftService);
    private branchService = inject(BranchService);
    

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            StartTime: [null, Validators.required],
            EndTime: [null, Validators.required],
            ShiftType: [null, Validators.required],
            Status: [1, Validators.required],
            BranchIds: [[]]
        });
    }

    ngOnInit(): void {
        this.branchService.getByFilter({ Page: 1, Size: 100 }).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.branches.set(res?.Data?.Content || []);
            }
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                StartTime: this.config?.data.data.StartTime,
                EndTime: this.config?.data.data.EndTime,
                ShiftType: this.config?.data.data.ShiftType,
                Status: this.config?.data.data.Status,
                BranchIds: this.config?.data.data.BranchIds || []
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.edition() ? { ...this.config?.data.data, ...this.form.value } : this.form.value;
        console.log('[WorkShiftSubmit] Payload:', payload);
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
