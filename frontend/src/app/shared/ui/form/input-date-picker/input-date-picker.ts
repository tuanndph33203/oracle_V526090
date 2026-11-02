import { CommonModule } from '@angular/common';
import { Component, forwardRef, input, ViewChild } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';

@Component({
    selector: 'app-input-date-picker',
    standalone: true,
    imports: [DatePicker, FormsModule, CommonModule],
    templateUrl: './input-date-picker.html',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => InputDatePicker),
            multi: true
        }
    ]
})
export class InputDatePicker implements ControlValueAccessor {
    rangeDates: [Date | null, Date | null] = [null, null];
    showClear = input<boolean>(true);

    @ViewChild('startPicker') startPicker!: DatePicker;
    @ViewChild('endPicker') endPicker!: DatePicker;

    private onChange: (value: [Date | null, Date | null]) => void = () => {};
    private onTouched: () => void = () => {};

    writeValue(value: [Date | null, Date | null] | null): void {
        this.rangeDates = value ?? [null, null];
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    onStartChange(date: Date | null) {
        if (!date) return;

        const end = this.rangeDates[1];
        this.rangeDates = [date, end];

        this.onChange(this.rangeDates);
        this.onTouched();

        this.endPicker.showOverlay();
    }

    onEndChange(date: Date | null) {
        if (!date) return;

        const start = this.rangeDates[0];
        this.rangeDates = [start, date];

        this.onChange(this.rangeDates);
        this.onTouched();
    }

    toDate(d: any): Date {
        return new Date(d.year, d.month, d.day);
    }

    isSameDay(a: Date, b: Date | null): boolean {
        if (!b) return false;
        return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }

    isStartOrEnd(d: any): boolean {
        const current = this.toDate(d);
        const [start, end] = this.rangeDates;

        return !!((start && this.isSameDay(current, start)) || (end && this.isSameDay(current, end)));
    }

    isInRange(d: any): boolean {
        const [start, end] = this.rangeDates;
        if (!start || !end) return false;

        const current = this.toDate(d);
        return current > start && current < end;
    }
    onClear(index: number) {
        this.rangeDates = index === 0 ? [this.rangeDates[0], null] : [null, this.rangeDates[1]];
        this.onChange(this.rangeDates);
        console.log(this.rangeDates);

        this.onTouched();
    }
}
