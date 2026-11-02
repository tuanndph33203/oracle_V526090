import { Component, inject, OnInit, Renderer2, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { Topbar } from '../topbar/topbar';
import { Sidebar } from '../sidebar/sidebar';
import { Loading } from '../loading/loading';
import { LayoutService } from '@/core/services/layout';
import { Footer } from '../footer/footer';
import { MenuService } from '@/core/services/menu';
import { Store } from '@ngrx/store';
import AuthAction from '@/store/auth/auth.actions';
import SystemSettingsAction from '@/store/system-settings/system-settings.action';
import { MenuItem } from 'primeng/api';

@Component({
    selector: 'app-layout',
    imports: [CommonModule, Topbar, Sidebar, RouterModule, Loading, Footer],
    templateUrl: './layout.html',
    styleUrl: './layout.scss'
})
export class Layout implements OnInit {
    overlayMenuOpenSubscription: Subscription;
    menuOutsideClickListener: any;
    hideBreadcrumb = false;

    @ViewChild(Sidebar) appSidebar!: Sidebar;

    @ViewChild(Topbar) appTopBar!: Topbar;

    private menuService = inject(MenuService);
    layoutService = inject(LayoutService);
    private renderer = inject(Renderer2);
    private router = inject(Router);
    private store = inject(Store);
    private activatedRoute = inject(ActivatedRoute);

    readonly breadcrumbItems = this.menuService.breadcrumb;
    readonly currentPageTitle = this.menuService.currentPageTitle;
    home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };

    constructor() {
        this.overlayMenuOpenSubscription = this.layoutService.overlayOpen$.subscribe(() => {
            if (!this.menuOutsideClickListener) {
                this.menuOutsideClickListener = this.renderer.listen('document', 'click', (event) => {
                    if (this.isOutsideClicked(event)) {
                        this.hideMenu();
                    }
                });
            }
            if (this.layoutService.layoutState().staticMenuMobileActive) {
                this.blockBodyScroll();
            }
        });

        this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
            this.store.dispatch(SystemSettingsAction.loadAll());
            this.hideMenu();

            let route = this.activatedRoute;
            while (route.firstChild) {
                route = route.firstChild;
            }
            this.hideBreadcrumb = route.snapshot.data['hideBreadcrumb'] ?? false;
        });
    }

    ngOnInit(): void {
        this.store.dispatch(AuthAction.requestUser());
        if (this.menuService.menu().length === 0) {
            this.menuService.loadMenu().subscribe();
        }
    }

    isOutsideClicked(event: MouseEvent) {
        const sidebarEl = document.querySelector('.layout-sidebar');
        const topbarEl = document.querySelector('.layout-menu-button');
        const eventTarget = event.target as Node;

        const isSidebar = sidebarEl?.isSameNode(eventTarget) || sidebarEl?.contains(eventTarget);
        const isTopbar = topbarEl?.isSameNode(eventTarget) || topbarEl?.contains(eventTarget);

        const popovers = Array.from(document.querySelectorAll('.p-popover, .p-overlaypanel, .p-listbox-overlay'));
        const isPopover = popovers.some((popover) => popover.isSameNode(eventTarget) || popover.contains(eventTarget));

        return !(isSidebar || isTopbar || isPopover);
    }

    hideMenu() {
        if (this.appSidebar?.opSidebar) {
            this.appSidebar.opSidebar.hide();
        }
        this.layoutService.layoutState.update((prev) => ({ ...prev, overlayMenuActive: false, staticMenuMobileActive: false, menuHoverActive: false }));
        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
            this.menuOutsideClickListener = null;
        }
        this.unblockBodyScroll();
    }

    blockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.add('blocked-scroll');
        } else {
            document.body.className += ' blocked-scroll';
        }
    }

    unblockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.remove('blocked-scroll');
        } else {
            document.body.className = document.body.className.replace(new RegExp('(^|\\b)' + 'blocked-scroll'.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        }
    }

    get containerClass() {
        return {
            'layout-overlay': this.layoutService.layoutConfig().menuMode === 'overlay',
            'layout-static': this.layoutService.layoutConfig().menuMode === 'static',
            'layout-static-inactive': this.layoutService.layoutState().staticMenuDesktopInactive && this.layoutService.layoutConfig().menuMode === 'static',
            'layout-overlay-active': this.layoutService.layoutState().overlayMenuActive,
            'layout-mobile-active': this.layoutService.layoutState().staticMenuMobileActive
        };
    }

    ngOnDestroy() {
        if (this.overlayMenuOpenSubscription) {
            this.overlayMenuOpenSubscription.unsubscribe();
        }

        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
        }
    }
}
