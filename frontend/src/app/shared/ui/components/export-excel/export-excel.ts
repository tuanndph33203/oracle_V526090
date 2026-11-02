import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';

@Component({
    selector: 'app-export-excel',
    imports: [CommonModule, FormsModule, Textarea, ButtonModule, Select, ToggleSwitch],
    templateUrl: './export-excel.html',
    styleUrl: './export-excel.scss'
})
export class ExportExcel implements OnInit {
    notes = 'sale-detail-revenue-report';
    type = 'xlsx';
    format = true;
    dialogRef: DynamicDialogRef = inject(DynamicDialogRef);
    protected config = inject(DynamicDialogConfig);
    exportTypeOptions = [
        { Name: 'xlsx', Code: 'xlsx' },
        { Name: 'xls', Code: 'xls' },
        { Name: 'txt', Code: 'txt' }
    ];
    ngOnInit(): void {
        if (this.config?.data) {
            this.notes = this.config.data.fileName || this.notes;
            this.type = this.config.data.type || this.type;
            this.format = this.config.data.format !== undefined ? this.config.data.format : this.format;
        }
    }
    print() {
        var model = {
            Notes: this.notes,
            Type: this.type,
            Format: this.format
        };
        this.dialogRef?.close(model);
    }
    close() {
        this.dialogRef?.close();
    }
}
