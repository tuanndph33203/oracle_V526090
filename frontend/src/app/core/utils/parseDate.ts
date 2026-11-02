export function parseDate(d?: string | null): Date | null {
    if (!d) return null;
    if (/^\d{4}-\d{2}-\d{2}$/.test(d)) {
        const [y, m, day] = d.split('-').map(Number);
        return new Date(y, m - 1, day);
    }
    if (d.endsWith('Z')) {
        const utc = new Date(d);
        return new Date(
            utc.getUTCFullYear(),
            utc.getUTCMonth(),
            utc.getUTCDate()
        );
    }

    const date = new Date(d);
    return isNaN(date.getTime()) ? null : date;
}
