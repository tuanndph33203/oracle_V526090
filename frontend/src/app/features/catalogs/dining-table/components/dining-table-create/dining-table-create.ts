import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { DiningTableService } from '../../services/dining-table';
import { FloorPlanService } from '../../../floor-plan/services/floor-plan';
import { BranchService } from '../../../../branch/services/branch';

@Component({
    selector: 'app-dining-table-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, ButtonModule, DialogWrap],
    templateUrl: './dining-table-create.html',
    styleUrl: './dining-table-create.scss'
})
export class DiningTableCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(DiningTableService);
    private floorplanService = inject(FloorPlanService);
    private branchService = inject(BranchService);
    
    FloorPlanIdOptions = signal<any[]>([]);
    BranchIdOptions = signal<any[]>([]);
    
    qrInfo = signal<any>(null);
    qrLoading = signal<boolean>(false);

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            FloorPlanId: [null, Validators.required],
            BranchId: [null, Validators.required],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        this.floorplanService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.FloorPlanIdOptions.set(this.getResponseItems(res));
        });
        this.branchService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.BranchIdOptions.set(this.getResponseItems(res));
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                FloorPlanId: this.config?.data.data.FloorPlanId,
                BranchId: this.config?.data.data.BranchId,
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
