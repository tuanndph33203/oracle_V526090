import { Injectable, effect, signal, computed, inject } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { LAYOUT_CONFIG_KEY } from '../constants/layout';
import { IUser } from '../models/auth';
import { selectUser } from '@/store/auth/auth.selectors';
import { select, Store } from '@ngrx/store';

export interface layoutConfig {
    preset?: string;
    primary?: string;
    surface?: string | undefined | null;
    darkTheme?: boolean;
    menuMode?: string;
}

interface LayoutState {
    staticMenuDesktopInactive?: boolean;
    overlayMenuActive?: boolean;
    configSidebarVisible?: boolean;
    staticMenuMobileActive?: boolean;
    menuHoverActive?: boolean;
}

interface MenuChangeEvent {
    key: string;
    routeEvent?: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class LayoutService {
    _config: layoutConfig = {
        preset: 'Aura',
        primary: 'brand',
        surface: null,
        darkTheme: false,
        menuMode: 'overlay'
    };

    _state: LayoutState = {
        staticMenuDesktopInactive: false,
        overlayMenuActive: false,
        configSidebarVisible: false,
        staticMenuMobileActive: false,
        menuHoverActive: false
    };

    layoutConfig = signal<layoutConfig>(this._config);

    layoutState = signal<LayoutState>(this._state);

    posViewMode = signal<'floor-plan' | 'order-list'>('floor-plan');
    showPosSwitcher = signal<boolean>(false);

    setPosViewMode(mode: 'floor-plan' | 'order-list') {
        this.posViewMode.set(mode);
    }

    configReady = signal<boolean>(false);
    viewReady = signal<boolean>(false);

    private configUpdate = new Subject<layoutConfig>();

    private overlayOpen = new Subject<any>();

    private menuSource = new Subject<MenuChangeEvent>();

    private resetSource = new Subject();

    private destroy = new Subject<void>();

    private store = inject(Store);

    user = signal<IUser | null>(null);

    menuSource$ = this.menuSource.asObservable();

    resetSource$ = this.resetSource.asObservable();

    configUpdate$ = this.configUpdate.asObservable();

    overlayOpen$ = this.overlayOpen.asObservable();

    theme = computed(() => (this.layoutConfig()?.darkTheme ? 'light' : 'dark'));

    isSidebarActive = computed(() => this.layoutState().overlayMenuActive || this.layoutState().staticMenuMobileActive);

    isDarkTheme = computed(() => this.layoutConfig().darkTheme);

    getPrimary = computed(() => this.layoutConfig().primary);

    getSurface = computed(() => this.layoutConfig().surface);

    isOverlay = computed(() => this.layoutConfig().menuMode === 'overlay');

    loadingState = computed(() => !(this.viewReady() && this.configReady() && this.user()));

    transitionComplete = signal<boolean>(false);

    private initialized = false;

    constructor() {
        const savedConfig = this.getConfigFromStorage();
        if (savedConfig) {
            this.layoutConfig.set({
                ...this._config,
                ...savedConfig
            });
        }

        this.configReady.set(true);

        effect(() => {
            const config = this.layoutConfig();
            if (config) {
                this.onConfigUpdate();
                this.saveConfigToStorage(config);
            }
        });

        effect(() => {
            const config = this.layoutConfig();

            if (!this.initialized || !config) {
                this.initialized = true;
                return;
            }

            this.handleDarkModeTransition(config);
        });

        this.store.pipe(select(selectUser), takeUntil(this.destroy)).subscribe((user) => {
            this.user.set(user);
        });
    }

    private handleDarkModeTransition(config: layoutConfig): void {
        if ((document as any).startViewTransition) {
            this.startViewTransition(config);
        } else {
            this.toggleDarkMode(config);
            this.onTransitionEnd();
        }
    }

    private startViewTransition(config: layoutConfig): void {
        const transition = (document as any).startViewTransition(() => {
            this.toggleDarkMode(config);
        });

        transition.ready
            .then(() => {
                this.onTransitionEnd();
            })
            .catch(() => {});
    }

    private saveConfigToStorage(config: layoutConfig): void {
        try {
            localStorage.setItem(LAYOUT_CONFIG_KEY, JSON.stringify(config));
        } catch (e) {
            console.warn('Cannot save layout config', e);
        }
    }

    private getConfigFromStorage(): layoutConfig | null {
        try {
            const raw = localStorage.getItem(LAYOUT_CONFIG_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    }

    toggleDarkMode(config?: layoutConfig): void {
        const _config = config || this.layoutConfig();
        if (_config.darkTheme) {
            document.documentElement.classList.add('app-dark');
        } else {
            document.documentElement.classList.remove('app-dark');
        }
    }

    private onTransitionEnd() {
        this.transitionComplete.set(true);
        setTimeout(() => {
            this.transitionComplete.set(false);
        });
    }

    onMenuToggle() {
        if (this.isOverlay()) {
            this.layoutState.update((prev) => ({ ...prev, overlayMenuActive: !this.layoutState().overlayMenuActive }));

            if (this.layoutState().overlayMenuActive) {
                this.overlayOpen.next(null);
            }
        }

        if (this.isDesktop()) {
            this.layoutState.update((prev) => ({ ...prev, staticMenuDesktopInactive: !this.layoutState().staticMenuDesktopInactive }));
        } else {
            this.layoutState.update((prev) => ({ ...prev, staticMenuMobileActive: !this.layoutState().staticMenuMobileActive }));

            if (this.layoutState().staticMenuMobileActive) {
                this.overlayOpen.next(null);
            }
        }
    }

    isDesktop() {
        return window.innerWidth > 991;
    }

    isMobile() {
        return !this.isDesktop();
    }

    expandDesktopSidebar() {
        this.layoutState.update((prev) => ({
            ...prev,
            staticMenuDesktopInactive: false
        }));
    }

    onConfigUpdate() {
        this._config = { ...this.layoutConfig() };
        this.configUpdate.next(this.layoutConfig());
    }

    onMenuStateChange(event: MenuChangeEvent) {
        this.menuSource.next(event);
    }

    reset() {
        this.resetSource.next(true);
    }
}
