using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.CashDrawerBussiness
{
    public class CashDrawerDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Status { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
    }

    public class CashDrawerCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class CashDrawerUpdateModel : CashDrawerCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class CashDrawerFilterModel : FilterModel
    {
        public Guid? BranchId { get; set; }
    }
}