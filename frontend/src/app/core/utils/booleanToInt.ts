export function mapBooleanToInt<
    T extends Record<string, any>,
    K extends {
        [P in keyof T]: T[P] extends boolean ? P : never;
    }[keyof T]
>(data: T, fields: K[]): Omit<T, K> & Record<K, number> {
    const result = { ...data } as any;

    fields.forEach((field) => {
        result[field] = data[field] ? 1 : 0;
    });

    return result;
}
