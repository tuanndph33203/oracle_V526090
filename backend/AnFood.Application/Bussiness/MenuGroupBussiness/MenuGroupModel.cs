using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.MenuGroupBussiness
{
    public class MenuGroupModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Type { get; set; }
        public int? Priority { get; set; }
        public Guid? PreparationAreaId { get; set; }
        public string? PreparationAreaName { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class MenuGroupCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Type { get; set; }
        public int? Priority { get; set; }
        public Guid? PreparationAreaId { get; set; }
        public string? Description { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class MenuGroupUpdateModel : MenuGroupModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class MenuGroupFilterModel : FilterModel
    {
        public int? Type { get; set; }
        public Guid? PreparationAreaId { get; set; }
    }
}