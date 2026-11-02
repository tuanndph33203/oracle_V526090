import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { catchError, map, of, switchMap } from 'rxjs';
import { MinioService } from '@/core/services/minio';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { FileUploadMeta } from '@/shared/ui/form/file-upload-meta/file-upload-meta';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { MenuGroupService } from '../../../menu-group/services/menu-group';
import { TaxRateService } from '../../../tax-rate/services/tax-rate';
import { UnitService } from '../../../../catalogs/unit/services/unit';
import { MenuItemService } from '../../services/menu-item';
import { BranchService } from '../../../../branch/services/branch';
import { PreparationAreaService } from '../../../preparation-area/services/preparation-area';
import { ServicePreferenceService } from '../../../service-preference/services/service-preference';
import { MultiSelect } from 'primeng/multiselect';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { DialogModule } from 'primeng/dialog';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-menu-item-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, TextareaModule, SelectModule, MultiSelect, ToggleSwitch, ButtonModule, DialogModule, DialogWrap, FileUploadMeta],
    templateUrl: './menu-item-create.html',
    styleUrl: './menu-item-create.scss'
})
export class MenuItemCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;

    private mainService = inject(MenuItemService);
    private menugroupService = inject(MenuGroupService);
    private unitService = inject(UnitService);
    private taxrateService = inject(TaxRateService);
    private minioService = inject(MinioService);
    private branchService = inject(BranchService);
    private preparationAreaService = inject(PreparationAreaService);
    private servicePreferenceService = inject(ServicePreferenceService);

    menuGroupOptions = signal<any[]>([]);
    unitOptions = signal<any[]>([]);
    taxRateOptions = signal<any[]>([]);
    branchOptions = signal<any[]>([]);
    preparationAreaOptions = signal<any[]>([]);
    servicePreferenceOptions = signal<any[]>([]);
    imageFile = signal<File[] | null>(null);
    expectedTotalPrice = signal<number>(0);
    optionEditorVisible = false;
    selectedOptionGroupIndex = 0;
    previewSelectedIndexes: number[] = [];
    private optionEditorSnapshot: any[] = [];
    private optionEditorApplied = false;

    get optionItemImageUrl(): string | null {
        const image = this.form.get('ImageUrl')?.value;
        if (typeof image !== 'string' || !image) return null;
        return /^https?:\/\//.test(image) ? image : `${environment.apiUrl}minio/download?objectName=${encodeURIComponent(image)}`;
    }

    get selectedOptionGroup(): FormGroup | null {
        return (this.optionGroups.at(this.selectedOptionGroupIndex) as FormGroup) || null;
    }

    openOptionEditor() {
        this.optionEditorSnapshot = structuredClone(this.optionGroups.getRawValue());
        this.optionEditorApplied = false;
        this.selectOptionGroup(0);
        this.optionEditorVisible = true;
    }

    onOptionEditorHide() {
        if (!this.optionEditorApplied) {
            this.optionGroups.clear();
            this.optionEditorSnapshot.forEach(group => this.addOptionGroup(group));
        }
        this.optionEditorApplied = false;
        this.selectOptionGroup(0);
    }

    selectOptionGroup(index: number) {
        this.selectedOptionGroupIndex = index;
        const values = this.optionGroups.length ? this.optionValues(index).controls : [];
        this.previewSelectedIndexes = values.map((value, i) => value.get('IsDefault')?.value ? i : -1).filter(i => i >= 0);
        if (!this.previewSelectedIndexes.length && this.selectedOptionGroup?.get('Required')?.value && values.length) {
            this.previewSelectedIndexes = [0];
        }
    }

    setOptionDataType(type: string) {
        const group = this.selectedOptionGroup;
        if (!group) return;
        group.get('Multiple')?.setValue(type === 'checkbox');
        if (type === 'selectbox') {
            let firstDefaultFound = false;
            this.optionValues(this.selectedOptionGroupIndex).controls.forEach(value => {
                if (value.get('IsDefault')?.value) {
                    value.get('IsDefault')?.setValue(!firstDefaultFound);
                    firstDefaultFound = true;
                }
            });
        }
        this.selectOptionGroup(this.selectedOptionGroupIndex);
    }

    selectPreviewValue(index: number, checked: boolean) {
        const multiple = this.selectedOptionGroup?.get('Multiple')?.value;
        this.previewSelectedIndexes = multiple
            ? (checked ? [...this.previewSelectedIndexes, index] : this.previewSelectedIndexes.filter(i => i !== index))
            : (index >= 0 ? [index] : []);
    }

    setDefaultValue(index: number) {
        const group = this.selectedOptionGroup;
        if (!group) return;
        if (!group.get('Multiple')?.value) {
            this.optionValues(this.selectedOptionGroupIndex).controls.forEach((value, i) =>
                value.get('IsDefault')?.setValue(i === index));
        } else {
            const control = this.optionValues(this.selectedOptionGroupIndex).at(index).get('IsDefault');
            control?.setValue(!control.value);
        }
        this.selectOptionGroup(this.selectedOptionGroupIndex);
    }

    get previewValues(): any[] {
        if (!this.selectedOptionGroup) return [];
        return this.previewSelectedIndexes
            .map(index => this.optionValues(this.selectedOptionGroupIndex).at(index)?.value)
            .filter(Boolean);
    }

    get previewExtraCharge(): number {
        return this.previewValues.reduce((sum, value) => sum + Number(value.ExtraCharge || 0), 0);
    }

    get previewTaxRate(): number {
        const taxRateId = this.form.get('TaxRateId')?.value;
        return Number(this.taxRateOptions().find(x => x.Id?.toString().toLowerCase() === taxRateId?.toString().toLowerCase())?.Rate || 0);
    }

    get previewUnitPrice(): number {
        return Number(this.form.get('SalePrice')?.value || 0) + this.previewExtraCharge;
    }

    get previewTaxAmount(): number {
        const fixedTax = Number(this.form.get('FixedTaxAmount')?.value || 0);
        const taxRate = this.previewTaxRate;
        const extraTax = this.previewValues.reduce((sum, val) => sum + this.optionValueTax(val), 0);
        if (fixedTax > 0) {
            return fixedTax + extraTax;
        }
        const baseTax = taxRate > 0 ? Math.round(Number(this.form.get('SalePrice')?.value || 0) * taxRate / 100) : 0;
        return baseTax + extraTax;
    }

    optionValueTax(value: any): number {
        const extra = Number(value?.get ? value.get('ExtraCharge')?.value : value?.ExtraCharge || 0);
        const taxRate = this.previewTaxRate;
        const percentTax = taxRate > 0 ? Math.sign(extra) * Math.round(Math.abs(extra * taxRate / 100)) : 0;
        const fixedTaxVal = value?.get ? value.get('FixedTax')?.value : value?.FixedTax;
        const fixedTax = (fixedTaxVal !== null && fixedTaxVal !== undefined && fixedTaxVal !== '') ? (Number(fixedTaxVal) || 0) : 0;
        return fixedTax !== 0 ? fixedTax : percentTax;
    }

    optionValueTotal(value: any): number {
        const extra = Number(value?.get ? value.get('ExtraCharge')?.value : value?.ExtraCharge || 0);
        return extra + this.optionValueTax(value);
    }

    resetEmptyOptionAmount(value: any, field: 'ExtraCharge' | 'FixedTax') {
        const control = value.get(field);
        if (control?.value === null || control?.value === '') {
            control.setValue(0);
        }
    }

    get previewTotal(): number {
        return this.previewUnitPrice + this.previewTaxAmount;
    }

    saveOptionEditor() {
        if (this.optionGroups.invalid) {
            this.optionGroups.markAllAsTouched();
            this.messageService.add({ severity: 'warn', summary: 'Vui lòng nhập đầy đủ mã và tên tùy chọn.' });
            return;
        }
        this.optionEditorApplied = true;
        this.optionEditorVisible = false;
    }

    itemTypeOptions = [
        { value: 1, label: 'Món ăn' },
        { value: 2, label: 'Đồ uống' },
        { value: 3, label: 'Combo' },
        { value: 4, label: 'Khác' }
    ];

    courseOrderOptions = [
        { value: 1, label: 'Món khai vị' },
        { value: 2, label: 'Món chính' },
        { value: 3, label: 'Món tráng miệng' }
    ];

    statusOptions = [
        { value: 1, label: 'Đang hoạt động' },
        { value: 2, label: 'Ngừng sử dụng' }
    ];

    constructor() {
        super();
        this.form = this.fb.group({
            Code: [null, Validators.required],
            Name: [null, Validators.required],
            MenuGroupIds: [[], Validators.required],
            ItemType: [1, Validators.required],
            CourseOrder: [1, [Validators.required, Validators.min(0)]],
            SalePrice: [null, Validators.required],
            CostPrice: [null],
            UnitId: [null, Validators.required],
            TaxRateId: [null],
            FixedTaxAmount: [0],
            Amount: [0],
            Description: [null],
            Status: [1, Validators.required],
            IsAvailable: [true],
            IsMustTry: [false],
            IsTrackStock: [false],
            StockQuantity: [null],
            DailyQuota: [null],
            ImageUrl: [null],
            BranchIds: [[]],
            PreparationAreaId: [null],
            ServicePreferences: this.fb.array([]),
            OptionGroups: this.fb.array([])
        });
    }

    get servicePreferences() {
        return this.form.get('ServicePreferences') as FormArray;
    }

    get optionGroups() {
        return this.form.get('OptionGroups') as FormArray;
    }

    optionValues(index: number): FormArray {
        return this.optionGroups.at(index).get('Values') as FormArray;
    }

    addOptionGroup(group?: any) {
        const values = this.fb.array<any>([]);
        const formGroup = this.fb.group({
            Code: [group?.Code ?? '', Validators.required],
            Name: [group?.Name ?? '', Validators.required],
            Required: [group?.Required ?? false],
            Multiple: [group?.Multiple ?? false],
            Values: values
        });
        this.optionGroups.push(formGroup);
        (group?.Values?.length ? group.Values : [null]).forEach((value: any) => this.addOptionValue(this.optionGroups.length - 1, value));
        this.selectOptionGroup(this.optionGroups.length - 1);
    }

    addOptionValue(groupIndex: number, value?: any) {
        this.optionValues(groupIndex).push(this.fb.group({
            Code: [value?.Code ?? '', Validators.required],
            Name: [value?.Name ?? '', Validators.required],
            ExtraCharge: [value?.ExtraCharge ?? 0, Validators.required],
            FixedTax: [value?.FixedTax !== null && value?.FixedTax !== undefined && value?.FixedTax !== '' ? value.FixedTax : null],
            IsDefault: [value?.IsDefault ?? false]
        }));
    }

    removeOptionGroup(index: number) {
        this.optionGroups.removeAt(index);
        this.selectOptionGroup(Math.min(index, Math.max(0, this.optionGroups.length - 1)));
    }
    removeOptionValue(groupIndex: number, valueIndex: number) {
        this.optionValues(groupIndex).removeAt(valueIndex);
        this.selectOptionGroup(groupIndex);
    }

    addServicePreference(prefId: string = '', charge: number = 0) {
        this.servicePreferences.push(this.fb.group({
            ServicePreferenceId: [prefId || null, Validators.required],
            ExtraCharge: [charge || 0, [Validators.required, Validators.min(0)]]
        }));
    }

    removeServicePreference(index: number) {
        this.servicePreferences.removeAt(index);
    }

    ngOnInit(): void {
        this.menugroupService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            const items = this.getResponseItems(res) || [];
            this.menuGroupOptions.set(items.filter((x: any) => Number(x.Status) === 1).map((x: any) => ({
                ...x,
                Name: `${x.Code} - ${x.Name}`
            })));
        });
        this.unitService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            const items = this.getResponseItems(res) || [];
            this.unitOptions.set(items.map((x: any) => ({
                ...x,
                Name: `${x.Code} - ${x.Name}`
            })));
        });
        this.branchService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.branchOptions.set(this.getResponseItems(res));
        });
        this.preparationAreaService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.preparationAreaOptions.set(this.getResponseItems(res));
        });
        this.servicePreferenceService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            this.servicePreferenceOptions.set(this.getResponseItems(res));
        });
        this.taxrateService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            const rawItems = this.getResponseItems(res) || [];
            this.taxRateOptions.set(
                rawItems.map((item: any) => ({
                    ...item,
                    Name: `${item.Code} - ${item.Rate}%`
                }))
            );
            this.updateExpectedTotalPrice();
        });

        this.form.valueChanges.subscribe(() => {
            this.updateExpectedTotalPrice();
        });

        this.form.get('StockQuantity')?.valueChanges.subscribe((qty: any) => {
            if (this.form.get('IsTrackStock')?.value) {
                if (qty !== null && qty !== undefined && qty !== '' && Number(qty) <= 0) {
                    this.form.get('IsAvailable')?.setValue(false, { emitEvent: false });
                } else if (qty !== null && qty !== undefined && qty !== '' && Number(qty) > 0) {
                    this.form.get('IsAvailable')?.setValue(true, { emitEvent: false });
                }
            }
        });

        this.form.get('DailyQuota')?.valueChanges.subscribe((quota: any) => {
            const currentStock = this.form.get('StockQuantity')?.value;
            if (quota !== null && quota !== undefined && quota !== '' && (currentStock === null || currentStock === undefined || currentStock === '')) {
                this.form.get('StockQuantity')?.setValue(quota);
            }
        });

        this.form.get('IsTrackStock')?.valueChanges.subscribe((isTrack: boolean) => {
            if (isTrack) {
                const stock = this.form.get('StockQuantity')?.value;
                if (stock !== null && stock !== undefined && stock !== '' && Number(stock) <= 0) {
                    this.form.get('IsAvailable')?.setValue(false, { emitEvent: false });
                }
            }
        });

        if (this.config?.data?.edition) {
            const data = this.config.data.data;
            this.edition.set(true);
            this.form.patchValue({
                Code: data.Code,
                Name: data.Name,
                MenuGroupIds: data.MenuGroupIds?.length ? data.MenuGroupIds : (data.MenuGroupId ? [data.MenuGroupId] : []),
                ItemType: data.ItemType,
                CourseOrder: data.CourseOrder,
                SalePrice: data.SalePrice,
                CostPrice: data.CostPrice,
                UnitId: data.UnitId,
                TaxRateId: data.TaxRateId,
                FixedTaxAmount: data.FixedTaxAmount || 0,
                Amount: data.Amount || 0,
                Description: data.Description,
                Status: data.Status,
                IsAvailable: data.IsAvailable !== false,
                IsMustTry: data.IsMustTry === true,
                IsTrackStock: data.IsTrackStock === true,
                StockQuantity: data.StockQuantity,
                DailyQuota: data.DailyQuota,
                ImageUrl: data.ImageUrl,
                BranchIds: data.BranchIds || [],
                PreparationAreaId: data.PreparationAreaId
            });
            this.form.get('Code')?.disable();

            if (data.ServicePreferences && Array.isArray(data.ServicePreferences)) {
                this.servicePreferences.clear();
                data.ServicePreferences.forEach((pref: any) => {
                    this.addServicePreference(pref.ServicePreferenceId, pref.ExtraCharge);
                });
            }
            this.optionGroups.clear();
            (data.OptionGroups || []).forEach((group: any) => this.addOptionGroup(group));
        }
    }

    updateExpectedTotalPrice() {
        const salePrice = Number(this.form.get('SalePrice')?.value) || 0;
        const fixedTax = Number(this.form.get('FixedTaxAmount')?.value) || 0;
        const taxRateId = this.form.get('TaxRateId')?.value;
        
        let taxPercent = 0;
        if (taxRateId) {
            const tax = this.taxRateOptions().find(t => t.Id?.toString().toLowerCase() === taxRateId.toString().toLowerCase());
            if (tax) {
                taxPercent = Number(tax.Rate) || 0;
            }
        }
        
        let total = 0;
        if (fixedTax > 0) {
            total = salePrice + fixedTax;
        } else {
            total = salePrice * (1 + taxPercent / 100);
        }
        
        this.expectedTotalPrice.set(total);
        this.form.get('Amount')?.setValue(total, { emitEvent: false });
    }

    onImagesUploaded(files: File[]) {
        this.imageFile.set(files);
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const file = this.imageFile();
        const upload$ = file
            ? this.minioService.upload(file[0]).pipe(
                  map((res) => res?.Data?.ObjectName),
                  catchError(() => {
                      this.messageService.add({
                          severity: 'warn',
                          summary: 'Tải ảnh thất bại, vui lòng thử lại!'
                      });
                      return of(undefined);
                  })
              )
            : of(undefined);

        upload$
            .pipe(
                switchMap((image) => {
                    const rawValue = this.form.getRawValue();
                    const payload = {
                        ...(this.edition() ? { Id: this.config?.data.data.Id } : {}),
                        ...rawValue,
                        MenuGroupId: this.edition() && rawValue.MenuGroupIds?.includes(this.config?.data.data.MenuGroupId)
                            ? this.config?.data?.data?.MenuGroupId : rawValue.MenuGroupIds?.[0],
                        SalePrice: rawValue.SalePrice != null && rawValue.SalePrice !== '' ? Number(rawValue.SalePrice) : 0,
                        CostPrice: rawValue.CostPrice != null && rawValue.CostPrice !== '' ? Number(rawValue.CostPrice) : 0,
                        FixedTaxAmount: rawValue.FixedTaxAmount != null && rawValue.FixedTaxAmount !== '' ? Number(rawValue.FixedTaxAmount) : 0,
                        Amount: rawValue.Amount != null && rawValue.Amount !== '' ? Number(rawValue.Amount) : 0,
                        IsTrackStock: !!rawValue.IsTrackStock,
                        StockQuantity: rawValue.IsTrackStock && rawValue.StockQuantity != null && rawValue.StockQuantity !== '' ? Number(rawValue.StockQuantity) : null,
                        DailyQuota: rawValue.IsTrackStock && rawValue.DailyQuota != null && rawValue.DailyQuota !== '' ? Number(rawValue.DailyQuota) : null,
                        OptionGroups: (rawValue.OptionGroups || []).map((group: any) => ({
                            ...group,
                            Values: (group.Values || []).map((val: any) => ({
                                ...val,
                                ExtraCharge: val.ExtraCharge != null && val.ExtraCharge !== '' ? Number(val.ExtraCharge) : 0,
                                FixedTax: val.FixedTax !== null && val.FixedTax !== undefined && val.FixedTax !== '' ? Number(val.FixedTax) : null
                            }))
                        })),
                        ImageUrl: image ? image : rawValue.ImageUrl ?? ''
                    };
                    return this.edition() ? this.mainService.update(payload) : this.mainService.create(payload);
                })
            )
            .subscribe((res: any) => {
                if (res.Code === 200) {
                    this.onSuccess();
                } else {
                    this.messageService.add({
                        severity: 'error',
                        summary: res.Message || 'Có lỗi xảy ra!'
                    });
                }
            });
    }

    private onSuccess() {
        this.messageService.add({
            severity: 'info',
            summary: 'Thành công!'
        });

        setTimeout(() => {
            this.dialogRef?.close(true);
        }, 500);
    }
}
