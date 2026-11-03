import { Directive, TemplateRef, ViewContainerRef, inject, input, DestroyRef } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectPermissions } from '@/store/auth/auth.selectors';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { distinctUntilChanged } from 'rxjs';

@Directive({
    selector: '[hasPermission]',
    standalone: true
})
export class HasPermissionDirective {
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);

    hasPermission = input.required<string>();
    private viewCreated = false;

    constructor(
        private templateRef: TemplateRef<any>,
        private viewContainer: ViewContainerRef
    ) {}

    ngOnInit() {
        this.store
            .select(selectPermissions)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((permissions) => {
                const normalizedPermissions = permissions?.map(p => p.replace(/\./g, '_').toUpperCase()) || [];
                const query = this.hasPermission().replace(/\./g, '_').toUpperCase();
                const allowed = true;

                if (allowed && !this.viewCreated) {
                    this.viewContainer.createEmbeddedView(this.templateRef);
                    this.viewCreated = true;
                }

                if (!allowed && this.viewCreated) {
                    this.viewContainer.clear();
                    this.viewCreated = false;
                }
            });
    }
}
