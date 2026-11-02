import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Popover } from 'primeng/popover';
import { InputText } from 'primeng/inputtext';
import { finalize } from 'rxjs';
import { ISettingGroup, ISettingItem } from '../../models/system-setting';
import { ISystemSettingBulkPayload, SystemSettingsService } from '../../services/system-settings';
import { SYSTEM_SETTING_GROUPS } from './system-settings.config';
import { DialogService } from 'primeng/dynamicdialog';
import { SystemSettingCreate } from '../../components/system-setting-create/system-setting-create';

import { Store } from '@ngrx/store';
import SystemSettingsAction from '@/store/system-settings/system-settings.action';

import { SelectModule } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';
import { DialogModule } from 'primeng/dialog';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { PreparationAreaService } from '../../../../catalogs/preparation-area/services/preparation-area';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-system-settings',
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        ToggleSwitch,
        Popover,
        InputText,
        SelectModule,
        MultiSelect,
        DialogModule,
        DatePickerModule,
        CheckboxModule,
        RouterLink
    ],
    templateUrl: './system-settings.html',
    styleUrl: './system-settings.scss'
})
export class SystemSettings implements OnInit {
    readonly settingGroups = signal<ISettingGroup[]>(this.buildGroups());
    readonly activeModule = signal<string>(SYSTEM_SETTING_GROUPS[0]?.module ?? '');
    readonly isLoading = signal(false);
    readonly isSaving = signal(false);
    readonly missingCodes = signal<string[]>([]);
    readonly selectedItem = signal<ISettingItem | null>(null);
    readonly preparationAreaOptions = signal<any[]>([]);
    readonly settingsVersion = signal(0);
    readonly isInvoiceLockEnabled = signal<boolean>(false);
    readonly invoiceLockDate = signal<Date | null>(new Date());
    readonly isInvoiceLockAuto = signal<boolean>(true);
    readonly invoiceLockAutoDays = signal<number>(0);
    readonly isSavingInvoiceLock = signal<boolean>(false);
    readonly activeGroup = computed(() => this.settingGroups().find((group) => group.module === this.activeModule()) ?? this.settingGroups()[0] ?? null);
    readonly hasChanges = computed(() => {
        this.settingsVersion();
        const initial = this.initialState();
        return this.settingGroups().some((group) =>
            group.items.some((item) => initial[item.Code] !== this.serializeItemValue(item))
        );
    });

    private readonly initialState = signal<Record<string, string>>(this.createSnapshot(this.settingGroups()));
    private readonly service = inject(SystemSettingsService);
    private readonly prepAreaService = inject(PreparationAreaService);
    private readonly messageService = inject(MessageService);
    private readonly dialogService = inject(DialogService);
    private readonly store = inject(Store);

    ngOnInit(): void {
        this.loadSettings();
        this.loadPreparationAreas();
    }

    loadPreparationAreas(): void {
        this.prepAreaService.getByFilter({ Page: 1, Size: 1000 }).subscribe({
            next: (res: any) => {
                const items = res?.Data?.Content || res?.Data?.Items || (Array.isArray(res?.Data) ? res?.Data : []);
                this.preparationAreaOptions.set(Array.isArray(items) ? items : []);
            },
            error: () => {
                this.preparationAreaOptions.set([]);
            }
        });
    }

    onAddSetting(): void {
        const ref = this.dialogService.open(SystemSettingCreate, {
            header: 'Thêm cấu hình hệ thống',
            width: '500px'
        });

        if (ref) {
            ref.onClose.subscribe((success) => {
                if (success) {
                    this.loadSettings();
                }
            });
        }
    }

    onEditSetting(item: ISettingItem): void {
        const ref = this.dialogService.open(SystemSettingCreate, {
            header: 'Cập nhật cấu hình hệ thống',
            width: '500px',
            data: { data: item }
        });

        if (ref) {
            ref.onClose.subscribe((success) => {
                if (success) {
                    this.loadSettings();
                }
            });
        }
    }

    onRowContextMenu(event: MouseEvent, item: ISettingItem, op: Popover): void {
        event.preventDefault();
        this.selectedItem.set(item);
        op.show(event);
    }

    selectModule(module: string): void {
        this.activeModule.set(module);
    }

    activeModuleLabel(): string {
        return this.activeGroup()?.label ?? 'Cài đặt hệ thống';
    }

    onToggle(item: ISettingItem): void {
        if (item.DataType === 'Boolean') {
            item.Value = String(!!item._checked);
            item.IsActive = !!item._checked;
        }
        this.settingsVersion.update((v) => v + 1);
    }

    onValueChange(item: ISettingItem, newValue: any): void {
        item.Value = newValue !== undefined && newValue !== null ? String(newValue) : '';
        this.settingsVersion.update((v) => v + 1);
    }

    onSave(): void {
        if (this.isSaving()) {
            return;
        }

        // Validate number type
        const invalidNumberItem = this.settingGroups()
            .flatMap((group) => group.items)
            .find((item) => item.DataType === 'Number' && (item.Value === '' || isNaN(Number(item.Value))));

        if (invalidNumberItem) {
            this.messageService.add({
                severity: 'error',
                summary: 'Loi dinh dang',
                detail: 'Gia tri phai la so hop le'
            });
            return;
        }

        const payload = this.buildPayload();
        if (!payload.Items.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Chua co du lieu de luu.'
            });
            return;
        }

        this.isSaving.set(true);

        this.service
            .updateBulk(payload)
            .pipe(finalize(() => this.isSaving.set(false)))
            .subscribe({
                next: (res) => {
                    if (res?.Code === 200) {
                        this.initialState.set(this.createSnapshot(this.settingGroups()));
                        this.store.dispatch(SystemSettingsAction.loadAll());
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Cập nhật thành công.'
                        });
                        return;
                    }

                    if (res?.Code === 409) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Trung ma.'
                        });
                        return;
                    }

                    this.messageService.add({
                        severity: 'error',
                        summary: res?.Message || 'Cap nhat that bai.'
                    });
                },
                error: () => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Khong the cap nhat.'
                    });
                }
            });
    }

    private loadSettings(): void {
        this.isLoading.set(true);

        this.service
            .getAll()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: (res) => {
                    const settings = (res?.Data ?? []) as ISettingItem[];
                    const groups = this.buildGroups(settings);

                    this.settingGroups.set(groups);
                    if (groups.length > 0 && !groups.some((g) => g.module === this.activeModule())) {
                        this.activeModule.set(groups[0].module);
                    }
                    this.initialState.set(this.createSnapshot(groups));
                    this.missingCodes.set(this.collectMissingCodes(groups));
                    this.syncInvoiceLockFormFromSettings();
                },
                error: () => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Không thể tải dữ liệu cài đặt.'
                    });
                }
            });
    }

    private buildPayload(): ISystemSettingBulkPayload {
        return {
            Items: this.settingGroups()
                .flatMap((group) => group.items)
                .filter((item) => !!item.Id)
                .map((item) => ({
                    Id: item.Id!,
                    Name: item.Name,
                    Value: item.DataType === 'Boolean' ? String(!!item._checked) : item.Value,
                    DataType: item.DataType,
                    Module: item.Module,
                    Description: item.Description,
                    IsActive: item.DataType === 'Boolean' ? !!item._checked : !!item.IsActive
                }))
        };
    }

    private buildGroups(apiSettings: ISettingItem[] = []): ISettingGroup[] {
        const processedCodes = new Set<string>();

        const groups: ISettingGroup[] = SYSTEM_SETTING_GROUPS.map((group) => {
            const groupItems: ISettingItem[] = [];

            for (const definition of group.items) {
                const normCode = this.normalizeCode(definition.Code);
                const apiItem = apiSettings.find((item) => this.normalizeCode(item.Code) === normCode);

                if (apiItem) {
                    processedCodes.add(normCode);
                    const rawVal = apiItem.Value ?? definition.Value ?? '';
                    groupItems.push({
                        Id: apiItem.Id,
                        Code: apiItem.Code || definition.Code,
                        Name: apiItem.Name || definition.Name,
                        Value: rawVal,
                        DataType: apiItem.DataType || definition.DataType,
                        Module: group.module,
                        Description: apiItem.Description || definition.Description,
                        IsActive: apiItem.IsActive ?? definition.IsActive,
                        _checked: this.parseBoolean(rawVal, apiItem.IsActive),
                        _mapped: true,
                        _multiValues: rawVal ? rawVal.split(',').map((x: string) => x.trim()).filter(Boolean) : []
                    });
                } else {
                    const rawVal = definition.Value ?? '';
                    groupItems.push({
                        Id: '',
                        Code: definition.Code,
                        Name: definition.Name,
                        Value: rawVal,
                        DataType: definition.DataType,
                        Module: group.module,
                        Description: definition.Description,
                        IsActive: definition.IsActive,
                        _checked: definition.IsActive,
                        _mapped: false,
                        _multiValues: rawVal ? rawVal.split(',').map((x: string) => x.trim()).filter(Boolean) : []
                    });
                }
            }

            return {
                module: group.module,
                label: group.label,
                icon: group.icon,
                description: group.description,
                items: groupItems
            };
        });

        // Add unmapped items from API dynamically
        const unmappedApiItems = apiSettings.filter((item) => !processedCodes.has(this.normalizeCode(item.Code)));
        for (const item of unmappedApiItems) {
            const itemModule = item.Module || 'system';
            let group = groups.find((g) => this.normalizeCode(g.module) === this.normalizeCode(itemModule));

            if (!group) {
                const label = itemModule === 'accounting' ? 'Kế toán & Hóa đơn' : itemModule === 'system' ? 'Hệ thống & QR Order' : itemModule;
                const icon = itemModule === 'accounting' ? 'pi pi-file-edit' : 'pi pi-qrcode';
                group = {
                    module: itemModule,
                    label,
                    icon,
                    description: `Cấu hình cho module ${itemModule}`,
                    items: []
                };
                groups.push(group);
            }

            const rawVal = item.Value ?? '';
            group.items.push({
                Id: item.Id,
                Code: item.Code,
                Name: item.Name || item.Code,
                Value: rawVal,
                DataType: item.DataType || 'string',
                Module: itemModule,
                Description: item.Description || '',
                IsActive: item.IsActive ?? true,
                _checked: this.parseBoolean(rawVal, item.IsActive),
                _mapped: true,
                _multiValues: rawVal ? rawVal.split(',').map((x: string) => x.trim()).filter(Boolean) : []
            });
        }

        // If API returned settings, filter out groups without any API-backed items to keep view clean
        if (apiSettings.length > 0) {
            const hasMapped = groups.some((g) => g.items.some((i) => !!i.Id));
            if (hasMapped) {
                return groups.filter((g) => g.items.some((i) => !!i.Id));
            }
        }

        return groups;
    }

    private createSnapshot(groups: ISettingGroup[]): Record<string, string> {
        return groups.flatMap((group) => group.items).reduce(
            (acc, item) => ({
                ...acc,
                [item.Code]: this.serializeItemValue(item)
            }),
            {} as Record<string, string>
        );
    }

    private collectMissingCodes(groups: ISettingGroup[]): string[] {
        return groups
            .flatMap((group) => group.items)
            .filter((item) => !item.Id && !this.isInvoiceLockSubItem(item.Code))
            .map((item) => item.Code);
    }

    private serializeItemValue(item: ISettingItem): string {
        const val = item.DataType === 'Boolean' ? String(!!item._checked) : item.Value;
        const active = item.DataType === 'Boolean' ? !!item._checked : !!item.IsActive;
        return `${val}::${active}`;
    }

    toNumber(val: string | undefined): number | null {
        if (val === undefined || val === null || val === '') return null;
        const num = Number(val);
        return isNaN(num) ? null : num;
    }

    private parseBoolean(value: string | undefined, fallback = false): boolean {
        const normalizedValue = `${value ?? ''}`.trim().toLowerCase();

        if (normalizedValue === 'true' || normalizedValue === '1' || normalizedValue === 'yes' || normalizedValue === 'on') {
            return true;
        }

        if (normalizedValue === 'false' || normalizedValue === '0' || normalizedValue === 'no' || normalizedValue === 'off') {
            return false;
        }

        return fallback;
    }

    onAreaMultiSelectChange(item: ISettingItem): void {
        const joined = Array.isArray(item._multiValues) ? item._multiValues.join(',') : '';
        item.Value = joined;
        this.settingsVersion.update((v) => v + 1);
    }

    syncInvoiceLockFormFromSettings(syncEnabled = true): void {
        const allItems = this.settingGroups().flatMap((g) => g.items);
        const enabledItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-enabled');
        const autoItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-auto');
        const daysItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-auto-days');
        const dateItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-date');

        if (syncEnabled) {
            const isEnabled = enabledItem ? this.parseBoolean(enabledItem.Value, enabledItem.IsActive) : false;
            this.isInvoiceLockEnabled.set(isEnabled);
        }
        const isAuto = autoItem ? this.parseBoolean(autoItem.Value, true) : true;
        const autoDays = daysItem && !isNaN(Number(daysItem.Value)) ? Math.max(0, Number(daysItem.Value)) : 0;

        this.isInvoiceLockAuto.set(isAuto);
        this.invoiceLockAutoDays.set(autoDays);

        if (isAuto) {
            const calculatedDate = new Date();
            calculatedDate.setDate(calculatedDate.getDate() - autoDays);
            this.invoiceLockDate.set(calculatedDate);
        } else if (dateItem?.Value) {
            this.invoiceLockDate.set(this.parseDateString(dateItem.Value));
        } else {
            this.invoiceLockDate.set(new Date());
        }
    }

    onInvoiceLockToggleChange(checked: boolean, item?: ISettingItem): void {
        this.isInvoiceLockEnabled.set(checked);
        if (item) {
            item._checked = checked;
            item.Value = String(checked);
            item.IsActive = checked;
        }
        if (checked) {
            this.syncInvoiceLockFormFromSettings(false);
        }
        this.settingsVersion.update((v) => v + 1);
    }

    onInvoiceLockAutoChange(event: any): void {
        const checked = typeof event === 'boolean' ? event : !!event?.checked;
        this.isInvoiceLockAuto.set(checked);
        if (checked) {
            const days = this.invoiceLockAutoDays() || 0;
            const targetDate = new Date();
            targetDate.setDate(targetDate.getDate() - days);
            this.invoiceLockDate.set(targetDate);
        }
    }

    onInvoiceLockAutoDaysChange(newDays: any): void {
        const days = Number(newDays) || 0;
        this.invoiceLockAutoDays.set(days);
        if (this.isInvoiceLockAuto()) {
            const targetDate = new Date();
            targetDate.setDate(targetDate.getDate() - days);
            this.invoiceLockDate.set(targetDate);
        }
    }

    onInvoiceLockDateChange(newDate: Date | null): void {
        if (newDate) {
            this.invoiceLockDate.set(newDate);
        }
    }

    onSaveInvoiceLock(): void {
        if (this.isSavingInvoiceLock()) return;

        const date = this.invoiceLockDate() || new Date();
        const formattedDate = this.formatDateString(date);
        const isAuto = this.isInvoiceLockAuto();
        const autoDays = String(this.invoiceLockAutoDays() ?? 0);

        this.isSavingInvoiceLock.set(true);

        const allItems = this.settingGroups().flatMap((g) => g.items);
        const enabledItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-enabled');
        const autoItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-auto');
        const daysItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-auto-days');
        const dateItem = allItems.find((i) => this.normalizeCode(i.Code) === 'invoice-lock-date');

        const isEnabled = this.isInvoiceLockEnabled();

        const itemsToUpdate = [
            {
                Id: enabledItem?.Id || '',
                Code: 'invoice-lock-enabled',
                Name: 'Thiết lập sửa hoá đơn',
                Value: String(isEnabled),
                DataType: 'Boolean',
                Module: 'SaleBill',
                Description: 'Bật để thiết lập khóa ngày sửa hoá đơn theo ngày cố định hoặc tự động lùi số ngày.',
                IsActive: isEnabled
            },
            {
                Id: dateItem?.Id || '',
                Code: 'invoice-lock-date',
                Name: 'Ngày khóa hoá đơn',
                Value: formattedDate,
                DataType: 'String',
                Module: 'SaleBill',
                Description: 'Ngày khóa sửa/hủy hóa đơn. Hoá đơn có ngày lập nhỏ hơn hoặc bằng ngày này sẽ không được sửa.',
                IsActive: true
            },
            {
                Id: autoItem?.Id || '',
                Code: 'invoice-lock-auto',
                Name: 'Tự động khóa hoá đơn',
                Value: String(isAuto),
                DataType: 'Boolean',
                Module: 'SaleBill',
                Description: 'Tự động tính ngày khóa theo số ngày cấu hình.',
                IsActive: isAuto
            },
            {
                Id: daysItem?.Id || '',
                Code: 'invoice-lock-auto-days',
                Name: 'Ngày tự động khóa hoá đơn',
                Value: autoDays,
                DataType: 'Number',
                Module: 'SaleBill',
                Description: 'Số ngày tự động lùi so với ngày hiện tại để xác định ngày khóa hoá đơn.',
                IsActive: true
            }
        ];

        const bulkItems = itemsToUpdate.filter((x) => !!x.Id);
        const newItems = itemsToUpdate.filter((x) => !x.Id);

        const finalizeSave = () => {
            this.isSavingInvoiceLock.set(false);
            this.loadSettings();
            this.store.dispatch(SystemSettingsAction.loadAll());
            this.messageService.add({
                severity: 'success',
                summary: 'Thành công',
                detail: 'Cập nhật thiết lập sửa hoá đơn thành công.'
            });
        };

        if (bulkItems.length > 0) {
            this.service.updateBulk({
                Items: bulkItems.map((item) => ({
                    Id: item.Id,
                    Name: item.Name,
                    Value: item.Value,
                    DataType: item.DataType,
                    Module: item.Module,
                    Description: item.Description,
                    IsActive: item.IsActive
                }))
            }).subscribe({
                next: (res) => {
                    if (res?.Code === 200 || res?.code === 200) {
                        if (newItems.length > 0) {
                            let pending = newItems.length;
                            for (const ni of newItems) {
                                this.service.create(ni).subscribe({
                                    next: () => {
                                        pending--;
                                        if (pending === 0) finalizeSave();
                                    },
                                    error: () => {
                                        pending--;
                                        if (pending === 0) finalizeSave();
                                    }
                                });
                            }
                        } else {
                            finalizeSave();
                        }
                    } else {
                        this.isSavingInvoiceLock.set(false);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Lỗi',
                            detail: res?.Message || 'Không thể lưu thiết lập sửa hoá đơn.'
                        });
                    }
                },
                error: (err) => {
                    this.isSavingInvoiceLock.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: err?.error?.Message || 'Không thể lưu thiết lập sửa hoá đơn.'
                    });
                }
            });
        } else if (newItems.length > 0) {
            let pending = newItems.length;
            for (const ni of newItems) {
                this.service.create(ni).subscribe({
                    next: () => {
                        pending--;
                        if (pending === 0) finalizeSave();
                    },
                    error: () => {
                        pending--;
                        if (pending === 0) finalizeSave();
                    }
                });
            }
        } else {
            finalizeSave();
        }
    }

    isInvoiceLockSubItem(code: string | undefined): boolean {
        const c = this.normalizeCode(code);
        return c === 'invoice-lock-date' || c === 'invoice-lock-auto' || c === 'invoice-lock-auto-days';
    }

    isInvoiceLockItem(code: string | undefined): boolean {
        return this.isInvoiceLockSubItem(code);
    }

    formatDateString(d: Date): string {
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        return `${day}-${month}-${year}`;
    }

    parseDateString(str: string): Date {
        if (!str) return new Date();
        const parts = str.trim().split(/[-/]/);
        if (parts.length === 3) {
            if (parts[0].length === 4) {
                return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
            } else {
                return new Date(Number(parts[2]), Number(parts[1]) - 1, Number(parts[0]));
            }
        }
        const parsed = new Date(str);
        return isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private normalizeCode(code: string | undefined): string {
        return `${code ?? ''}`.trim().toLowerCase();
    }
}
