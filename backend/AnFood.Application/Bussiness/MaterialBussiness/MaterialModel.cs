using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.MaterialBussiness
{
    public class MaterialDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int PropertyType { get; set; }
        public Guid? MaterialGroupId { get; set; }
        public string? MaterialGroupName { get; set; }
        public Guid UnitId { get; set; }
        public string? UnitName { get; set; }
        public Guid? DefaultWarehouseId { get; set; }
        public string? DefaultWarehouseName { get; set; }
        public int? ShelfLife { get; set; }
        public int? ShelfLifeUnit { get; set; }
        public decimal MinimumStockQuantity { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
    }

    public class MaterialCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int PropertyType { get; set; }
        public Guid? MaterialGroupId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? DefaultWarehouseId { get; set; }
        public int? ShelfLife { get; set; }
        public int? ShelfLifeUnit { get; set; }
        public decimal MinimumStockQuantity { get; set; }
        public string? Note { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class MaterialUpdateModel : MaterialCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class MaterialFilterModel : FilterModel
    {
        public int? PropertyType { get; set; }
        public Guid? MaterialGroupId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? DefaultWarehouseId { get; set; }
        public Guid? BranchId { get; set; }
    }
}