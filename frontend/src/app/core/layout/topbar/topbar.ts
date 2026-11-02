import { Component, effect, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StyleClassModule } from 'primeng/styleclass';
import { ButtonModule } from 'primeng/button';
import { Menubar } from 'primeng/menubar';
import { LayoutService } from '@/core/services/layout';
import { Tooltip } from 'primeng/tooltip';
import { filter, Subject, takeUntil } from 'rxjs';
import { AppMenuItem } from '@/core/models/common';
import { FloatingConfigurator } from '../floating-configurator/floating-configurator';
import { IUser } from '@/core/models/auth';
import { MenuService } from '@/core/services/menu';
import { Skeleton } from 'primeng/skeleton';
import { PopoverModule } from 'primeng/popover';
import { Auth } from '@/core/services/auth';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { actionUser } from '@/core/constants/layout';
import { Badge } from 'primeng/badge';
import { select, Store } from '@ngrx/store';
import AuthAction from '@/store/auth/auth.actions';
import { FormsModule } from '@angular/forms';
import { Listbox, ListboxSelectAllChangeEvent } from 'primeng/listbox';
import { selectAvailableBranchCodes, selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import BranchFilterAction from '@/store/branch-filter/branch-filter.actions';
import { selectUser } from '@/store/auth/auth.selectors';
import { OverlayBadge } from 'primeng/overlaybadge';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { environment } from 'src/environments/environment';
import { DialogService } from 'primeng/dynamicdialog';
import { QrCode } from '../qr-code/qr-code';
import { PaymentRealtimeService } from '@/core/services/payment-realtime';
import { HubConnectionState } from '@microsoft/signalr';
@Component({
    selector: 'app-topbar',
    imports: [FormsModule, RouterModule, CommonModule, StyleClassModule, ButtonModule, Menubar, Tooltip, FloatingConfigurator, Skeleton, PopoverModule, Menu, Badge, Listbox, OverlayBadge, HasPermissionDirective],
    templateUrl: './topbar.html',
    styleUrl: './topbar.scss'
})
export class Topbar implements OnInit {
    name = environment.name;
    @ViewChild('menu') menubar!: Menubar;
    listboxPt = {
        root: {
            class: 'branch-listbox-root'
        }
    };

    branchOptions = signal<any[]>([]);
    availableBranchCodes: any[] = [];
    selectedBranchCodes: string[] = [];
    branchUser: any[] = [];
    selectAll: boolean = true;
    user = signal<IUser | null>(null);
    searchQuery = signal<any>({
        TextSearch: ''
    });

    itemsAction = signal<MenuItem[]>([]);

    store = inject(Store);
    authService = inject(Auth);
    router = inject(Router);
    layoutService = inject(LayoutService);
    route = inject(ActivatedRoute);
    menuService = inject(MenuService);
    dialogService = inject(DialogService);
    paymentRealtimeService = inject(PaymentRealtimeService);
    private destroy = new Subject<void>();

    readonly notifications = this.paymentRealtimeService.notifications;
    readonly unreadCount = this.paymentRealtimeService.unreadCount;
    readonly realtimeState = this.paymentRealtimeService.connectionState;
    readonly branchReady = this.paymentRealtimeService.branchReady;
    readonly audioReady = this.paymentRealtimeService.audioReady;
    readonly connectedState = HubConnectionState.Connected;
    bellTestMessage = signal('');

    async testBell(): Promise<void> {
        const ready = await this.paymentRealtimeService.testNotificationSound();
        this.bellTestMessage.set(ready ? 'Chuông đã phát' : 'Trình duyệt chưa cho phép âm thanh');
    }

    readonly menuItems = this.menuService.menu;
    constructor() {
        this.router.events
            .pipe(
                filter((e) => e instanceof NavigationEnd),
                takeUntil(this.destroy)
            )
            .subscribe(() => {
                const menu = this.menuItems();
                if (menu.length) {
                    this.markActiveMenu(menu);
                }
            });
        effect(() => {
            const menu = this.menuItems();
            if (menu.length) {
                this.markActiveMenu(menu);
            }
        });
    }

    ngOnInit(): void {
        this.store.pipe(select(selectAvailableBranchCodes), takeUntil(this.destroy)).subscribe((options) => {
            if (!options.length) return;
            this.branchOptions.set(options);
            this.applyBranch();
        });

        this.store.pipe(select(selectSelectedBranchCodes), takeUntil(this.destroy)).subscribe(async (selectedCodes) => {
            this.selectedBranchCodes = selectedCodes.map((code: any) => code.Code) ?? [];
            this.applyBranch();

            if (this.selectedBranchCodes.length > 0) {
                try {
                    await this.paymentRealtimeService.updateBranchSubscription(this.selectedBranchCodes);
                    await this.paymentRealtimeService.loadInitialNotifications(this.selectedBranchCodes);
                } catch (err) {
                    console.error('Error initializing payment notification connection', err);
                }
            } else {
                await this.paymentRealtimeService.updateBranchSubscription([]);
            }
        });
        this.store.pipe(select(selectUser), takeUntil(this.destroy)).subscribe((user) => {
            this.user.set(user);
        });
        this.itemsAction.set(
            actionUser.map((item) => ({
                ...item,
                command: () => this.handleAction(item.action)
            }))
        );
    }

    applyBranch() {
        const selectedCodes = this.selectedBranchCodes ?? [];
        this.branchUser = this.branchOptions().filter((b: any) => selectedCodes.includes(b.Code));
    }

    applyFilter() {
        const branchCodes = this.branchUser ?? [];
        if (!branchCodes.length) return;
        this.store.dispatch(BranchFilterAction.setSelectedBranches({ codes: branchCodes }));
    }

    onChange() {
        this.applyFilter();
    }

    onSelectAllChange(event: ListboxSelectAllChangeEvent) {
        this.branchUser = event.checked ? [...this.branchOptions()] : [this.branchOptions()[0]];
        this.selectAll = event.checked;
        this.applyFilter();
    }

    handleAction(action?: string) {
        switch (action) {
            case 'profile':
                this.router.navigate(['/user/profile']);
                break;

            case 'shift-session':
                this.router.navigate(['/catalogs/shift-session']);
                break;

            case 'system':
                this.router.navigate(['/systems/settings']);
                break;

            case 'logs':
                this.router.navigate(['/logs']);
                break;

            default:
                break;
        }
    }

    logout() {
        this.store.dispatch(AuthAction.logout());
    }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }

    navLink(link: string) {
        this.router.navigate([link]);
    }
    isRouteActive(item: AppMenuItem): boolean {
        if (!item.routerLink) return false;
        const tree = this.router.createUrlTree(Array.isArray(item.routerLink) ? item.routerLink : [item.routerLink]);
        return this.router.isActive(tree, {
            paths: 'exact',
            queryParams: 'ignored',
            fragment: 'ignored',
            matrixParams: 'ignored'
        });
    }

    markActiveMenu(items: AppMenuItem[], level = 0): boolean {
        let hasActiveInBranch = false;
        for (const item of items) {
            item._active = false;
            item._hasActiveDescendant = false;
            item._rootActive = false;
            if (this.isRouteActive(item)) {
                item._active = true;
                hasActiveInBranch = true;
            }
            if (item.items?.length) {
                const childHasActive = this.markActiveMenu(item.items as AppMenuItem[], level + 1);
                if (childHasActive) {
                    item._hasActiveDescendant = true;
                    if (level === 0) {
                        item._rootActive = true;
                    }

                    hasActiveInBranch = true;
                }
            }
        }
        return hasActiveInBranch;
    }

    openOnHover(event: MouseEvent, item: any, root: boolean) {
        if (!root || !item.items?.length) return;
        const processedItem = this.menubar.processedItems?.find((p: any) => p.item === item);
        event.stopPropagation();
        this.menubar.onItemClick({
            originalEvent: event,
            processedItem
        });
    }

    generateQRCode() {
        this.dialogService.open(QrCode, {
            header: 'Thanh toán nhanh',
            width: '400px',
            styleClass: 'p-fluid',
            closable: true
        });
    }

    markAsRead(notification: any) {
        if (notification && !notification.isRead) {
            this.paymentRealtimeService.markNotificationAsRead(notification.id);
        }
    }

    markAllAsRead() {
        if (this.selectedBranchCodes.length > 0) {
            this.paymentRealtimeService.markAllNotificationsAsRead(this.selectedBranchCodes);
        }
    }
    ngOnDestroy(): void {
        this.destroy.next();
        this.destroy.complete();
    }
}
