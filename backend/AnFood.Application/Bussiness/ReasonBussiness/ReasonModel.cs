using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.ReasonBussiness
{
    public class ReasonModel
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Status { get; set; }
    }

    public class ReasonCreateModel
    {
        public int Type { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class ReasonUpdateModel : ReasonModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ReasonFilterModel : FilterModel
    {
        public int? Type { get; set; }
    }
}