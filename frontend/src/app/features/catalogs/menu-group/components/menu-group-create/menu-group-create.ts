import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { PreparationAreaService } from '../../../preparation-area/services/preparation-area';
import { MenuGroupService } from '../../services/menu-group';

@Component({
    selector: 'app-menu-group-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, InputNumberModule, ButtonModule, DialogWrap],
    templateUrl: './menu-group-create.html',
    styleUrl: './menu-group-create.scss'
})
export class MenuGroupCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(MenuGroupService);
    private preparationareaService = inject(PreparationAreaService);
    PreparationAreaIdOptions = signal<any[]>([]);

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
            Priority: [null, [Validators.min(0)]],
            PreparationAreaId: [null, null],
            Description: [null, null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
        this.preparationareaService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.PreparationAreaIdOptions.set(this.getResponseItems(res));
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                Type: this.config?.data.data.Type,
                Priority: this.config?.data.data.Priority ?? null,
                PreparationAreaId: this.config?.data.data.PreparationAreaId,
                Description: this.config?.data.data.Description,
                Status: this.config?.data.data.Status
            });
            this.form.get('Code')?.disable();
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
