using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.MenuItemBussiness
{
    public class MenuOptionValueModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal ExtraCharge { get; set; }
        public decimal? FixedTax { get; set; }
        public bool IsDefault { get; set; }
    }

    public class MenuOptionGroupModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool Required { get; set; }
        public bool Multiple { get; set; }
        public List<MenuOptionValueModel> Values { get; set; } = new();
    }

    public class MenuItemServicePreferenceModel
    {
        public Guid ServicePreferenceId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal ExtraCharge { get; set; }
    }

    public class MenuItemDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int ItemType { get; set; }
        public int CourseOrder { get; set; }
        public Guid? MenuGroupId { get; set; }
        public List<Guid> MenuGroupIds { get; set; } = new List<Guid>();
        public string? MenuGroupName { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal SalePrice { get; set; }
        public Guid? TaxRateId { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal FixedTaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal CostPrice { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? PreparationAreaId { get; set; }
        public int Status { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsMustTry { get; set; } = false;
        public bool IsTrackStock { get; set; } = false;
        public int? StockQuantity { get; set; }
        public int? DailyQuota { get; set; }
        public DateTime? DisabledAt { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public List<MenuItemServicePreferenceModel> ServicePreferences { get; set; } = new List<MenuItemServicePreferenceModel>();
        public List<MenuOptionGroupModel> OptionGroups { get; set; } = new();
    }

    public class MenuItemCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int ItemType { get; set; }
        public int CourseOrder { get; set; }
        public Guid? MenuGroupId { get; set; }
        public List<Guid>? MenuGroupIds { get; set; }
        public Guid? UnitId { get; set; }
        public decimal SalePrice { get; set; }
        public Guid? TaxRateId { get; set; }
        public decimal FixedTaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal CostPrice { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? PreparationAreaId { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsMustTry { get; set; } = false;
        public bool IsTrackStock { get; set; } = false;
        public int? StockQuantity { get; set; }
        public int? DailyQuota { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public List<MenuItemServicePreferenceModel> ServicePreferences { get; set; } = new List<MenuItemServicePreferenceModel>();
        public List<MenuOptionGroupModel>? OptionGroups { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class MenuItemUpdateModel : MenuItemCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ToggleMenuItemAvailabilityModel
    {
        public Guid Id { get; set; }
        public bool IsAvailable { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class MenuItemFilterModel : FilterModel
    {
        public List<Guid>? Ids { get; set; }
        public List<Guid>? MenuGroupIds { get; set; }
        public int? ItemType { get; set; }
        public int? CourseOrder { get; set; }
        public Guid? MenuGroupId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? PreparationAreaId { get; set; }
        public Guid? BranchId { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
