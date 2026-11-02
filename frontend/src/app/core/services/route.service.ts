import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class RouteService {
    private urlCurrent = '';
    private readonly STORAGE_KEY = 'url';

    saveUrlCurrent(url: string) {
        this.urlCurrent = url;
    }
    getUrlCurrent() {
        return this.urlCurrent;
    }
    saveUrl(url: string) {
        localStorage.setItem(this.STORAGE_KEY, url);
    }
    loadUrl(): string | null {
        const urlString = localStorage.getItem(this.STORAGE_KEY);
        this.remove();
        return urlString ? urlString : null;
    }
    remove() {
        this.urlCurrent = '';
        localStorage.removeItem(this.STORAGE_KEY);
    }
}
