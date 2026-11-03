import { DestroyRef, inject, signal, WritableSignal } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { catchError, debounceTime, distinctUntilChanged, finalize, of, Subject, switchMap, tap } from 'rxjs';
import { cleanObject } from '@/core/utils/cleanQuery';
import { PaginatorState } from 'primeng/paginator';
import { ActivatedRoute, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { selectAvailableBranchCodes, selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectPermissions } from '@/store/auth/auth.selectors';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import { fromEvent } from 'rxjs';

export abstract class BaseCrudPage<T> {
    private searchMap = new Map<string, Subject<string>>();
    systemSettings = signal<any[]>([]);
    branchUser = signal<any[]>([]);
    data = signal<T[]>([]);
    loading = signal(true);
    total = signal(0);
    itemViewport = signal(15);
    optionPanigations = [10, 15, 50, 100, 200, 500, 1000];
    first = 0;
    firstLoad = true;
    BranchCode = signal('');
    selectedBranch = signal<any[]>([]);
    userPermissions = signal<string[]>([]);
    isMobileView = signal(window.innerWidth < 768);
    mobileFilterVisible = signal(false);

    searchQuery = signal({
        page: 1,
        size: 15
    });

    searchQueryOrigin = signal({
        page: 1,
        size: 15
    });

    private search$ = new Subject<any>();
    protected abstract service: {
        getByFilter: (q: any) => any;
        delete?: (id: any) => any;
    };

    protected abstract formComponent?: any;
    protected abstract entityName: string;
    protected store = inject(Store);
    protected dialogService = inject(DialogService);
    protected router = inject(Router);
    protected route = inject(ActivatedRoute);
    protected confirmationService = inject(ConfirmationService);
    protected messageService = inject(MessageService);
    protected destroyRef: DestroyRef = inject(DestroyRef);
    constructor() {
        fromEvent(window, 'resize')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                const isMobile = window.innerWidth < 768;
                this.isMobileView.set(isMobile);
                if (!isMobile) {
                    this.mobileFilterVisible.set(false);
                }
            });

        this.store.pipe(select(selectSelectedBranchCodes), takeUntilDestroyed(this.destroyRef)).subscribe((codes) => {
            if (codes?.length === 0) return;
            this.selectedBranch.set(codes ?? []);
            this.BranchCode.set(codes?.length ? codes.map((code: any) => code.Code).join(',') : '');
            this.searchQuery.update((q) => ({ ...q, BranchCode: this.BranchCode() }));
            if (!this.firstLoad) {
                this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() });
            }
        });

        this.store.pipe(select(selectAvailableBranchCodes), takeUntilDestroyed(this.destroyRef)).subscribe((branchUser) => {
            this.branchUser.set(branchUser);
        });
        this.store
            .select(selectPermissions)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((permissions) => {
                this.userPermissions.set(permissions || []);
            });

        this.store.pipe(select(selectSystemSettings), takeUntilDestroyed(this.destroyRef)).subscribe((configs) => {
            this.systemSettings.set(configs);
        });
        const size = Math.max(10, Math.round((window.innerHeight - 220) / 50 / 5) * 5);
        this.itemViewport.set(size);
        this.searchQuery.update((prev) => {
            return {
                ...prev,
                size
            };
        });

        this.search$
            .pipe(
                debounceTime(400),
                takeUntilDestroyed(this.destroyRef),
                switchMap((query) => {
                    this.loading.set(true);
                    this.data.set([]);
                    return this.service.getByFilter(cleanObject(query)).pipe(
                        catchError((err) => {
                            return of({
                                Data: {
                                    Content: [],
                                    TotalElements: 0
                                }
                            });
                        }),
                        finalize(() => this.loading.set(false))
                    );
                })
            )
            .subscribe((res: any) => {
                const items = this.mapResponseItems(this.getResponseItems(res));
                this.data.set(items);
                this.total.set(this.getResponseTotal(res, items.length));
            });
    }

    private syncFilterToUrl(query: any) {
        if (!query) return;
        const { page, size, BranchCode, ...filters } = query;
        const cleaned = cleanObject(filters);
        this.router.navigate([], {
            queryParams: cleaned,
            replaceUrl: true
        });
    }

    protected getAutoCompleteLabel(item: any): string {
        return `${item?.Code} ${item?.Name ? '- ' + item?.Name : ''}`;
    }

    search(query: any) {
        this.searchQuery.set(query);
        this.syncFilterToUrl(query);
        this.search$.next(query);
    }

    protected mapResponseItems(items: T[]): T[] {
        return items;
    }

    onSearch(filters: any) {
        this.firstLoad = false;
        this.searchQuery.update((q) => ({ ...q, page: 1, BranchCode: this.BranchCode(), ...filters }));
        this.syncFilterToUrl(this.searchQuery());
        this.search$.next(this.searchQuery());
    }

    closeMobileFilter() {
        this.mobileFilterVisible.set(false);
    }

    onPageChange(event: PaginatorState) {
        const rows = event.rows ?? 15;
        const first = event.first ?? 0;
        this.first = first;
        this.searchQuery.set({
            ...this.searchQuery(),
            page: first / rows + 1,
            size: rows
        });
        this.search$.next(this.searchQuery());
    }

    onCreate() {
        this.dialogService.open(this.formComponent, { showHeader: false })?.onClose.subscribe((ok) => ok && this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() }));
    }

    onEdit(row: any) {
        this.dialogService
            .open(this.formComponent, {
                showHeader: false,
                data: { edition: true, data: row }
            })
            ?.onClose.subscribe((ok) => ok && this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() }));
    }

    onDelete(row: any) {
        this.confirmationService.confirm({
            key: 'delete-data',
            message: 'Bạn có chắc chắn muốn xóa vĩnh viễn?',
            header: 'Xác nhận',
            icon: 'pi pi-info-circle',
            acceptButtonProps: { label: 'Đồng ý', severity: 'danger' },
            rejectButtonProps: { label: 'Hủy bỏ', severity: 'secondary', outlined: true },
            accept: () => {
                this.service.delete?.(row.Id).subscribe({
                    next: (res: any) => {
                        // Some endpoints may not wrap response; only validate when Code exists.
                        if (res && typeof res === 'object' && 'Code' in res && res?.Code !== 200) {
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Lỗi',
                                detail: res?.Message || 'Không thể xóa bản ghi'
                            });
                            return;
                        }

                        this.messageService.add({
                            severity: 'info',
                            summary: 'Thành công',
                            detail: 'Bản ghi đã bị xóa'
                        });
                        this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() });
                    },
                    error: (err: any) => {
                        const detail = err?.error?.Message || err?.message || 'Không thể xóa bản ghi';
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Lỗi',
                            detail
                        });
                    }
                });
            }
        });
    }

    protected initSearch(key: string, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        if (this.searchMap.has(key)) return;

        const search$ = new Subject<string>();
        this.searchMap.set(key, search$);

        search$
            .pipe(
                debounceTime(300),
                distinctUntilChanged(),
                switchMap((keyword) =>
                    service.getByFilter({
                        TextSearch: keyword || '',
                        ...extraQuery
                    })
                ),
                takeUntilDestroyed(this.destroyRef)
            )
            .subscribe((res: any) => {
                target.set(this.getResponseItems(res));
            });
        if (!target()?.length) {
            search$.next('');
        }
    }

    protected onFilterSelect(key: string, keyword: string) {
        this.searchMap.get(key)?.next(keyword);
    }

    protected searchAutoComplete(event: any, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        const query = {
            page: 1,
            size: 15,
            TextSearch: event?.query || '',
            ...extraQuery
        };

        const request$ = service.getByFilter(query);
        if (this.destroyRef) {
            request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res: any) => {
                target.set(this.getResponseItems(res));
            });
        } else {
            request$.subscribe((res: any) => {
                target.set(this.getResponseItems(res));
            });
        }
    }

    protected searchSelect(query: any, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>): void {
        const queryData = cleanObject(query);
        service.getByFilter(queryData).subscribe((res: any) => {
            const content = this.getResponseItems(res);
            target.set(content);
        });
    }

    protected onFilter(query: string, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>) {
        if (typeof query !== 'string') return;
        service.getByFilter({ TextSearch: query }).subscribe((res: any) => {
            if (res?.Code === 200) {
                const data = this.getResponseItems(res);
                target.set(data);
            }
        });
    }

    protected searchMultiSelect(key: string, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        service.getByFilter({ TextSearch: key, ...extraQuery }).subscribe((res: any) => {
            if (res?.Code === 200) {
                target.set(this.getResponseItems(res));
            }
        });
    }

    protected focusMultiSelect(service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        if (target().length > 0) return;
        service.getByFilter({ TextSearch: '', ...extraQuery }).subscribe((res: any) => {
            if (res?.Code === 200) {
                target.set(this.getResponseItems(res));
            }
        });
    }

    protected hasPermission(permission: string): boolean {
        return true;
    }

    protected getResponseItems(res: any): any[] {
        const candidates = [res?.Data?.Content, res?.data?.content, res?.Data?.Items, res?.data?.items, res?.Data, res?.data, res];

        for (const candidate of candidates) {
            if (Array.isArray(candidate)) {
                return candidate;
            }
        }

        return [];
    }

    protected getResponseTotal(res: any, fallbackLength = 0): number {
        const candidates = [res?.Data?.TotalElements, res?.data?.totalElements, res?.Data?.TotalCount, res?.data?.totalCount, res?.Data?.NumberOfElements, res?.data?.numberOfElements];

        for (const candidate of candidates) {
            const value = Number(candidate);
            if (Number.isFinite(value)) {
                return value;
            }
        }

        return fallbackLength;
    }

    protected isEnabled = (code: string): boolean => {
        return this.systemSettings().some((x) => x.Code === code);
    };

    // protected toggleSelectAll(formField: string, target: WritableSignal<any[]>, valueKey: string, checked: boolean) {
    //     const data = target();

    //     const values = checked ? data.map((x) => x[valueKey]) : [];

    //     this.form.patchValue({
    //         [formField]: values
    //     });
    // }

    // protected selectAll(formField: string, target: WritableSignal<any[]>, valueKey: string) {
    //     const all = target().map((x) => x[valueKey]);

    //     this.form.patchValue({
    //         [formField]: all
    //     });
    // }

    // protected clearAll(formField: string) {
    //     this.form.patchValue({
    //         [formField]: []
    //     });
    // }

   resolveDownloadFileName(response: any, fallbackName: string, fileType?: string): string {
        const disposition = response.headers.get('content-disposition') || response.headers.get('Content-Disposition');
        const match = disposition?.match(/filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i);
        const headerFileName = decodeURIComponent((match?.[1] || match?.[2] || '').trim());
        if (headerFileName) return headerFileName;

        const extension = (fileType || 'xlsx').toString().trim().replace('.', '').toLowerCase();
        return `${fallbackName}.${extension || 'xlsx'}`;
    }
}
