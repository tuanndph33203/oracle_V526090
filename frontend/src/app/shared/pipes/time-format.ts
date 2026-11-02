import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'timeFormat'
})
export class TimeFormatPipe implements PipeTransform {
    transform(value: string | null | undefined): string {
        if (!value) return '';

        const parts = value.split(':');
        if (parts.length >= 2) {
            return `${parts[0]}:${parts[1]}`;
        }

        return value;
    }
}
