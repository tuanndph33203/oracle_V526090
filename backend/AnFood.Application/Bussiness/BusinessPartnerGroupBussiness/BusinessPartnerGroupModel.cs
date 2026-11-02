using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.BusinessPartnerGroupBussiness
{
    public class BusinessPartnerGroupModel
    {
        public Guid Id { get; set; }
        public int GroupTypes { get; set; } // 1 = CustomerGroup, 2 = SupplierGroup, 3 = Both
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class BusinessPartnerGroupCreateModel
    {
        public int GroupTypes { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class BusinessPartnerGroupUpdateModel : BusinessPartnerGroupModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class BusinessPartnerGroupFilterModel : FilterModel
    {
        public int? GroupTypes { get; set; }
        public int? Status { get; set; }
    }
}