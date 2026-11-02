using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.MaterialGroupBussiness
{
    public class MaterialGroupModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class MaterialGroupCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public string? Description { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class MaterialGroupUpdateModel : MaterialGroupModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class MaterialGroupFilterModel : FilterModel
    {
        public Guid? ParentId { get; set; }
    }
}