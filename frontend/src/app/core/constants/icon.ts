import { PrimeIcons } from 'primeng/api';
import { PrimeIconItem } from '../models/common';
import { phosphorIconNames } from './phosphor-icons';

// ─── Title case helper for clean names ─────────────────────────────────────────
function toTitleCase(str: string): string {
    return str
        .split('-')
        .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ');
}

// ─── PrimeIcons ──────────────────────────────────────────────────────────────
export const primeIcons: PrimeIconItem[] = Object.entries(PrimeIcons).map(([key, value]) => ({
    key: toTitleCase(key.toLowerCase().replace(/_/g, '-')),
    class: value as string,
    library: 'prime' as const
}));

// ─── Phosphor Icons (Regular default, Bold & Fill included) ─────────────────
export const phosphorIcons: PrimeIconItem[] = [
    // Regular icons (clean Title Case names)
    ...phosphorIconNames.map((name) => ({
        key: toTitleCase(name),
        class: `ph ph-${name}`,
        library: 'phosphor' as const
    })),
    // Bold variants
    ...phosphorIconNames.map((name) => ({
        key: `${toTitleCase(name)} (Bold)`,
        class: `ph-bold ph-${name}`,
        library: 'phosphor' as const
    })),
    // Fill variants
    ...phosphorIconNames.map((name) => ({
        key: `${toTitleCase(name)} (Fill)`,
        class: `ph-fill ph-${name}`,
        library: 'phosphor' as const
    }))
];

// ─── Unified list (Phosphor first for modern POS icons, then Prime) ───────────
export const allIcons: PrimeIconItem[] = [...phosphorIcons, ...primeIcons];

// ─── Filter helpers ───────────────────────────────────────────────────────────
export function filterPrimeIcons(query: string): PrimeIconItem[] {
    const q = query.toLowerCase();
    return primeIcons.filter((icon) => icon.key.toLowerCase().includes(q));
}

export function filterAllIcons(query: string): PrimeIconItem[] {
    if (!query || query.trim() === '') {
        // Return clean regular icons on empty search
        return phosphorIcons.slice(0, 100);
    }

    const cleanQuery = query
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[-_]/g, ' ')
        .trim();

    return allIcons
        .filter((icon) => {
            const keyNorm = icon.key.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[-_]/g, ' ');
            const classNorm = icon.class.toLowerCase().replace(/[-_]/g, ' ');
            return keyNorm.includes(cleanQuery) || classNorm.includes(cleanQuery);
        })
        .slice(0, 100);
}
