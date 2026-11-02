using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.PromotionBussiness
{
    public class PromotionCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PromotionType { get; set; } // 1 = Discount, 2 = GiftItem

        // Links
        public List<Guid> BranchIds { get; set; } = new();
        public List<Guid> CustomerGroupIds { get; set; } = new();
        public List<int> DaysOfWeek { get; set; } = new(); // 0 = Sunday, 1 = Monday...
        public List<PromotionTimeRangeModel> TimeRanges { get; set; } = new();

        public bool RequireTherapyVoucher { get; set; } = false;
        public bool ApplyOnlyToQrOrders { get; set; } = false;

        // Config Discount
        public PromotionDiscountConfigModel? DiscountConfig { get; set; }
        public List<PromotionDiscountItemModel> DiscountItems { get; set; } = new();

        // Config Gift
        public List<PromotionGiftConditionModel> GiftConditions { get; set; } = new();
        public List<PromotionGiftItemModel> GiftItems { get; set; } = new();

        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class PromotionUpdateModel : PromotionCreateModel
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; } = true;
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class PromotionTimeRangeModel
    {
        public string StartTime { get; set; } = default!; // HH:mm
        public string EndTime { get; set; } = default!;   // HH:mm
    }

    public class PromotionDiscountConfigModel
    {
        public int DiscountScope { get; set; } // 1 = Order, 2 = Item
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal QrOrderDiscountAmount { get; set; }
        public decimal MinOrderAmount { get; set; }
    }

    public class PromotionDiscountItemModel
    {
        public Guid MenuItemId { get; set; }
        public decimal MinAmount { get; set; }
        public double MinQuantity { get; set; }
    }

    public class PromotionGiftConditionModel
    {
        public Guid MenuItemId { get; set; }
        public double RequiredQuantity { get; set; }
    }

    public class PromotionGiftItemModel
    {
        public Guid MenuItemId { get; set; }
        public double GiftQuantity { get; set; }
        public decimal GiftValue { get; set; }
    }

    public class PromotionFilterModel
    {
        public string? TextSearch { get; set; }
        public Guid? BranchId { get; set; }
        public int? PromotionType { get; set; }
        public int? Page { get; set; }
        public int? Size { get; set; }
    }

    public class PromotionDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PromotionType { get; set; }
        public bool IsActive { get; set; }
        public bool RequireTherapyVoucher { get; set; }
        public bool ApplyOnlyToQrOrders { get; set; }

        public List<Guid> BranchIds { get; set; } = new();
        public List<Guid> CustomerGroupIds { get; set; } = new();
        public List<int> DaysOfWeek { get; set; } = new();
        public List<PromotionTimeRangeModel> TimeRanges { get; set; } = new();

        public PromotionDiscountConfigModel? DiscountConfig { get; set; }
        public List<PromotionDiscountItemModel> DiscountItems { get; set; } = new();

        public List<PromotionGiftConditionModel> GiftConditions { get; set; } = new();
        public List<PromotionGiftItemModel> GiftItems { get; set; } = new();
    }
}
