using System;
using System.Collections.Generic;

namespace AnFood.Application.Bussiness.KitchenBussiness
{
    public class SendKitchenRequestDto
    {
        public string BranchCode { get; set; } = default!;
        public Guid OrderId { get; set; }
        public Guid? TableId { get; set; }
        public string? SendBy { get; set; }
    }

    public class SendKitchenResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public string OrderStatus { get; set; } = default!;
        public List<CreatedRequestDto> CreatedRequests { get; set; } = new();
        public List<KitchenPrintSlipDto> PrintSlips { get; set; } = new();
        public List<KitchenPrintDispatchResultDto> PrintDispatchResults { get; set; } = new();
    }

    public class KitchenPrintDispatchResultDto
    {
        public Guid JobId { get; set; }
        public Guid KitchenAreaId { get; set; }
        public string KitchenAreaName { get; set; } = default!;
        public string PrinterName { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? Message { get; set; }
    }

    public class KitchenPrintItemDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = default!;
        public double Quantity { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }

    public class KitchenPrintSlipDto
    {
        public string SlipType { get; set; } = "Kitchen"; // "Kitchen" (Phiếu báo bếp) | "Cancel" (Phiếu hủy món)
        public Guid KitchenAreaId { get; set; }
        public string KitchenAreaName { get; set; } = default!;
        public int DeviceType { get; set; }
        public string? ProcessingArea { get; set; } // Printer / IP
        public string RequestCode { get; set; } = default!;
        public string OrderCode { get; set; } = default!;
        public string? TableName { get; set; }
        public string? BranchCode { get; set; }
        public string? ServiceType { get; set; }
        public string? WaiterName { get; set; }
        public int CustomerCount { get; set; }
        public DateTime PrintTime { get; set; }
        public string? OrderNote { get; set; }
        public List<KitchenPrintItemDto> Items { get; set; } = new();
    }

    public class KitchenCancellationDispatchRequest
    {
        public Guid OrderId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string Reason { get; set; } = "Hủy món";
        public List<KitchenCancellationDispatchItem> Items { get; set; } = new();
    }

    public class KitchenCancellationDispatchItem
    {
        public Guid OrderItemId { get; set; }
        public double Quantity { get; set; }
    }

    public class CreatedRequestDto
    {
        public Guid RequestId { get; set; }
        public string RequestCode { get; set; } = default!;
        public Guid KitchenAreaId { get; set; }
        public string KitchenAreaName { get; set; } = default!;
    }

    public class KitchenRequestDto
    {
        public Guid RequestId { get; set; }
        public string RequestCode { get; set; } = default!;
        public string OrderCode { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public string? TableName { get; set; }
        public string? FloorName { get; set; }
        public DateTime RequestedAt { get; set; }
        public double ElapsedSeconds { get; set; }
        public string KitchenAreaName { get; set; } = default!;
        public List<KitchenRequestDetailDto> Details { get; set; } = new();
    }

    public class KitchenRequestDetailDto
    {
        public Guid RequestDetailId { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = default!;
        public double RequestQty { get; set; }
        public double ServedQty { get; set; }
        public double SoldOutQty { get; set; }
        public double RemainingQty { get; set; }
        public string? Note { get; set; }
    }

    public class KitchenSummaryDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = default!;
        public double RemainingQty { get; set; }
    }

    public class ServeItemsRequestDto
    {
        public List<ServeItemDetailDto> Items { get; set; } = new();
    }

    public class ServeItemDetailDto
    {
        public Guid RequestDetailId { get; set; }
        public double ServeQty { get; set; }
    }

    public class ServeItemsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public List<PrintJobDto> PrintJobs { get; set; } = new();
        public List<KitchenPrintSlipDto> PrintSlips { get; set; } = new();
        public List<KitchenPrintDispatchResultDto> PrintDispatchResults { get; set; } = new();
    }

    public class PrintJobDto
    {
        public string RequestCode { get; set; } = default!;
        public string PrintType { get; set; } = default!; // ServeItem, SoldOutItem
    }

    public class SoldOutItemsRequestDto
    {
        public List<SoldOutItemDetailDto> Items { get; set; } = new();
    }

    public class SoldOutItemDetailDto
    {
        public Guid RequestDetailId { get; set; }
        public double SoldOutQty { get; set; }
    }

    public class SoldOutItemsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public List<PrintJobDto> PrintJobs { get; set; } = new();
        public List<KitchenPrintSlipDto> PrintSlips { get; set; } = new();
        public List<KitchenPrintDispatchResultDto> PrintDispatchResults { get; set; } = new();
    }
}
