export function arrayToString(value?: Array<string | number | null | undefined>): string {
    return (value ?? []).filter((v) => v !== null && v !== undefined && v !== '').join(',');
}
