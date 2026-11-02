export function parseHoursToDate(value: string | Date | null, dateTime?: Date): Date | null {
    if (!value) return null;
    if (value instanceof Date) return value;
    const parts = value.split(':').map(Number);
    if (parts.length < 2) return null;
    const [hours, minutes, seconds = 0] = parts;
    const date = dateTime ? new Date(dateTime) : new Date();
    date.setHours(hours, minutes, seconds, 0);
    return date;
}
