export function formatDatetoHours(value: Date | string | null): string | null {
    if (!value) return null;
    if (typeof value === 'string') return value;

    const hh = value.getHours().toString().padStart(2, '0');
    const mm = value.getMinutes().toString().padStart(2, '0');
    return `${hh}:${mm}`;
}
