import { Component, HostBinding, Input } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { RippleModule } from 'primeng/ripple';
import { LayoutService } from '../../services/layout';

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: '[app-menuitem]',
    imports: [CommonModule, RouterModule, RippleModule],
    template: `
        <ng-container>
            <div *ngIf="root && item.visible !== false"
                class="layout-menuitem-root-text"
                [class.active-route]="isRootActive"
                [class.menu-expanded]="active"
                (click)="toggleRootMenu($event)"
                [attr.title]="item.lable">
                <span class="menuitem-icon-wrapper">
                    <i [ngClass]="item.icon" class="layout-menuitem-icon"></i>
                </span>
                <span class="menuitem-label">{{ item.lable }}</span>
                @if (item.items?.length) {
                    <i class="pi pi-fw pi-angle-down layout-submenu-toggler" [class.rotated]="active"></i>
                }
            </div>
            <a *ngIf="(!item.routerLink || item.items?.length > 0) && item.visible !== false" [attr.href]="item.url" (click)="itemClick($event)" [ngClass]="item.styleClass" [attr.target]="item.target" tabindex="0" pRipple [attr.title]="item.lable">
                <span class="menuitem-icon-wrapper sm">
                    <i [ngClass]="item.icon" class="layout-menuitem-icon"></i>
                </span>
                <span class="layout-menuitem-text">{{ item.lable }}</span>
                @if (item.items?.length) {
                    <i class="pi pi-fw pi-angle-down layout-submenu-toggler"></i>
                }
            </a>
            <a
                *ngIf="item.routerLink && !item.items?.length && item.visible !== false"
                (click)="itemClick($event)"
                [ngClass]="item.styleClass"
                [routerLink]="item.routerLink"
                routerLinkActive="active-route"
                [routerLinkActiveOptions]="item.routerLinkActiveOptions || { paths: 'exact', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' }"
                [fragment]="item.fragment"
                [queryParamsHandling]="item.queryParamsHandling"
                [preserveFragment]="item.preserveFragment"
                [skipLocationChange]="item.skipLocationChange"
                [replaceUrl]="item.replaceUrl"
                [state]="item.state"
                [queryParams]="item.queryParams"
                [attr.target]="item.target"
                tabindex="0"
                pRipple
                [attr.title]="item.lable"
            >
                <span class="menuitem-icon-wrapper sm">
                    <i [ngClass]="item.icon" class="layout-menuitem-icon"></i>
                </span>
                <span class="layout-menuitem-text">{{ item.lable }}</span>
                @if (item.items?.length) {
                    <i class="pi pi-fw pi-angle-down layout-submenu-toggler"></i>
                }
            </a>

            @if (item.items?.length) {
                <ul [@children]="submenuAnimation" class="submenu-list">
                    <ng-template ngFor let-child let-i="index" [ngForOf]="item.items">
                        <li app-menuitem [item]="child" [index]="i" [parentKey]="key" [class]="child['badgeClass']"></li>
                    </ng-template>
                </ul>
            }
        </ng-container>
    `,
    animations: [
        trigger('children', [
            state(
                'collapsed',
                style({
                    height: '0',
                    overflow: 'hidden'
                })
            ),
            state(
                'expanded',
                style({
                    height: '*',
                    overflow: 'hidden'
                })
            ),
            transition('collapsed <=> expanded', animate('400ms cubic-bezier(0.86, 0, 0.07, 1)'))
        ])
    ]
})
export class AppMenuitem {
    @Input() item!: any;

    @Input() index!: number;

    @Input() @HostBinding('class.layout-root-menuitem') root!: boolean;

    @Input() parentKey!: string;

    active = false;

    menuSourceSubscription: Subscription;

    menuResetSubscription: Subscription;

    key: string = '';

    constructor(
        public router: Router,
        private layoutService: LayoutService
    ) {
        this.menuSourceSubscription = this.layoutService.menuSource$.subscribe((value) => {
            Promise.resolve(null).then(() => {
                if (value.routeEvent) {
                    this.active = value.key === this.key || value.key.startsWith(this.key + '-') ? true : false;
                } else {
                    if (value.key !== this.key && !value.key.startsWith(this.key + '-')) {
                        this.active = false;
                    }
                }
            });
        });

        this.menuResetSubscription = this.layoutService.resetSource$.subscribe(() => {
            this.active = false;
        });

        this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
            if (this.item.routerLink) {
                this.updateActiveStateFromRoute();
            }
        });
    }

    ngOnInit() {
        this.key = this.parentKey ? this.parentKey + '-' + this.index : String(this.index);

        // Chỉ mở menu nào chứa route hiện tại, không bung tất cả
        if (this.item.routerLink) {
            this.updateActiveStateFromRoute();
        }
    }

    updateActiveStateFromRoute() {
        const route = this.resolveRouteLink(this.item.routerLink);
        if (!route) {
            return;
        }

        const activeRoute = this.router.isActive(route, { paths: 'exact', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' });

        if (activeRoute) {
            this.layoutService.onMenuStateChange({ key: this.key, routeEvent: true });
        }
    }

    itemClick(event: Event) {
        if (this.item.disabled) {
            event.preventDefault();
            return;
        }

        if (this.item.command) {
            this.item.command({ originalEvent: event, item: this.item });
        }

        if (this.item.items) {
            this.active = !this.active;
        }

        this.layoutService.onMenuStateChange({ key: this.key });
    }

    get isRootActive(): boolean {
        if (!this.root) return false;
        if (this.item?.routerLink) {
            const route = this.resolveRouteLink(this.item.routerLink);
            return route ? this.router.isActive(route, { paths: 'exact', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' }) : false;
        }
        if (this.item?.items?.length) {
            return this.item.items.some((child: any) => {
                if (!child.routerLink) return false;
                const childRoute = this.resolveRouteLink(child.routerLink);
                return childRoute ? this.router.isActive(childRoute, { paths: 'subset', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' }) : false;
            });
        }
        return false;
    }

    toggleRootMenu(event: Event) {
        if (!this.item.items?.length) {
            const route = this.resolveRouteLink(this.item.routerLink);
            if (!route) {
                return;
            }

            event.preventDefault();
            this.router.navigateByUrl(route);
            if (this.layoutService.isOverlay() || this.layoutService.isMobile()) {
                this.layoutService.layoutState.update((prev) => ({ ...prev, overlayMenuActive: false, staticMenuMobileActive: false }));
            }
            return;
        }

        event.preventDefault();

        if (this.layoutService.isDesktop() && this.layoutService.layoutState().staticMenuDesktopInactive) {
            this.layoutService.expandDesktopSidebar();
            this.active = true;
            this.layoutService.onMenuStateChange({ key: this.key });
            return;
        }

        this.active = !this.active;
        this.layoutService.onMenuStateChange({ key: this.key });
    }

    get submenuAnimation() {
        return this.active ? 'expanded' : 'collapsed';
    }

    @HostBinding('class.active-menuitem')
    get activeClass() {
        return this.active;
    }

    private resolveRouteLink(routerLink: unknown): string | null {
        if (Array.isArray(routerLink)) {
            return typeof routerLink[0] === 'string' && routerLink[0].trim() ? routerLink[0] : null;
        }

        if (typeof routerLink === 'string') {
            const route = routerLink.trim();
            return route ? route : null;
        }

        return null;
    }

    ngOnDestroy() {
        if (this.menuSourceSubscription) {
            this.menuSourceSubscription.unsubscribe();
        }

        if (this.menuResetSubscription) {
            this.menuResetSubscription.unsubscribe();
        }
    }
}
