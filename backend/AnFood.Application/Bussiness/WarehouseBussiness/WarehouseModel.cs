using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.WarehouseBussiness
{
    public class WarehouseDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
    }

    public class WarehouseCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public string? Description { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class WarehouseUpdateModel : WarehouseCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class WarehouseFilterModel : FilterModel
    {
        public Guid? ParentId { get; set; }
        public Guid? BranchId { get; set; }
    }
}