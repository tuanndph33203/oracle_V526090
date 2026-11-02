export interface Promotion {
    Id?: string;
    Code: string;
    Name: string;
    Description?: string;
    StartDate: Date | string;
    EndDate: Date | string;
    PromotionType: number; // 1 = Discount, 2 = GiftItem
    IsActive: boolean;
    RequireTherapyVoucher?: boolean;
    ApplyOnlyToQrOrders?: boolean;

    BranchIds?: string[];
    CustomerGroupIds?: string[];
    DaysOfWeek?: number[];
    TimeRanges?: PromotionTimeRange[];

    DiscountConfig?: PromotionDiscountConfig;
    DiscountItems?: PromotionDiscountItem[];

    GiftConditions?: PromotionGiftCondition[];
    GiftItems?: PromotionGiftItem[];
}

export interface PromotionTimeRange {
    StartTime: string; // HH:mm
    EndTime: string;   // HH:mm
}

export interface PromotionDiscountConfig {
    DiscountScope: number; // 1 = Order, 2 = Item
    DiscountPercent: number;
    DiscountAmount: number;
    QrOrderDiscountAmount?: number;
    MinOrderAmount: number;
}

export interface PromotionDiscountItem {
    MenuItemId: string;
    MinAmount: number;
    MinQuantity: number;
}

export interface PromotionGiftCondition {
    MenuItemId: string;
    RequiredQuantity: number;
}

export interface PromotionGiftItem {
    MenuItemId: string;
    GiftQuantity: number;
    GiftValue: number;
}
