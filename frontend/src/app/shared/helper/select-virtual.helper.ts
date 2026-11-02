import { WritableSignal } from '@angular/core';
export const EMPTY_OPTION = { __empty: true };
export function onLazyloadSelect(event: { first: number; last: number }, options: WritableSignal<any[]>, searchQuery: WritableSignal<any>, loadFn: (query: any) => void) {
    const { first = 0, last = 0 } = event;
    const items = options();

    let needLoad = false;
    for (let i = first; i < last; i++) {
        if (items[i]?.__empty) {
            needLoad = true;
            break;
        }
    }

    if (!needLoad) return;

    const page = Math.ceil(last / searchQuery().size);

    searchQuery.update((q) => ({
        ...q,
        page
    }));

    loadFn(searchQuery());
}
