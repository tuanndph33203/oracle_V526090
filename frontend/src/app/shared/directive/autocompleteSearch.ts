import { Directive, HostListener } from '@angular/core';
import { AutoComplete } from 'primeng/autocomplete';

@Directive({
    selector: '[openOnClick]'
})
export class AutoCompleteSearchDirective {
    constructor(private ac: AutoComplete) {}

    @HostListener('click')
    @HostListener('focus')
    open() {
        if (this.ac.disabled?.() === true || this.ac.readonly === true) return;
        this.ac.search(null, '', 'click');
    }
}
