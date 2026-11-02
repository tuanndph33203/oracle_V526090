using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.TaxRateBussiness
{
    public class TaxRateModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public decimal Rate { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class TaxRateCreateModel
    {
        public string Code { get; set; } = default!;
        public decimal Rate { get; set; }
        public string? Description { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class TaxRateUpdateModel : TaxRateModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class TaxRateFilterModel : FilterModel
    {
    }
}