import { Component, input, output, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Popover } from 'primeng/popover';
import { Checkbox } from 'primeng/checkbox';
import { Button } from 'primeng/button';
import { ITableConfig } from '@/core/models/common';
import { Tooltip } from 'primeng/tooltip';

@Component({
    selector: 'app-column-toggle',
    imports: [Popover, Checkbox, FormsModule, Tooltip, Button],
    templateUrl: './column-toggle.html',
    styleUrl: './column-toggle.scss'
})
export class ColumnToggle {
    columns = input.required<ITableConfig[]>();

    change = output<string[]>();

    model: Record<string, boolean> = {};
    defaultModel: Record<string, boolean> = {};

    ngOnInit() {
        this.columns().forEach((col) => {
            const checked = col.visible === true;
            this.model[col.field] = checked;
            this.defaultModel[col.field] = checked;
        });
    }
    apply(op: Popover) {
        const visibleFields = Object.entries(this.model)
            .filter(([_, v]) => v)
            .map(([k]) => k);

        console.log(visibleFields);

        this.change.emit(visibleFields);
        op.hide();
    }
    selectAll(): void {
        this.columns().forEach((col) => {
            this.model[col.field] = true;
        });
    }

    resetDefault(): void {
        this.model = { ...this.defaultModel };
    }
}
