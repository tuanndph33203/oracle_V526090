import { cleanObject } from '@/core/utils/cleanQuery';
import { DestroyRef, inject, signal, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MessageService, ConfirmationService, ScrollerOptions } from 'primeng/api';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { EMPTY_OPTION, onLazyloadSelect } from '../helper/select-virtual.helper';
import { SelectLazyLoadEvent } from 'primeng/select';
import { select, Store } from '@ngrx/store';
import { selectEmployee, selectPermissions, selectUser } from '@/store/auth/auth.selectors';
import { selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
export abstract class BaseFormPage<TForm, TPayload> {
    private searchMap = new Map<string, Subject<string>>();
    systemSettings = signal<any[]>([]);
    isSubmitting = signal(false);
    user = signal<any | null>(null);
    employee = signal<any | null>(null);
    branchUser = signal<any[]>([]);
    userPermissions = signal<any[]>([]);
    protected destroyRef: DestroyRef = inject(DestroyRef);
    protected abstract form: FormGroup;

    protected fb = inject(FormBuilder);
    protected confirmationService = inject(ConfirmationService);
    protected messageService = inject(MessageService);
    protected dialogRef = inject(DynamicDialogRef, { optional: true });
    protected config = inject(DynamicDialogConfig, { optional: true });
    protected store = inject(Store);

    protected getAutoCompleteLabel(item: any): string {
        return `${item?.Code} ${item?.Name ? '- ' + item?.Name : ''}`;
    }

    constructor() {
        this.store.pipe(select(selectUser), takeUntilDestroyed(this.destroyRef)).subscribe((user) => {
            this.user.set(user);
        });
        this.store.pipe(select(selectEmployee), takeUntilDestroyed(this.destroyRef)).subscribe((employee) => {
            this.employee.set(employee);
        });
        this.store.pipe(select(selectSystemSettings), takeUntilDestroyed(this.destroyRef)).subscribe((configs) => {
            this.systemSettings.set(configs);
        });
        this.store.pipe(select(selectSelectedBranchCodes), takeUntilDestroyed(this.destroyRef)).subscribe((codes) => {
            if (codes?.length === 0) return;
            this.branchUser.set(codes ?? []);
        });
        this.store
            .select(selectPermissions)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((permissions) => {
                this.userPermissions.set(permissions || []);
            });
    }

    protected selectAllAutocomplete(formField: string, target: WritableSignal<any[]>) {
        const all = target();
        this.form.patchValue({
            [formField]: all
        });
    }

    protected clearAllAutocomplete(formField: string) {
        this.form.patchValue({
            [formField]: []
        });
    }

    protected onError(): void {
        this.messageService.add({
            severity: 'error',
            summary: 'Có lỗi xảy ra',
            detail: 'Vui lòng thử lại sau'
        });
    }

    protected closeDialog(): void {
        if (!this.form.dirty) {
            this.dialogRef?.close(false);
            return;
        }
        this.confirmationService.close();
        this.confirmationService.confirm({
            key: 'leave-form',
            header: 'Xác nhận',
            message: 'Dữ liệu chưa được lưu. Bạn có chắc chắn muốn thoát?',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonProps: {
                label: 'Thoát',
                severity: 'danger'
            },
            rejectLabel: 'Tiếp tục nhập',
            accept: () => this.dialogRef?.close(false)
        });
    }

    protected err(control: string, error: string): boolean {
        const c = this.form.get(control);
        return !!(c && c.hasError(error) && (c.touched || c.dirty));
    }
    protected validateForm(): boolean {
        if (this.isSubmitting()) return false;
        this.isSubmitting.set(true);
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            this.form.markAllAsDirty();
            this.messageService.add({
                severity: 'warn',
                summary: 'Dữ liệu chưa được lưu',
                detail: 'Vui lòng kiểm tra các trường bắt buộc'
            });
            Object.keys(this.form.controls).forEach((key) => {
                const control = this.form.get(key);

                if (control?.invalid) {
                    console.log(key, {
                        errors: control.errors,
                        value: control.value,
                        status: control.status
                    });
                }
            });

            setTimeout(() => {
                const el = document.querySelector('.ng-invalid:not(form)') as HTMLElement;

                if (!el) return;

                el.scrollIntoView({
                    behavior: 'smooth',
                    block: 'center'
                });
                setTimeout(() => {
                    window.scrollBy({
                        top: -100,
                        behavior: 'smooth'
                    });
                }, 300);
            }, 0);
            this.isSubmitting.set(false);
            return false;
        }
        return true;
    }

    protected handleResponse(res: any): void {
        if (res?.Code === 200 || res?.code === 200) {
            this.messageService.add({
                severity: 'info',
                summary: 'Thành công!'
            });
            this.dialogRef?.close(true);
            return;
        }

        if (res?.Code === 409 || res?.code === 409) {
            this.messageService.add({
                severity: 'warn',
                summary: res?.Message || 'Dữ liệu đã tồn tại'
            });
            this.isSubmitting.set(false);
            return;
        }
        if (res?.Code === 500) {
            this.messageService.add({
                severity: 'warn',
                summary: res?.Message || 'Đã có lỗi xảy ra, vui lòng liên hệ quản trị để được hỗ trợ'
            });
            this.isSubmitting.set(false);
            return;
        }

        this.messageService.add({
            severity: 'warn',
            summary: 'Đã có lỗi xảy ra, vui lòng liên hệ quản trị để được hỗ trợ',
        });
        this.isSubmitting.set(false);
    }

    protected onFilterSelect(key: string, keyword: string) {
        this.searchMap.get(key)?.next(keyword);
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

        if (target().length === 0) {
            search$.next('');
        }
    }

    protected searchAutoComplete(event: any, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, extraQuery: any = {}) {
        const query = {
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

    protected buildSelectOptions(service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>, searchQuery: WritableSignal<any>): ScrollerOptions {
        return {
            lazy: true,
            delay: 200,
            onLazyLoad: (event: SelectLazyLoadEvent) => {
                onLazyloadSelect(event, target, searchQuery, (q) => this.searchSelect(q, service, target));
            }
        };
    }

    protected searchSelect(query: any, service: { getByFilter: (q: any) => any }, target: WritableSignal<any[]>): void {
        const queryData = cleanObject(query);
        service.getByFilter(queryData).subscribe((res: any) => {
            const content = this.getResponseItems(res);
            const total = this.getResponseTotal(res, content.length);
            let items = target();
            if (!items.length || items.length !== total) {
                items = Array.from({ length: total }, () => EMPTY_OPTION);
            } else {
                items = [...items];
            }
            const page = query.page ?? 1;
            content.forEach((item: any, idx: number) => {
                const index = (page - 1) * query.size + idx;
                items[index] = item;
            });
            target.set(items);
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

    protected isEnabled = (code: string): boolean => {
        return this.systemSettings().some((x) => x.Code === code);
    };
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
}
