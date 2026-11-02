using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.ServicePreferenceBussiness
{
    public class ServicePreferenceModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal DefaultExtraCharge { get; set; }
        public int Status { get; set; }
    }

    public class ServicePreferenceCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal DefaultExtraCharge { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class ServicePreferenceUpdateModel : ServicePreferenceModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ServicePreferenceFilterModel : FilterModel
    {
    }
}