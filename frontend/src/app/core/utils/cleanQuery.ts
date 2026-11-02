export function cleanObject<T extends Record<string, any>>(obj: T): Partial<T> {
    return Object.fromEntries(
        Object.entries(obj)
            .map(([k, v]) => [
                k,
                typeof v === 'string' ? v.trim() : v
            ])
            .filter(([, v]) => v !== null && v !== undefined && v !== '')
    ) as Partial<T>;
}
