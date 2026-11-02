using System;
using System.Collections.Generic;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.WorkShiftBussiness
{
    public class WorkShiftDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int ShiftType { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? BreakStartTime { get; set; }
        public string? BreakEndTime { get; set; }
        public int Status { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
    }

    public class WorkShiftCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int ShiftType { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? BreakStartTime { get; set; }
        public string? BreakEndTime { get; set; }
        public List<Guid> BranchIds { get; set; } = new List<Guid>();
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class WorkShiftUpdateModel : WorkShiftCreateModel
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class WorkShiftFilterModel : FilterModel
    {
        public int? ShiftType { get; set; }
        public Guid? BranchId { get; set; }
    }
}