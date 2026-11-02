export function toNumberArray(
  value: string | number | Array<string | number> | null | undefined
): number[] | undefined {
  if (value === null || value === undefined) return undefined;
  if (typeof value === 'string') {
    const result = value
      .split(',')
      .map(v => Number(v.trim()))
      .filter(v => !isNaN(v));

    return result.length ? result : undefined;
  }
  if (Array.isArray(value)) {
    const result = value
      .map(v => Number(v))
      .filter(v => !isNaN(v));

    return result.length ? result : undefined;
  }
  const num = Number(value);
  return isNaN(num) ? undefined : [num];
}