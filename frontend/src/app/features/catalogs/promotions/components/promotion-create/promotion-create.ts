import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators, FormBuilder, FormArray, FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { PromotionService } from '../../services/promotion';
import { BranchService } from '../../../branch/services/branch';
import { BusinessPartnerGroupService } from '../../../business-partner-group/services/business-partner-group';
import { MenuItemService } from '../../../menu-item/services/menu-item';
import { Promotion, PromotionDiscountItem, PromotionGiftCondition, PromotionGiftItem } from '../../models/promotion';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-promotion-create',
    imports: [CommonModule, ReactiveFormsModule, FormsModule, InputTextModule, ButtonModule, DialogWrap, DatePickerModule, SelectModule, MultiSelectModule, CheckboxModule, InputNumberModule],
    templateUrl: './promotion-create.html',
    styleUrl: './promotion-create.scss'
})
export class PromotionCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;

    private promotionService = inject(PromotionService);
    private branchService = inject(BranchService);
    private groupService = inject(BusinessPartnerGroupService);
    private menuItemService = inject(MenuItemService);

    // Dynamic Options Data
    branches = signal<any[]>([]);
    customerGroups = signal<any[]>([]);
    menuItems = signal<any[]>([]);

    // Days of week choices
    weekDays = [
        { label: 'Chủ nhật', value: 0 },
        { label: 'Thứ hai', value: 1 },
        { label: 'Thứ ba', value: 2 },
        { label: 'Thứ tư', value: 3 },
        { label: 'Thứ năm', value: 4 },
        { label: 'Thứ sáu', value: 5 },
        { label: 'Thứ bảy', value: 6 }
    ];

    // Promotion types
    promotionTypes = [
        { label: 'Giảm giá', value: 1 },
        { label: 'Tặng món', value: 2 }
    ];

    // Discount scopes
    discountScopes = [
        { label: 'Đơn hàng', value: 1 },
        { label: 'Món ăn', value: 2 }
    ];

    // Local selected items tables state
    selectedDiscountItems = signal<any[]>([]);
    selectedGiftConditions = signal<any[]>([]);
    selectedGiftItems = signal<any[]>([]);

    // Temp selection dropdows values
    tempDiscountItemId: string | null = null;
    tempGiftConditionId: string | null = null;
    tempGiftItemId: string | null = null;

    constructor() {
        super();
        this.form = this.fb.group({
            Code: ['', [Validators.required]],
            Name: ['', [Validators.required]],
            Description: [''],
            StartDate: [null, [Validators.required]],
            EndDate: [null, [Validators.required]],
            PromotionType: [1, [Validators.required]],
            IsActive: [true],
            RequireTherapyVoucher: [false],
            ApplyOnlyToQrOrders: [false],

            BranchIds: [[], [Validators.required]],
            CustomerGroupIds: [[]],
            DaysOfWeek: [[]],
            StartTime: ['00:00', [Validators.required]],
            EndTime: ['23:59', [Validators.required]],

            // Discount config
            DiscountScope: [1],
            DiscountPercent: [0],
            DiscountAmount: [0],
            QrOrderDiscountAmount: [0],
            MinOrderAmount: [0]
        });
    }

    ngOnInit(): void {
        this.loadMetadataAndDetail();

        // Theo dõi sự thay đổi của PromotionType để validate
        this.form.get('PromotionType')?.valueChanges.subscribe((type) => {
            if (type === 1) {
                this.form.get('DiscountScope')?.setValidators([Validators.required]);
            } else {
                this.form.get('DiscountScope')?.clearValidators();
            }
            this.form.get('DiscountScope')?.updateValueAndValidity();
        });
    }

    private loadMetadataAndDetail() {
        forkJoin({
            branches: this.branchService.getByFilter({ Page: 1, Size: 1000 }),
            groups: this.groupService.getByFilter({ Page: 1, Size: 1000 }),
            menuItems: this.menuItemService.getByFilter({ Page: 1, Size: 1000 })
        }).subscribe((res: any) => {
            const branchList = res.branches?.Data?.Content || res.branches?.data?.content || [];
            this.branches.set(branchList);

            const groupList = res.groups?.Data?.Content || res.groups?.data?.content || [];
            this.customerGroups.set(groupList.filter((x: any) => x.GroupTypes === 1));

            const itemList = res.menuItems?.Data?.Content || res.menuItems?.data?.content || [];
            this.menuItems.set(itemList.filter((x: any) => x.Status === 1));

            if (this.config?.data?.edition) {
                this.edition.set(true);
                const promoId = this.config.data.data.Id;
                this.loadPromotionDetail(promoId);
            }
        });
    }

    private loadPromotionDetail(id: string) {
        this.promotionService.getById(id).subscribe((res: any) => {
            if (res.Code === 200 && res.Data) {
                const data = res.Data;
                this.form.patchValue({
                    Code: data.Code,
                    Name: data.Name,
                    Description: data.Description,
                    StartDate: data.StartDate ? new Date(data.StartDate) : null,
                    EndDate: data.EndDate ? new Date(data.EndDate) : null,
                    PromotionType: data.PromotionType,
                    IsActive: data.IsActive,
                    RequireTherapyVoucher: data.RequireTherapyVoucher ?? false,
                    ApplyOnlyToQrOrders: data.ApplyOnlyToQrOrders ?? false,
                    QrOrderDiscountAmount: data.DiscountConfig?.QrOrderDiscountAmount ?? 0,
                    BranchIds: data.BranchIds || [],
                    CustomerGroupIds: data.CustomerGroupIds || [],
                    DaysOfWeek: data.DaysOfWeek || [],
                    StartTime: data.TimeRanges?.[0]?.StartTime || '00:00',
                    EndTime: data.TimeRanges?.[0]?.EndTime || '23:59'
                });

                if (data.PromotionType === 1 && data.DiscountConfig) {
                    this.form.patchValue({
                        DiscountScope: data.DiscountConfig.DiscountScope,
                        DiscountPercent: data.DiscountConfig.DiscountPercent,
                        DiscountAmount: data.DiscountConfig.DiscountAmount,
                        MinOrderAmount: data.DiscountConfig.MinOrderAmount
                    });

                    if (data.DiscountConfig.DiscountScope === 2 && data.DiscountItems) {
                        this.selectedDiscountItems.set(data.DiscountItems.map((x: any) => ({
                            MenuItemId: x.MenuItemId,
                            MenuItemName: this.getMenuItemName(x.MenuItemId),
                            MenuItemCode: this.getMenuItemCode(x.MenuItemId),
                            MinAmount: x.MinAmount,
                            MinQuantity: x.MinQuantity
                        })));
                    }
                } else if (data.PromotionType === 2) {
                    if (data.GiftConditions) {
                        this.selectedGiftConditions.set(data.GiftConditions.map((x: any) => ({
                            MenuItemId: x.MenuItemId,
                            MenuItemName: this.getMenuItemName(x.MenuItemId),
                            MenuItemCode: this.getMenuItemCode(x.MenuItemId),
                            RequiredQuantity: x.RequiredQuantity
                        })));
                    }
                    if (data.GiftItems) {
                        this.selectedGiftItems.set(data.GiftItems.map((x: any) => ({
                            MenuItemId: x.MenuItemId,
                            MenuItemName: this.getMenuItemName(x.MenuItemId),
                            MenuItemCode: this.getMenuItemCode(x.MenuItemId),
                            GiftQuantity: x.GiftQuantity,
                            GiftValue: x.GiftValue
                        })));
                    }
                }
            }
        });
    }

    getMenuItemName(id: string): string {
        const item = this.menuItems().find((x) => x.Id === id);
        return item ? item.Name : 'Món ăn không xác định';
    }

    getMenuItemCode(id: string): string {
        const item = this.menuItems().find((x) => x.Id === id);
        return item ? item.Code : '';
    }

    // ── DISCOUNT ITEMS MANAGEMENT ────────────────────────────────
    addDiscountItem() {
        if (!this.tempDiscountItemId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món ăn cần thêm!' });
            return;
        }
        const selectedId = typeof this.tempDiscountItemId === 'object' 
            ? (this.tempDiscountItemId as any)?.Id 
            : this.tempDiscountItemId;

        if (!selectedId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món ăn hợp lệ!' });
            return;
        }

        const exists = this.selectedDiscountItems().some(x => x.MenuItemId === selectedId);
        if (exists) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Món ăn này đã có trong danh sách khuyến mại!' });
            return;
        }

        const menuItem = this.menuItems().find(x => x.Id === selectedId);
        if (!menuItem) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không tìm thấy thông tin món ăn được chọn!' });
            return;
        }

        this.selectedDiscountItems.update(list => [...list, {
            MenuItemId: menuItem.Id,
            MenuItemName: menuItem.Name,
            MenuItemCode: menuItem.Code,
            MinAmount: 0,
            MinQuantity: 1
        }]);
        this.tempDiscountItemId = null;
    }

    removeDiscountItem(id: string) {
        this.selectedDiscountItems.update(list => list.filter(x => x.MenuItemId !== id));
    }

    // ── GIFT CONDITIONS MANAGEMENT ───────────────────────────────
    addGiftCondition() {
        if (!this.tempGiftConditionId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món ăn điều kiện mua!' });
            return;
        }
        const selectedId = typeof this.tempGiftConditionId === 'object' 
            ? (this.tempGiftConditionId as any)?.Id 
            : this.tempGiftConditionId;

        if (!selectedId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món ăn hợp lệ!' });
            return;
        }

        const exists = this.selectedGiftConditions().some(x => x.MenuItemId === selectedId);
        if (exists) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Món ăn này đã có trong danh sách điều kiện!' });
            return;
        }

        const menuItem = this.menuItems().find(x => x.Id === selectedId);
        if (!menuItem) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không tìm thấy thông tin món ăn được chọn!' });
            return;
        }

        this.selectedGiftConditions.update(list => [...list, {
            MenuItemId: menuItem.Id,
            MenuItemName: menuItem.Name,
            MenuItemCode: menuItem.Code,
            RequiredQuantity: 1
        }]);
        this.tempGiftConditionId = null;
    }

    removeGiftCondition(id: string) {
        this.selectedGiftConditions.update(list => list.filter(x => x.MenuItemId !== id));
    }

    // ── GIFT ITEMS MANAGEMENT ────────────────────────────────────
    addGiftItem() {
        if (!this.tempGiftItemId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món được tặng!' });
            return;
        }
        const selectedId = typeof this.tempGiftItemId === 'object' 
            ? (this.tempGiftItemId as any)?.Id 
            : this.tempGiftItemId;

        if (!selectedId) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng chọn món ăn hợp lệ!' });
            return;
        }

        const exists = this.selectedGiftItems().some(x => x.MenuItemId === selectedId);
        if (exists) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Món ăn này đã có trong danh sách tặng!' });
            return;
        }

        const menuItem = this.menuItems().find(x => x.Id === selectedId);
        if (!menuItem) {
            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không tìm thấy thông tin món ăn được chọn!' });
            return;
        }

        this.selectedGiftItems.update(list => [...list, {
            MenuItemId: menuItem.Id,
            MenuItemName: menuItem.Name,
            MenuItemCode: menuItem.Code,
            GiftQuantity: 1,
            GiftValue: menuItem.SalePrice
        }]);
        this.tempGiftItemId = null;
    }

    removeGiftItem(id: string) {
        this.selectedGiftItems.update(list => list.filter(x => x.MenuItemId !== id));
    }

    // ── SUBMIT / CANCEL ──────────────────────────────────────────

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            this.messageService.add({
                severity: 'error',
                summary: 'Lỗi nhập liệu',
                detail: 'Vui lòng kiểm tra lại các trường thông tin bắt buộc!'
            });
            return;
        }

        const raw = this.form.value;
        const promoType = raw.PromotionType;

        // Custom validation
        if (promoType === 1) { // Giảm giá
            if (raw.DiscountScope === 2 && this.selectedDiscountItems().length === 0) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Vui lòng chọn ít nhất một món ăn được giảm giá!' });
                return;
            }
            if (raw.DiscountPercent === 0 && raw.DiscountAmount === 0 && raw.QrOrderDiscountAmount === 0) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Vui lòng nhập phần trăm giảm hoặc số tiền giảm giá lớn hơn 0!' });
                return;
            }
        } else if (promoType === 2) { // Tặng món
            if (this.selectedGiftConditions().length === 0) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Vui lòng chọn ít nhất một món ăn điều kiện mua!' });
                return;
            }
            if (this.selectedGiftItems().length === 0) {
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Vui lòng chọn ít nhất một món ăn được tặng!' });
                return;
            }
        }

        // Xây dựng Payload
        const payload: Promotion = {
            Code: raw.Code,
            Name: raw.Name,
            Description: raw.Description,
            StartDate: raw.StartDate,
            EndDate: raw.EndDate,
            PromotionType: promoType,
            IsActive: raw.IsActive,
            RequireTherapyVoucher: raw.RequireTherapyVoucher,
            ApplyOnlyToQrOrders: raw.ApplyOnlyToQrOrders,
            BranchIds: raw.BranchIds,
            CustomerGroupIds: raw.CustomerGroupIds,
            DaysOfWeek: raw.DaysOfWeek,
            TimeRanges: [{
                StartTime: raw.StartTime,
                EndTime: raw.EndTime
            }]
        };

        if (this.edition()) {
            payload.Id = this.config?.data?.data?.Id;
        }

        if (promoType === 1) {
            payload.DiscountConfig = {
                DiscountScope: raw.DiscountScope,
                DiscountPercent: raw.DiscountPercent || 0,
                DiscountAmount: raw.DiscountAmount || 0,
                QrOrderDiscountAmount: raw.QrOrderDiscountAmount || 0,
                MinOrderAmount: raw.MinOrderAmount || 0
            };
            if (raw.DiscountScope === 2) {
                payload.DiscountItems = this.selectedDiscountItems().map(x => ({
                    MenuItemId: x.MenuItemId,
                    MinAmount: x.MinAmount || 0,
                    MinQuantity: x.MinQuantity || 0
                }));
            }
        } else {
            payload.GiftConditions = this.selectedGiftConditions().map(x => ({
                MenuItemId: x.MenuItemId,
                RequiredQuantity: x.RequiredQuantity || 1
            }));
            payload.GiftItems = this.selectedGiftItems().map(x => ({
                MenuItemId: x.MenuItemId,
                GiftQuantity: x.GiftQuantity || 1,
                GiftValue: x.GiftValue || 0
            }));
        }

        const request$ = this.edition() ? this.promotionService.update(payload) : this.promotionService.create(payload);
        request$.subscribe((res) => {
            if (res.Code === 200) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Thành công',
                    detail: this.edition() ? 'Cập nhật chương trình khuyến mại thành công!' : 'Tạo chương trình khuyến mại thành công!'
                });
                setTimeout(() => {
                    this.dialogRef?.close(true);
                }, 500);
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Lỗi',
                    detail: res.Message || 'Có lỗi xảy ra khi lưu chương trình khuyến mại!'
                });
            }
        });
    }
}
