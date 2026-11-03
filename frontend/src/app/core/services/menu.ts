import { Injectable, inject, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, map, catchError, of, tap } from 'rxjs';
import { NavigationService } from '@/features/system/navigation/services/navigation';
import { AppMenuItem } from '../models/common';
import { MenuItem } from 'primeng/api';
import { Store } from '@ngrx/store';
import { selectPermissions } from '@/store/auth/auth.selectors';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import { distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class MenuService {
    private navigationService = inject(NavigationService);
    private router = inject(Router);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);

    private _menu = signal<AppMenuItem[]>([]);
    private _rawMenu = signal<AppMenuItem[]>([]);
    private _permissions = signal<string[]>([]);
    private _requireKitchenSend = signal<boolean>(true);
    readonly menu = this._menu.asReadonly();
    readonly currentPageTitle = computed(() => {
        const items = this._breadcrumb();
        return items.length ? items[items.length - 1] : '';
    });

    private _breadcrumb = signal<any[]>([]);
    readonly breadcrumb = this._breadcrumb.asReadonly();

    constructor() {
        this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => this.buildBreadcrumb());
        this.store
            .select(selectPermissions)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((permissions) => {
                this._permissions.set(permissions || []);
                this.applyMenuFilter();
            });
        this.store
            .select(selectSystemSettings)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((configs) => {
                const setting = configs?.find((x) => `${x.Code ?? ''}`.trim().toLowerCase() === 'sale-require-kitchen');
                if (setting) {
                    const val = `${setting.Value ?? ''}`.trim().toLowerCase();
                    const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                    this._requireKitchenSend.set(isTrue && setting.IsActive);
                } else {
                    this._requireKitchenSend.set(true);
                }
                this.applyMenuFilter();
            });
    }

    private readonly defaultCafeMenu: AppMenuItem[] = [
        {
            lable: 'Bán hàng & Sơ đồ bàn',
            icon: 'pi pi-th-large',
            routerLink: '/catalogs/dining-tables',
            order: 1
        },
        {
            lable: 'Lịch sử hóa đơn',
            icon: 'pi pi-receipt',
            routerLink: '/catalogs/orders',
            order: 2
        },
        {
            lable: 'Quản lý Thực đơn',
            icon: 'pi pi-book',
            order: 3,
            items: [
                {
                    lable: 'Nhóm đồ uống',
                    icon: 'pi pi-folder',
                    routerLink: '/catalogs/menu-groups',
                    order: 1
                },
                {
                    lable: 'Món / Thức uống',
                    icon: 'pi pi-coffee',
                    routerLink: '/catalogs/menu-items',
                    order: 2
                }
            ]
        },
        {
            lable: 'Quản lý Chi nhánh',
            icon: 'pi pi-building',
            routerLink: '/catalogs/branches',
            order: 4
        },
        {
            lable: 'Sơ đồ Bàn & Khu vực',
            icon: 'pi pi-table',
            routerLink: '/catalogs/floor-plans',
            order: 5
        },
        {
            lable: 'Quản lý Khách hàng',
            icon: 'pi pi-users',
            routerLink: '/catalogs/business-partners',
            order: 5
        },
        {
            lable: 'Quản lý Nhân sự',
            icon: 'pi pi-id-card',
            order: 6,
            items: [
                {
                    lable: 'Danh sách nhân viên',
                    icon: 'pi pi-user-edit',
                    routerLink: '/catalogs/employees',
                    order: 1
                },
                {
                    lable: 'Tài khoản người dùng',
                    icon: 'pi pi-user',
                    routerLink: '/systems/account',
                    order: 2
                },
                {
                    lable: 'Vai trò & Quyền hạn',
                    icon: 'pi pi-shield',
                    routerLink: '/systems/role',
                    order: 3
                }
            ]
        },
        {
            lable: 'Kho & Nguyên liệu',
            icon: 'pi pi-box',
            routerLink: '/catalogs/materials',
            order: 7
        },
        {
            lable: 'Báo cáo Doanh thu',
            icon: 'pi pi-chart-bar',
            order: 8,
            items: [
                {
                    lable: 'Doanh thu theo ngày',
                    icon: 'pi pi-calendar',
                    routerLink: '/reports/daily-revenue',
                    order: 1
                },
                {
                    lable: 'Doanh thu theo món',
                    icon: 'pi pi-chart-pie',
                    routerLink: '/reports/revenue-by-item',
                    order: 2
                },
                {
                    lable: 'Doanh thu chi tiết',
                    icon: 'pi pi-list',
                    routerLink: '/reports/detailed-revenue',
                    order: 3
                }
            ]
        }
    ];

    loadMenu() {
        return of(this.defaultCafeMenu).pipe(
            tap((menu) => {
                this._rawMenu.set(this.normalizeMenuTree(menu));
                this.applyMenuFilter();
            })
        );
    }

    private filterActiveMenu(items: any[]): any[] {
        if (!items) return [];

        const permissions = this._permissions();
        const requireKitchen = this._requireKitchenSend();

        return items
            .filter((item) => {
                const status = item.status;
                if (status === false || status === 'false') {
                    return false;
                }

                const route = `${item.routerLink ?? ''}`.replace(/^\/+|\/+$/g, '').toLowerCase();
                if (route === 'catalogs/menu-visibility' &&
                    !permissions.some((code) => code.toUpperCase().replace(/\./g, '_') === 'SALESCATALOG_MENU_VISIBILITY_VIEW' || code.toLowerCase() === 'admin')) {
                    return false;
                }

                if (!requireKitchen) {
                    const routerLinkStr = `${item.routerLink ?? ''}`.toLowerCase();
                    const codeStr = `${item.code ?? item.Code ?? ''}`.toLowerCase();
                    if (routerLinkStr.includes('catalogs/kitchen') || routerLinkStr === 'kitchen' || codeStr.includes('kitchen')) {
                        return false;
                    }
                }

                const requiredPermissions = this.extractPermissions(item);
                if (!requiredPermissions.length) {
                    return true;
                }

                return requiredPermissions.some((permission) => permissions.includes(permission));
            })
            .map((item) => {
                const children = item.items && item.items.length > 0 ? this.filterActiveMenu(item.items) : [];
                if (item.items && item.items.length > 0) {
                    return {
                        ...item,
                        items: children
                    };
                }
                return item;
            })
            .filter((item) => {
                if (item.items?.length) return true;
                return !!item.routerLink || !item.items;
            });
    }

    clear(): void {
        this._rawMenu.set([]);
        this._menu.set([]);
        this._breadcrumb.set([]);
    }

    private applyMenuFilter() {
        const filtered = this.filterActiveMenu(this._rawMenu());
        this._menu.set(filtered);
        this.buildBreadcrumb();
    }

    private normalizeMenuTree(items: any[]): AppMenuItem[] {
        if (!Array.isArray(items)) {
            return [];
        }

        return items
            .filter((item) => item && item.parentId == null)
            .map((item) => this.normalizeMenuItem(item))
            .sort((a, b) => this.compareMenuOrder(a, b));
    }

    private normalizeMenuItem(item: any): AppMenuItem {
        const children: AppMenuItem[] = Array.isArray(item?.items) ? item.items.map((child: any) => this.normalizeMenuItem(child)) : [];

        return {
            ...item,
            routerLink: this.normalizeRouterLink(item?.routerLink),
            items: children.sort((a, b) => this.compareMenuOrder(a, b))
        };
    }

    private normalizeRouterLink(routerLink: unknown): string | undefined {
        if (typeof routerLink !== 'string') {
            return undefined;
        }

        const value = routerLink.trim();
        return value ? value : undefined;
    }

    private compareMenuOrder(a: any, b: any): number {
        const orderA = Number.isFinite(Number(a?.order)) ? Number(a.order) : Number.MAX_SAFE_INTEGER;
        const orderB = Number.isFinite(Number(b?.order)) ? Number(b.order) : Number.MAX_SAFE_INTEGER;

        if (orderA !== orderB) {
            return orderA - orderB;
        }

        return String(a?.lable ?? '').localeCompare(String(b?.lable ?? ''));
    }

    private getResponseItems(res: any): any[] {
        const candidates = [res?.Data?.Content, res?.data?.content, res?.Data?.Items, res?.data?.items, res?.Data, res?.data, res];

        for (const candidate of candidates) {
            if (Array.isArray(candidate)) {
                return candidate;
            }
        }

        return [];
    }

    private extractPermissions(item: any): string[] {
        const sources = [item?.permission, item?.permissions, item?.listPermission];
        const values = sources.flatMap((source) => {
            if (!source) return [];
            return Array.isArray(source) ? source : [source];
        });

        return values
            .map((value: any) => value?.code || value?.Code || value?.name || value?.Name || value)
            .filter((value: any) => typeof value === 'string' && !!value.trim());
    }

    private buildBreadcrumb() {
        const menu = this._menu();
        const url = this.router.url.split('?')[0].split('#')[0];

        if (!menu?.length) {
            this._breadcrumb.set([]);
            return;
        }
        const path = this.activeRoute(menu, url);
        this._breadcrumb.set(path);

        this._breadcrumb.set(path);
    }

    activeRoute(menu: any[], url: string): MenuItem[] {
        const path = this.findBestPath(menu, url);
        if (!path) return [];

        return path.map((item, index, arr) => ({
            label: item.lable,
            icon: item.icon,
            routerLink: index === arr.length - 1 ? undefined : item.routerLink || undefined,
            status: item.status || undefined
        }));
    }

    private findBestPath(items: any[], url: string, parents: any[] = []): any[] | null {
        let best: any[] | null = null;

        for (const item of items) {
            const currentPath = [...parents, item];
            if (item.routerLink && this.matchRoute(url, item.routerLink)) {
                if (!best || this.normalize(item.routerLink).length > this.normalize(best.at(-1).routerLink).length) {
                    best = currentPath;
                }
            }
            if (item.items?.length) {
                const childBest = this.findBestPath(item.items, url, currentPath);
                if (childBest) {
                    if (!best || this.normalize(childBest.at(-1).routerLink).length > this.normalize(best.at(-1).routerLink).length) {
                        best = childBest;
                    }
                }
            }
        }

        return best;
    }

    private normalize(url: string): string[] {
        const clean = url.split('?')[0].split('#')[0];
        return clean.split('/').filter(Boolean);
    }

    private matchRoute(url: string, link: string): boolean {
        const u = this.normalize(url);
        const l = this.normalize(link);

        if (l.length > u.length) return false;

        return l.every((seg, i) => u[i] === seg);
    }
}
