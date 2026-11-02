export function buildOptionsFromEnum<T extends string | number>(record: Record<T, string>): { label: string; value: T }[] {
    return Object.entries(record).map(([value, label]) => ({
        label: label as string,
        value: isNaN(Number(value)) ? (value as T) : (Number(value) as T)
    }));
}
