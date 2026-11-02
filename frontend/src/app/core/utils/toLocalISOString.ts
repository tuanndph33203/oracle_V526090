export function toLocalISOString(
    date?: Date | string | null
): string | undefined {
    if (!date) return undefined;

    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return undefined;

    const offset = d.getTimezoneOffset() * 60000;
    return new Date(d.getTime() - offset).toISOString();
}