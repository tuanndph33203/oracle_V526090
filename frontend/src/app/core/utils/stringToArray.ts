export function stringToArray(value?: string): string[] {
    if (!value || typeof value !== 'string') return [];
    return value.split(',').map((v) => v.trim());
}
