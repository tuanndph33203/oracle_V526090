import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'parseDate',
    standalone: true,
    pure: true
})
export class ParseDatePipe implements PipeTransform {
    transform(value: string | null | undefined): Date | null {
        if (!value) return null;

        let normalized = value.trim();
        normalized = normalized.replace(/\.(\d{3})\d+/, '.$1')
        const isoDate = new Date(normalized);
        if (!isNaN(isoDate.getTime())) {
            return isoDate;
        }
        const parts = normalized.split(' ');
        if (parts.length !== 2) return null;
        const [time, date] = parts;
        const [hour, minute] = time.split(':').map(Number);
        const [day, month, year] = date.split('/').map(Number);

        if ([hour, minute, day, month, year].some(isNaN)) return null;

        return new Date(year, month - 1, day, hour, minute);
    }
}
