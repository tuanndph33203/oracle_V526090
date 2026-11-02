export const roundBank = (value: number, decimals = 0 ) : number => {
    const factor = Math.pow(10, decimals);
    const n = value * factor;
    const fractional = n - Math.trunc(n);
    if (Math.abs(fractional) === 0.5) {
        return (Math.trunc(n) % 2 === 0 ? Math.trunc(n) : Math.trunc(n) + (n > 0 ? 1 : -1)) / factor;
    }
    return Math.round(n) / factor;
};
