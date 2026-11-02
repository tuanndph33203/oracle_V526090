import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { MaterialService } from '../../services/material';
import { UnitService } from '../../../../catalogs/unit/services/unit';
import { MaterialGroupService } from '../../../material-group/services/material-group';

@Component({
    selector: 'app-material-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, ButtonModule, DialogWrap],
    templateUrl: './material-create.html',
    styleUrl: './material-create.scss'
})
export class MaterialCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    
    private mainService = inject(MaterialService);
private materialgroupService = inject(MaterialGroupService);
private unitService = inject(UnitService);
    MaterialGroupCodeOptions = signal<any[]>([]);
    UnitCodeOptions = signal<any[]>([]);

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            MaterialGroupCode: [null, Validators.required],
            UnitCode: [null, Validators.required],
            PurchasePrice: [null, null],
            SellingPrice: [null, null],
            Status: [1, Validators.required]
        });
    }

    ngOnInit(): void {
                this.materialgroupService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.MaterialGroupCodeOptions.set(this.getResponseItems(res));
        });
        this.unitService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.UnitCodeOptions.set(this.getResponseItems(res));
        });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            this.form.patchValue({
                Code: this.config?.data.data.Code,
                Name: this.config?.data.data.Name,
                MaterialGroupCode: this.config?.data.data.MaterialGroupCode,
                UnitCode: this.config?.data.data.UnitCode,
                PurchasePrice: this.config?.data.data.PurchasePrice,
                SellingPrice: this.config?.data.data.SellingPrice,
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
