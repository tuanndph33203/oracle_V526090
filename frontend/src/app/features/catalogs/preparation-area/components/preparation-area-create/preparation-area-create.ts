import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { PreparationAreaService } from '../../services/preparation-area';
import { BranchService } from '@/features/branch/services/branch';

@Component({
    selector: 'app-preparation-area-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, MultiSelectModule, ButtonModule, DialogWrap],
    templateUrl: './preparation-area-create.html',
    styleUrl: './preparation-area-create.scss'
})
export class PreparationAreaCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    branches = signal<any[]>([]);
    form: FormGroup;
    
    private mainService = inject(PreparationAreaService);
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
            Type: [null, Validators.required],
            DeviceType: [null, Validators.required],
            ProcessingArea: [null, null],
            BranchIds: [[], Validators.required],
            LabelPrintTiming: [null, null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        this.branchService.getByFilter({ Page: 1, Size: 1000 }).subscribe({
            next: (res: any) => {
                const data = res?.Data ?? res?.data;
                const list = data?.Content ?? data?.content ?? data?.Items ?? data?.items ?? data;
                this.branches.set((Array.isArray(list) ? list : []).filter((branch: any) => (branch.Status ?? branch.status) === 1));
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Không tải được danh sách chi nhánh' })
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                Type: this.config?.data.data.Type,
                DeviceType: this.config?.data.data.DeviceType,
                ProcessingArea: this.config?.data.data.ProcessingArea,
                BranchIds: this.config?.data.data.BranchIds ?? [],
                LabelPrintTiming: this.config?.data.data.LabelPrintTiming,
                Status: this.config?.data.data.Status
            });

            this.mainService.getById(this.config?.data.data.Id).subscribe({
                next: (res: any) => {
                    const area = res?.Data ?? res?.data;
                    if (area) this.form.patchValue({ BranchIds: area.BranchIds ?? area.branchIds ?? [] });
                },
                error: () => this.messageService.add({ severity: 'warn', summary: 'Không tải được chi nhánh đã gán; hãy kiểm tra lại trước khi lưu' })
            });
        }
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = {
            ...(this.edition() ? this.config?.data.data : {}),
            ...this.form.value,
            // Preserve legacy printer address values while the field is hidden from the UI.
            ProcessingArea: this.edition() ? this.config?.data.data.ProcessingArea : this.form.value.ProcessingArea,
            BranchIds: this.form.value.BranchIds ?? []
        };
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
