using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.FloorPlanBussiness
{
    public class FloorPlanDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class FloorPlanCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid BranchId { get; set; }
        public string? Description { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class FloorPlanUpdateModel : FloorPlanDetailModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class FloorPlanFilterModel : FilterModel
    {
        public Guid? BranchId { get; set; }
    }
}