import { CommonModule } from '@angular/common';
import { Component, forwardRef, input, OnInit, output } from '@angular/core';
import { AbstractControl, ControlValueAccessor, FormsModule, NG_VALIDATORS, NG_VALUE_ACCESSOR, ValidationErrors, Validator } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';

@Component({
    selector: 'app-input-time-picker',
    standalone: true,
    imports: [DatePicker, FormsModule, CommonModule],
    templateUrl: './input-time-picker.html',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => InputTimePicker),
            multi: true
        },
        {
            provide: NG_VALIDATORS,
            useExisting: forwardRef(() => InputTimePicker),
            multi: true
        }
    ]
})
export class InputTimePicker implements ControlValueAccessor, Validator {
    timeValue: Date | null = null;
    isDisabled = false;
    min = input<number | null | undefined>(undefined);
    max = input<number | null | undefined>(undefined);
    appendTo = input<any>('body');
    hourFormat = input<string>('24');
    stepMinute = input<number>(5);
    placeholder = input<string | null>(null);
    styleClass = input<string>('w-full');
    inputStyleClass = input<string>('');
    readonlyInput = input<boolean>(false);
    showIcon = input<boolean>(false);
    onFocus = output<Event>();
    onBlur = output<Event>();
    onSelect = output<Date>();
    onInput = output<any>();
    onClear = output<void>();
    onTodayClick = output<Date>();
    onClearClick = output<void>();
    onMonthChange = output<any>();
    onYearChange = output<any>();
    onClickOutside = output<any>();
    onShow = output<HTMLElement>();
    onClose = output<void>();

    private validationError: ValidationErrors | null = null;
    private lastTimeValue: Date | null = null;
    private isManualInputChangePending = false;
    private timeChangeSequence = 0;

    private validatorChange?: () => void;
    private onChange: (value: Date | null) => void = () => {};
    private onTouched: () => void = () => {};

    validate(control: AbstractControl): ValidationErrors | null {
        return this.validationError;
    }

    writeValue(value: Date | string | null): void {
        const normalized = this.parseValue(value, this.lastTimeValue ?? undefined);
        this.timeValue = normalized;
        this.lastTimeValue = normalized ? new Date(normalized) : null;
    }

    registerOnValidatorChange(fn: () => void): void {
        this.validatorChange = fn;
    }

    registerOnChange(fn: (value: Date | null) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
    }

    onTimeChange(value: Date | string | null): void {
        if (value === null) {
            this.validationError = null;
            this.validatorChange?.();

            this.timeValue = null;
            this.lastTimeValue = null;
            this.isManualInputChangePending = false;
            this.timeChangeSequence++;

            this.onChange(null);
            this.onTouched();

            return;
        }

        const parsed = this.parseValue(value, this.lastTimeValue ?? this.timeValue ?? undefined);

        if (!parsed) return;

        const sequence = ++this.timeChangeSequence;

        Promise.resolve().then(() => {
            if (sequence !== this.timeChangeSequence) return;

            const isManualInput = this.isManualInputChangePending;
            this.isManualInputChangePending = false;
            const finalValue = this.commitTimeChange(parsed, isManualInput);
            if (finalValue) {
                this.validateRange(finalValue);
            }
        });
    }

    handleSelect(date: Date) {
        this.timeValue = date;
        this.onChange(date);
        this.onSelect.emit(date);
    }

    handleInput(val: any) {
        this.isManualInputChangePending = true;
        this.onInput.emit(val);
    }

    handleClear() {
        this.timeValue = null;
        this.onChange(null);
        this.onClear.emit();
    }

    handleToday(date: Date) {
        this.timeValue = date;
        this.onChange(date);
        this.onTodayClick.emit(date);
    }

    handleClearClick() {
        this.timeValue = null;
        this.onChange(null);
        this.onClearClick.emit();
    }

    handleFocus(e: Event) {
        this.onFocus.emit(e);
    }

    handleBlur(e: Event) {
        this.onTouched(); // quan trọng
        this.onBlur.emit(e);
    }

    handleMonthChange(e: any) {
        this.onMonthChange.emit(e);
    }

    handleYearChange(e: any) {
        this.onYearChange.emit(e);
    }

    handleClickOutside(e: any) {
        this.onClickOutside.emit(e);
    }

    handleShow(e: HTMLElement) {
        this.onShow.emit(e);
    }

    handleClose() {
        this.onClose.emit();
    }

    private parseValue(value: Date | string | null, baseDate?: Date): Date | null {
        if (!value) {
            return null;
        }

        if (value instanceof Date) {
            return Number.isNaN(value.getTime()) ? null : new Date(value);
        }

        const raw = value.trim();
        if (!raw) {
            return null;
        }

        const matched = raw.match(/^(\d{1,2}):(\d{1,2})(?::\d{1,2})?$/);
        if (matched) {
            const hours = Number(matched[1]);
            const minutes = Number(matched[2]);
            if (Number.isNaN(hours) || Number.isNaN(minutes)) {
                return null;
            }

            const date = baseDate ? new Date(baseDate) : new Date();
            date.setHours(hours, minutes, 0, 0);
            return date;
        }

        const parsed = new Date(raw);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    private validateRange(date: Date): void {
        const minMinutes = this.min();
        const maxMinutes = this.max();

        const now = new Date();

        const hasMin = minMinutes != null;
        const hasMax = maxMinutes != null;

        const isOutOfRange = (hasMin && date.getTime() < now.getTime() + minMinutes * 60000) || (hasMax && date.getTime() > now.getTime() + maxMinutes * 60000);

        this.validationError = isOutOfRange ? { rangeTime: true } : null;

        this.validatorChange?.();
    }

    private commitTimeChange(value: Date | string, skipHourWrap: boolean): Date | null {
        let normalized = this.parseValue(value, this.lastTimeValue ?? this.timeValue ?? undefined);
        if (!normalized) return null;

        const hourAdjustment = !skipHourWrap && this.lastTimeValue ? this.getHourAdjustmentByMinuteWrap(this.lastTimeValue, normalized) : 0;

        if (hourAdjustment !== 0) {
            normalized = new Date(normalized.getTime() + hourAdjustment * 3600000);
        }

        // ✅ KHÔNG reset null nữa
        this.timeValue = normalized;

        this.lastTimeValue = new Date(normalized);
        this.onChange(normalized);
        this.onTouched();

        return normalized;
    }

    private getHourAdjustmentByMinuteWrap(previous: Date, current: Date): number {
        const sameDate = previous.getFullYear() === current.getFullYear() && previous.getMonth() === current.getMonth() && previous.getDate() === current.getDate();

        if (!sameDate) return 0;

        if (previous.getHours() !== current.getHours()) return 0;

        const prevMin = previous.getMinutes();
        const currMin = current.getMinutes();

        const step = this.getValidStepMinute();
        if (step === null) return 0;

        const minuteDelta = currMin - prevMin;

        const wrapForward = step - 60; // ví dụ -55
        const wrapBackward = 60 - step; // ví dụ +55

        // ✅ cho phép lệch trong khoảng step
        if (minuteDelta <= wrapForward + step && minuteDelta >= wrapForward - step) {
            return 1;
        }

        if (minuteDelta >= wrapBackward - step && minuteDelta <= wrapBackward + step) {
            return -1;
        }

        return 0;
    }

    private getValidStepMinute(): number | null {
        const step = this.stepMinute();

        if (!Number.isInteger(step) || step <= 0 || step >= 60) {
            return null;
        }

        return step;
    }
}
