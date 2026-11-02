using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.ShiftBussiness
{
    public class ShiftOpenModel
    {
        public Guid ShiftId { get; set; }
        public decimal OpeningCash { get; set; }
        public string BranchCode { get; set; } = default!;
        public string? MachineCode { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class ShiftExtendModel
    {
        public Guid ShiftId { get; set; }
        public int ExtendedMinutes { get; set; }
        public string? Reason { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class ShiftCloseModel
    {
        public Guid ShiftId { get; set; }
        public Guid ReceiverEmployeeId { get; set; }
        public decimal TransferCash { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ShiftCashCountItemModel
    {
        public decimal Denomination { get; set; }
        public int Quantity { get; set; }
    }

    public class ShiftSummaryModel
    {
        public Guid Id { get; set; }
        public string BranchCode { get; set; } = default!;
        public string CashierId { get; set; } = default!;
        public string CashierName { get; set; } = default!;
        public Guid ShiftId { get; set; }
        public string ShiftName { get; set; } = default!;
        public DateTime OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal TransferRevenue { get; set; }
        public decimal CardRevenue { get; set; }
        public decimal PromoRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal DifferenceCash { get; set; }
        public decimal TransferCash { get; set; }
        public string? ReceiverEmployeeName { get; set; }
        public int PaidInvoicesCount { get; set; }
        public int UnpaidOrdersCount { get; set; }
        public int Status { get; set; }
    }

    public class ShiftDetailModel
    {
        public Guid Id { get; set; }
        public string BranchCode { get; set; } = default!;
        public string? MachineCode { get; set; }
        public string CashierId { get; set; } = default!;
        public string CashierName { get; set; } = default!;
        public Guid ShiftId { get; set; }
        public string ShiftName { get; set; } = default!;
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int ExtendedMinutes { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal DifferenceCash { get; set; }
        public decimal TransferCash { get; set; }
        public Guid? ReceiverEmployeeId { get; set; }
        public int Status { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public List<ShiftCashCountItemModel> CashCounts { get; set; } = new List<ShiftCashCountItemModel>();
    }

    public class ShiftFilterModel
    {
        public string BranchCode { get; set; } = default!;
        public string? CashierId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
