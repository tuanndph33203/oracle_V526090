using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    public interface ISalesHandler
    {
        Task<Response> GetTableMap(string branchCode, Guid floorId, string status);
        Task<Response> OpenTable(OpenTableRequest request);
        Task<Response> MergeTablesPreview(MergeTablesRequest request);
        Task<Response> MergeTables(MergeTablesRequest request);
        Task<Response> SplitTablePreview(Guid tableId, string branchCode);
        Task<Response> SplitTable(SplitTableRequest request);
        Task<Response> CancelTable(CancelTableRequest request);
        Task<Response> TransferTable(TransferTableRequest request);
        Task<Response> GetAvailableTargetTables(string branchCode, Guid? floorId);
        Task<Response> CheckItems(Guid tableId, string branchCode);
        Task<Response> GetCurrentBill(Guid tableId, string branchCode);
        Task<Response> UpdateBill(UpdateBillRequest request);
        Task<Response> OpenDineInTable(DineInOpenTableRequest request);
        Task<Response> SaveDineInOrder(DineInOrderSaveRequest request);
        Task<Response> SendDineInOrderToKitchen(Guid orderId, string branchCode);
        Task<Response> PrintTemporaryBill(Guid orderId, string branchCode);
        Task<Response> PayDineInOrder(DineInPaymentRequest request);
        Task<Response> CancelDineInOrder(CancelDineInOrderRequest request);
        Task<Response> GetMenuItems(string branchCode);
        Task<Response> GetOrderHistory(OrderHistoryFilterModel model);
        Task<Response> ValidateTherapyVoucher(string code);
    }
}
