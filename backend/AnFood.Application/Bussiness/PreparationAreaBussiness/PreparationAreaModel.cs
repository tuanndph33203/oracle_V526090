using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.PreparationAreaBussiness
{
    public class PreparationAreaDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public int Type { get; set; }
        public string Name { get; set; } = default!;
        public int DeviceType { get; set; }
        public int? LabelPrintTiming { get; set; }
        public string? Description { get; set; }
        public string? ProcessingArea { get; set; }
        public int Status { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
    }

    public class PreparationAreaCreateModel
    {
        public string Code { get; set; } = default!;
        public int Type { get; set; }
        public string Name { get; set; } = default!;
        public int DeviceType { get; set; }
        public int? LabelPrintTiming { get; set; }
        public string? Description { get; set; }
        public string? ProcessingArea { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class PreparationAreaUpdateModel : PreparationAreaCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class PreparationAreaFilterModel : FilterModel
    {
        public int? Type { get; set; }
        public Guid? BranchId { get; set; }
    }
}