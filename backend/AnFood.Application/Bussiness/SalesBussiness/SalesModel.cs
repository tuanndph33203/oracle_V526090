using System;
using System.Collections.Generic;
using AnFood.Domain.Enums;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    public class SaleTableMapResponse
    {
        public string BranchCode { get; set; } = default!;
        public Guid FloorId { get; set; }
        public string StatusFilter { get; set; } = default!;
        public List<SaleTableDetailModel> Tables { get; set; } = new();
    }

    public class SaleTableDetailModel
    {
        public Guid TableId { get; set; }
        public string TableCode { get; set; } = default!;
        public string TableName { get; set; } = default!;
        public string Status { get; set; } = "Empty"; // e.g., Empty, WaitingService, Serving, Reserved, WaitingPayment
        public int CustomerCount { get; set; }
        public DateTime? OpenTime { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotate { get; set; }
        public List<SaleBillDetailModel> CurrentBills { get; set; } = new();

        public int Capacity { get; set; }
        public int ActiveOrderCount { get; set; }
        public int ActiveCustomerCount { get; set; }
        public int RemainingCapacity { get; set; }
        public List<ActiveOrderMapInfo> ActiveOrders { get; set; } = new();
        public Guid? FloorPlanId { get; set; }
        public string? FloorPlanName { get; set; }
        public int WaitingConfirmCount { get; set; }
    }

    public class ActiveOrderMapInfo
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = default!;
        public string Status { get; set; } = default!;
        public int CustomerCount { get; set; }
        public DateTime OpenTime { get; set; }
        public string? ServiceType { get; set; }
        public string? TableName { get; set; }
    }

    public class SaleBillDetailModel
    {
        public string BillId { get; set; } = default!;
        public string BillCode { get; set; } = default!;
        public string Status { get; set; } = "Serving";
        public int CustomerCount { get; set; }
        public DateTime OpenTime { get; set; }
        public Guid? WaiterId { get; set; }
        public string? WaiterName { get; set; }
        public List<SaleBillItemModel> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? TableName { get; set; }
        public string? TableCode { get; set; }
        public List<Guid>? TableIds { get; set; }
        public List<string>? TableNames { get; set; }
        public string? SourceTableName { get; set; }
        public int OriginalCustomerCount { get; set; }
        public string? Note { get; set; }
        public string? SelectedPromotionCode { get; set; }
        public string? TherapyVoucherCode { get; set; }
    }

    public class SaleBillItemModel
    {
        public Guid? Id { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; } = default!;
        public string ItemName { get; set; } = default!;
        public string? UnitName { get; set; }
        public double Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public double ServedQuantity { get; set; }
        public double RemainingQuantity { get; set; }
        public string? Note { get; set; }
        public string? SelectedOptionsJson { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PromotionCode { get; set; }
        public double DiscountRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public double VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int IsSentToKitchen { get; set; }
        public string KitchenStatus { get; set; } = "NotSent";
        public double SentQty { get; set; }
        public double ServedQty { get; set; }
        public double SoldOutQty { get; set; }
        public double RemainingQty { get; set; }
        public double CancelledQuantity { get; set; }
        public double ActualQuantity { get; set; }
        public double ReturnedQuantity { get; set; }
        public int IsChecked { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int IsCancelled { get; set; }
    }

    public class OpenTableRequest
    {
        public List<Guid> TableIds { get; set; } = new();
        public string BranchCode { get; set; } = default!;
        public int CustomerCount { get; set; } = 1;
    }

    public class OpenTableResult
    {
        public List<Guid> TableIds { get; set; } = new();
        public string BranchCode { get; set; } = default!;
        public List<SaleBillDetailModel> CreatedBills { get; set; } = new();
    }

    public class MergeTablesRequest
    {
        public List<Guid> TableIds { get; set; } = new();
        public string BranchCode { get; set; } = default!;
        public Guid TargetTableId { get; set; }
    }

    public class MergePreviewModel
    {
        public List<Guid> SourceTableIds { get; set; } = new();
        public List<string> TableNames { get; set; } = new();
        public List<SaleBillItemModel> MergedItems { get; set; } = new();
        public int TotalCustomerCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SplitTableRequest
    {
        public Guid TableId { get; set; }
        public string BranchCode { get; set; } = default!;
        public Guid TargetTableId { get; set; } // Can be an empty table or a new bill
        public bool IsNewBill { get; set; }
        public List<SplitItemRequest> ItemsToSplit { get; set; } = new();
        public int CustomerCount { get; set; }
    }

    public class SplitItemRequest
    {
        public Guid? Id { get; set; }
        public Guid ItemId { get; set; }
        public double Quantity { get; set; }
        public double ActualQuantity { get; set; }
        public double CancelledQuantity { get; set; }
    }

    public class SplitPreviewModel
    {
        public Guid SourceTableId { get; set; }
        public string SourceTableName { get; set; } = default!;
        public List<SaleBillItemModel> OriginalItems { get; set; } = new();
        public int OriginalCustomerCount { get; set; }
    }

    public class CancelTableRequest
    {
        public Guid TableId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string Reason { get; set; } = default!;
    }

    public class TransferTableRequest
    {
        public Guid SourceTableId { get; set; }
        public Guid TargetTableId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string? Note { get; set; }
    }

    public class UpdateBillRequest
    {
        public Guid TableId { get; set; }
        public string BranchCode { get; set; } = default!;
        public int CustomerCount { get; set; }
        public List<SaleBillItemModel> Items { get; set; } = new();
    }

    public class DineInOpenTableRequest
    {
        public List<Guid> TableIds { get; set; } = new();
        public string BranchCode { get; set; } = default!;
        public int CustomerCount { get; set; } = 1;
        public Guid? WaiterId { get; set; }
        public string? WaiterName { get; set; }
        public bool Force { get; set; }
        public string? ServiceType { get; set; }
        public DateTime? OpenTime { get; set; }
        public DateTime? OrderDate { get; set; }
    }

    public class DineInOrderSaveRequest
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public int CustomerCount { get; set; }
        public Guid? WaiterId { get; set; }
        public string? WaiterName { get; set; }
        public List<DineInOrderItemRequest> Items { get; set; } = new();
    }

    public class DineInOrderItemRequest
    {
        public Guid? Id { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; } = default!;
        public string ItemName { get; set; } = default!;
        public string? UnitName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public double DiscountRate { get; set; }
        public double VatRate { get; set; }
        public string? Note { get; set; }
    }

    public class DineInPaymentRequest
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string PaymentType { get; set; } = default!;
        public decimal PaymentAmount { get; set; }
        public string? TransactionId { get; set; }
    }

    public class CancelDineInOrderRequest
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string Reason { get; set; } = default!;
    }

    public class OrderHistoryFilterModel : Contant.FilterModel
    {
        public string? Status { get; set; }
    }

    public class OrderHistoryDetailModel
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = default!;
        public DateTime OrderDate { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string Status { get; set; } = default!;
        public Guid? WaiterId { get; set; }
        public string? WaiterName { get; set; }
        public int CustomerCount { get; set; }
        public string TableNames { get; set; } = default!;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CancelReason { get; set; }
        public string? ServiceType { get; set; }
        public string? Note { get; set; }
        public string? SelectedPromotionCode { get; set; }
        public List<SaleBillItemModel> Items { get; set; } = new();
        public List<DineInPaymentDetailModel> Payments { get; set; } = new();
    }

    public class DineInPaymentDetailModel
    {
        public Guid Id { get; set; }
        public string PaymentType { get; set; } = default!;
        public decimal PaymentAmount { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedOnDate { get; set; }
    }
}
