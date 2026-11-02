using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.DiningTableBussiness
{
    public class DiningTableDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid FloorPlanId { get; set; }
        public string? FloorPlanName { get; set; }
        public Guid BranchId { get; set; }
        public string? BranchName { get; set; }
        public int Status { get; set; }
        public int Capacity { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotate { get; set; }
        public int SortOrder { get; set; }
    }

    public class DiningTableCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public Guid FloorPlanId { get; set; }
        public Guid BranchId { get; set; }
        public int Capacity { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotate { get; set; }
        public int SortOrder { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class DiningTableUpdateModel : DiningTableDetailModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class DiningTableFilterModel : FilterModel
    {
        public Guid? BranchId { get; set; }
        public Guid? FloorPlanId { get; set; }
    }

    public class FloorLayoutUpdateModel
    {
        [Required]
        public string BranchCode { get; set; } = default!;
        [Required]
        public Guid FloorPlanId { get; set; }
        public List<TableLayoutItemModel> Tables { get; set; } = new();
    }

    public class TableLayoutItemModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Capacity { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotate { get; set; }
        public int SortOrder { get; set; }
    }
}