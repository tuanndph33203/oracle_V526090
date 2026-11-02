using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Infrastructure;
using AnFood.Persistence;
using AnFood.Domain.Datatables;
using AnFood.Application.Bussiness.AppLogger;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace AnFood.Application.Bussiness.ReportBussiness
{
    public class ReportHandler : IReportHandler
    {
        private readonly IReadOnlyUnitOfWork _unit;
        private readonly IAppLogger _logger;

        public ReportHandler(IReadOnlyUnitOfWork unit, IAppLogger logger)
        {
            _unit = unit;
            _logger = logger;
        }

        private static string? NormalizeBranchId(string? branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId)) return null;
            var trimmed = branchId.Trim();
            if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return trimmed;
        }

        public async Task<Response> GetRevenueReportByItem(RevenueReportFilterModel filter)
        {
            try
            {
                var response = new RevenueReportResponseDto();
                var dbConnection = _unit.Connection;
                
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = "sp_GetRevenueReportByItem";
                    command.CommandType = CommandType.StoredProcedure;

                    // BranchID parameter
                    var pBranch = command.CreateParameter();
                    pBranch.ParameterName = "@BranchID";
                    pBranch.Value = (object)NormalizeBranchId(filter.BranchId) ?? DBNull.Value;
                    command.Parameters.Add(pBranch);

                    // ReportType parameter
                    var pReportType = command.CreateParameter();
                    pReportType.ParameterName = "@ReportType";
                    pReportType.Value = filter.ReportType;
                    command.Parameters.Add(pReportType);

                    // FromDate parameter
                    var pFrom = command.CreateParameter();
                    pFrom.ParameterName = "@FromDate";
                    pFrom.Value = (object)filter.FromDate ?? DBNull.Value;
                    command.Parameters.Add(pFrom);

                    // ToDate parameter
                    var pTo = command.CreateParameter();
                    pTo.ParameterName = "@ToDate";
                    pTo.Value = (object)filter.ToDate ?? DBNull.Value;
                    command.Parameters.Add(pTo);

                    // PageNumber parameter
                    var pPageNum = command.CreateParameter();
                    pPageNum.ParameterName = "@PageNumber";
                    pPageNum.Value = filter.PageNumber;
                    command.Parameters.Add(pPageNum);

                    // PageSize parameter
                    var pPageSize = command.CreateParameter();
                    pPageSize.ParameterName = "@PageSize";
                    pPageSize.Value = filter.PageSize;
                    command.Parameters.Add(pPageSize);

                    if (dbConnection.State == ConnectionState.Closed)
                        await dbConnection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: Paginated items
                        while (await reader.ReadAsync())
                        {
                            response.Items.Add(new RevenueReportItemDto
                            {
                                STT = Convert.ToInt64(reader["STT"]),
                                ItemId = Guid.Parse(reader["ItemId"].ToString()!),
                                ItemCode = reader["ItemCode"].ToString()!,
                                ItemName = reader["ItemName"].ToString()!,
                                UnitName = reader["UnitName"] == DBNull.Value ? null : reader["UnitName"].ToString(),
                                Quantity = Convert.ToDouble(reader["Quantity"]),
                                AvgPrice = Convert.ToDecimal(reader["AvgPrice"]),
                                Total = Convert.ToDecimal(reader["Total"]),
                                Revenue = Convert.ToDecimal(reader["Revenue"]),
                                ItemAmount = Convert.ToDecimal(reader["ItemAmount"]),
                                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                                ItemType = MapItemType(reader["ItemType"] == DBNull.Value ? null : reader["ItemType"].ToString()),
                                MenuGroup = reader["MenuGroup"] == DBNull.Value ? null : reader["MenuGroup"].ToString()
                            });
                        }

                        // Result Set 2: Summary
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            response.Summary = new RevenueReportSummaryDto
                            {
                                TotalRecords = Convert.ToInt32(reader["TotalRecords"]),
                                TotalQuantity = Convert.ToDouble(reader["TotalQuantity"]),
                                TotalTotal = Convert.ToDecimal(reader["TotalTotal"]),
                                TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                                TotalItemAmount = Convert.ToDecimal(reader["TotalItemAmount"]),
                                TotalDiscountAmount = Convert.ToDecimal(reader["TotalDiscountAmount"])
                            };
                        }
                    }
                }

                return new ResponseObject<RevenueReportResponseDto>(response, "Lấy báo cáo doanh thu thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetRevenueReportByItem error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetItemInvoiceDetails(Guid itemId, string? branchId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                // Query sales order items for the given itemId from paid orders
                var query = from oi in itemRepo.GetAll()
                            join o in orderRepo.GetAll() on oi.OrderId equals o.Id
                            where oi.ItemId == itemId && o.Status == "Paid" && oi.IsCancelled == 0 && oi.IsReturned == 0 
                                  && (oi.Quantity - oi.ReturnedQuantity - oi.CancelledQuantity) > 0
                            select new { oi, o };

                if (!string.IsNullOrEmpty(branchId))
                {
                    query = query.Where(x => x.o.BranchCode == branchId);
                }

                if (fromDate.HasValue)
                {
                    var fDate = fromDate.Value.Date;
                    query = query.Where(x => x.o.OrderDate >= fDate);
                }

                if (toDate.HasValue)
                {
                    var nextDay = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.o.OrderDate < nextDay);
                }

                var dbList = await query.Select(x => new
                {
                    OrderId = x.o.Id,
                    OrderCode = x.o.OrderCode,
                    OrderDate = x.o.OrderDate,
                    Quantity = x.oi.Quantity - x.oi.ReturnedQuantity - x.oi.CancelledQuantity,
                    UnitPrice = x.oi.UnitPrice,
                    DiscountAmount = x.oi.DiscountAmount,
                    TotalAmount = x.oi.TotalAmount
                }).ToListAsync();

                var orderIds = dbList.Select(x => x.OrderId).Distinct().ToList();

                // Fetch tables for these orders
                var tablesList = await orderTableRepo.Get(x => orderIds.Contains(x.OrderId)).ToListAsync();
                var tablesLookup = tablesList.GroupBy(x => x.OrderId)
                    .ToDictionary(
                        g => g.Key,
                        g => string.Join(" + ", g.OrderBy(x => x.TableName).Select(x => x.TableName))
                    );

                var result = dbList.GroupBy(x => new { x.OrderId, x.OrderCode, x.OrderDate })
                    .Select(g => new ItemInvoiceDetailDto
                    {
                        OrderId = g.Key.OrderId,
                        OrderCode = g.Key.OrderCode,
                        OrderDate = g.Key.OrderDate,
                        TableName = tablesLookup.TryGetValue(g.Key.OrderId, out var tbl) ? tbl : "Mang về/Giao hàng",
                        Quantity = g.Sum(x => x.Quantity),
                        UnitPrice = g.First().UnitPrice,
                        DiscountAmount = g.Sum(x => x.DiscountAmount),
                        TotalAmount = g.Sum(x => x.TotalAmount)
                    })
                    .OrderByDescending(x => x.OrderDate)
                    .ThenByDescending(x => x.OrderCode)
                    .ToList();

                return new ResponseList<ItemInvoiceDetailDto>(result, "Lấy danh sách hóa đơn chi tiết thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetItemInvoiceDetails error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        private static bool HasColumn(IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public async Task<Response> GetDetailedRevenueReport(DetailedReportFilterModel filter)
        {
            try
            {
                var response = new DetailedRevenueResponseDto();
                var invoicesMap = new Dictionary<Guid, DetailedRevenueInvoiceDto>();
                var dbConnection = _unit.Connection;

                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = "sp_GetDetailedRevenueReport";
                    command.CommandType = CommandType.StoredProcedure;

                    var pBranch = command.CreateParameter();
                    pBranch.ParameterName = "@BranchID";
                    pBranch.Value = (object)NormalizeBranchId(filter.BranchId) ?? DBNull.Value;
                    command.Parameters.Add(pBranch);

                    var pFrom = command.CreateParameter();
                    pFrom.ParameterName = "@FromDate";
                    pFrom.Value = (object)filter.FromDate ?? DBNull.Value;
                    command.Parameters.Add(pFrom);

                    var pTo = command.CreateParameter();
                    pTo.ParameterName = "@ToDate";
                    pTo.Value = (object)filter.ToDate ?? DBNull.Value;
                    command.Parameters.Add(pTo);

                    var pPageNum = command.CreateParameter();
                    pPageNum.ParameterName = "@PageNumber";
                    pPageNum.Value = filter.PageNumber;
                    command.Parameters.Add(pPageNum);

                    var pPageSize = command.CreateParameter();
                    pPageSize.ParameterName = "@PageSize";
                    pPageSize.Value = filter.PageSize;
                    command.Parameters.Add(pPageSize);

                    if (dbConnection.State == ConnectionState.Closed)
                        await dbConnection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: Master Invoices
                        while (await reader.ReadAsync())
                        {
                            var invoice = new DetailedRevenueInvoiceDto
                            {
                                OrderId = Guid.Parse(reader["OrderId"].ToString()!),
                                OrderCode = reader["OrderCode"].ToString()!,
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                OpenTime = Convert.ToDateTime(reader["OpenTime"]),
                                PaymentTime = reader["PaymentTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["PaymentTime"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                PreTaxAmount = Convert.ToDecimal(reader["PreTaxAmount"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                DiscountPreTaxAmount = Convert.ToDecimal(reader["DiscountPreTaxAmount"]),
                                CustomerName = reader["CustomerName"] == DBNull.Value ? null : reader["CustomerName"].ToString(),
                                ServerName = reader["ServerName"] == DBNull.Value ? null : reader["ServerName"].ToString(),
                                CashierName = reader["CashierName"] == DBNull.Value ? null : reader["CashierName"].ToString(),
                                ShiftName = Enumerable.Range(0, reader.FieldCount).Any(i => reader.GetName(i).Equals("ShiftName", StringComparison.OrdinalIgnoreCase)) 
                                    ? (reader["ShiftName"] == DBNull.Value ? null : reader["ShiftName"].ToString()) 
                                    : null,
                                ServiceType = reader["ServiceType"] == DBNull.Value ? null : reader["ServiceType"].ToString(),
                                FloorName = reader["FloorName"] == DBNull.Value ? null : reader["FloorName"].ToString(),
                                CustomerCount = Convert.ToInt32(reader["CustomerCount"]),
                                TableName = reader["TableName"] == DBNull.Value ? null : reader["TableName"].ToString(),
                                Note = HasColumn(reader, "Note") && reader["Note"] != DBNull.Value ? reader["Note"].ToString() : null,
                                CashAmount = HasColumn(reader, "CashAmount") && reader["CashAmount"] != DBNull.Value ? Convert.ToDecimal(reader["CashAmount"]) : 0m,
                                TransferAmount = HasColumn(reader, "TransferAmount") && reader["TransferAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TransferAmount"]) : 0m,
                                VoucherAmount = HasColumn(reader, "VoucherAmount") && reader["VoucherAmount"] != DBNull.Value ? Convert.ToDecimal(reader["VoucherAmount"]) : 0m
                            };
                            response.Invoices.Add(invoice);
                            invoicesMap[invoice.OrderId] = invoice;
                        }

                        // Result Set 2: Detail Items
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var orderId = Guid.Parse(reader["OrderId"].ToString()!);
                                var detail = new DetailedRevenueDetailDto
                                {
                                    OrderId = orderId,
                                    ItemCode = reader["ItemCode"].ToString()!,
                                    ItemName = reader["ItemName"].ToString()!,
                                    UnitName = reader["UnitName"] == DBNull.Value ? null : reader["UnitName"].ToString(),
                                    Quantity = Convert.ToDouble(reader["Quantity"]),
                                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                    PreTaxAmount = Convert.ToDecimal(reader["PreTaxAmount"]),
                                    TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                    DiscountPreTaxAmount = Convert.ToDecimal(reader["DiscountPreTaxAmount"]),
                                    TaxRate = Convert.ToDouble(reader["TaxRate"]),
                                    ItemType = MapItemType(reader["ItemType"] == DBNull.Value ? null : reader["ItemType"].ToString()),
                                    MenuGroup = reader["MenuGroup"] == DBNull.Value ? null : reader["MenuGroup"].ToString()
                                };

                                if (invoicesMap.TryGetValue(orderId, out var inv))
                                {
                                    inv.Details.Add(detail);
                                }
                            }
                        }

                        // Result Set 3: Summary Totals
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            response.Summary = new DetailedRevenueSummaryDto
                            {
                                TotalRecords = Convert.ToInt32(reader["TotalRecords"]),
                                TotalTotalAmount = Convert.ToDecimal(reader["TotalTotalAmount"]),
                                TotalPreTaxAmount = Convert.ToDecimal(reader["TotalPreTaxAmount"]),
                                TotalTaxAmount = Convert.ToDecimal(reader["TotalTaxAmount"]),
                                TotalDiscountPreTaxAmount = Convert.ToDecimal(reader["TotalDiscountPreTaxAmount"]),
                                TotalCashAmount = HasColumn(reader, "TotalCashAmount") && reader["TotalCashAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCashAmount"]) : 0m,
                                TotalTransferAmount = HasColumn(reader, "TotalTransferAmount") && reader["TotalTransferAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalTransferAmount"]) : 0m,
                                TotalVoucherAmount = HasColumn(reader, "TotalVoucherAmount") && reader["TotalVoucherAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalVoucherAmount"]) : 0m,
                                TotalQuantity = Convert.ToDouble(reader["TotalQuantity"])
                            };
                        }
                    }
                }

                return new ResponseObject<DetailedRevenueResponseDto>(response, "Lấy báo cáo chi tiết thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetDetailedRevenueReport error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }
        public async Task<Response> GetDailyRevenueReport(DailyRevenueFilterModel filter)
        {
            try
            {
                var response = new DailyRevenueResponseDto();
                var dbConnection = _unit.Connection;

                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = "sp_GetDailyRevenueReport";
                    command.CommandType = CommandType.StoredProcedure;

                    // BranchID parameter
                    var pBranch = command.CreateParameter();
                    pBranch.ParameterName = "@BranchID";
                    pBranch.Value = (object)NormalizeBranchId(filter.BranchId) ?? DBNull.Value;
                    command.Parameters.Add(pBranch);

                    // FromDate parameter
                    var pFrom = command.CreateParameter();
                    pFrom.ParameterName = "@FromDate";
                    pFrom.Value = (object)filter.FromDate ?? DBNull.Value;
                    command.Parameters.Add(pFrom);

                    // ToDate parameter
                    var pTo = command.CreateParameter();
                    pTo.ParameterName = "@ToDate";
                    pTo.Value = (object)filter.ToDate ?? DBNull.Value;
                    command.Parameters.Add(pTo);

                    // PageNumber parameter
                    var pPageNum = command.CreateParameter();
                    pPageNum.ParameterName = "@PageNumber";
                    pPageNum.Value = filter.PageNumber;
                    command.Parameters.Add(pPageNum);

                    // PageSize parameter
                    var pPageSize = command.CreateParameter();
                    pPageSize.ParameterName = "@PageSize";
                    pPageSize.Value = filter.PageSize;
                    command.Parameters.Add(pPageSize);

                    if (dbConnection.State == ConnectionState.Closed)
                        await dbConnection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: Paginated daily aggregates
                        while (await reader.ReadAsync())
                        {
                            response.Items.Add(new DailyRevenueItemDto
                            {
                                STT = Convert.ToInt64(reader["STT"]),
                                ReportDate = Convert.ToDateTime(reader["ReportDate"]),
                                PreTaxAmount = Convert.ToDecimal(reader["PreTaxAmount"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                CashAmount = Convert.ToDecimal(reader["CashAmount"]),
                                TransferAmount = Convert.ToDecimal(reader["TransferAmount"]),
                                VoucherAmount = Convert.ToDecimal(reader["VoucherAmount"]),
                                ActualReceipt = Convert.ToDecimal(reader["ActualReceipt"]),
                                CustomerCount = Convert.ToInt32(reader["CustomerCount"])
                            });
                        }

                        // Result Set 2: Global totals for footer
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            response.Summary = new DailyRevenueSummaryDto
                            {
                                TotalRecords = Convert.ToInt32(reader["TotalRecords"]),
                                TotalPreTaxAmount = Convert.ToDecimal(reader["TotalPreTaxAmount"]),
                                TotalTaxAmount = Convert.ToDecimal(reader["TotalTaxAmount"]),
                                TotalDiscountAmount = Convert.ToDecimal(reader["TotalDiscountAmount"]),
                                TotalTotalAmount = Convert.ToDecimal(reader["TotalTotalAmount"]),
                                TotalCashAmount = Convert.ToDecimal(reader["TotalCashAmount"]),
                                TotalTransferAmount = Convert.ToDecimal(reader["TotalTransferAmount"]),
                                TotalVoucherAmount = Convert.ToDecimal(reader["TotalVoucherAmount"]),
                                TotalActualReceipt = Convert.ToDecimal(reader["TotalActualReceipt"]),
                                TotalCustomerCount = Convert.ToInt32(reader["TotalCustomerCount"])
                            };
                        }
                    }
                }

                return new ResponseObject<DailyRevenueResponseDto>(response, "Lấy báo cáo doanh thu theo ngày thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetDailyRevenueReport error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetDailyInvoiceDetails(DateTime date, string? branchId)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1).AddTicks(-1);

                var orders = await orderRepo.Get(x => x.OrderDate >= startOfDay && x.OrderDate <= endOfDay && x.Status == "Paid"
                    && (string.IsNullOrEmpty(branchId) || x.BranchCode == branchId))
                    .OrderByDescending(x => x.OrderDate)
                    .ThenByDescending(x => x.PaymentTime)
                    .ThenByDescending(x => x.OrderCode)
                    .ToListAsync();

                var orderIds = orders.Select(o => o.Id).ToList();
                var orderTables = await orderTableRepo.Get(x => orderIds.Contains(x.OrderId)).ToListAsync();

                var result = orders.Select(o => {
                    var tables = orderTables.Where(t => t.OrderId == o.Id).Select(t => t.TableName).ToList();
                    var tableNames = tables.Any() ? string.Join(", ", tables) : "Mang về/Giao hàng";
                    return new DailyInvoiceDetailDto
                    {
                        OrderId = o.Id,
                        OrderCode = o.OrderCode,
                        TableName = tableNames,
                        ServiceType = o.ServiceType,
                        SubTotal = o.SubTotal,
                        DiscountAmount = o.DiscountAmount,
                        VatAmount = o.VatAmount,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        OrderDate = o.OrderDate,
                        CashierUserName = o.CashierUserName
                    };
                }).ToList();

                return new ResponseObject<List<DailyInvoiceDetailDto>>(result, "Lấy bảng kê hóa đơn thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetDailyInvoiceDetails error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetInvoiceListReport(InvoiceListFilterModel filter)
        {
            try
            {
                var response = new InvoiceListResponseDto();
                var dbConnection = _unit.Connection;

                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = "sp_GetInvoiceListReport";
                    command.CommandType = CommandType.StoredProcedure;

                    // BranchID parameter
                    var pBranch = command.CreateParameter();
                    pBranch.ParameterName = "@BranchID";
                    pBranch.Value = (object)NormalizeBranchId(filter.BranchId) ?? DBNull.Value;
                    command.Parameters.Add(pBranch);

                    // FromDate parameter
                    var pFrom = command.CreateParameter();
                    pFrom.ParameterName = "@FromDate";
                    pFrom.Value = (object)filter.FromDate ?? DBNull.Value;
                    command.Parameters.Add(pFrom);

                    // ToDate parameter
                    var pTo = command.CreateParameter();
                    pTo.ParameterName = "@ToDate";
                    pTo.Value = (object)filter.ToDate ?? DBNull.Value;
                    command.Parameters.Add(pTo);

                    // PageNumber parameter
                    var pPageNum = command.CreateParameter();
                    pPageNum.ParameterName = "@PageNumber";
                    pPageNum.Value = filter.PageNumber;
                    command.Parameters.Add(pPageNum);

                    // PageSize parameter
                    var pPageSize = command.CreateParameter();
                    pPageSize.ParameterName = "@PageSize";
                    pPageSize.Value = filter.PageSize;
                    command.Parameters.Add(pPageSize);

                    if (dbConnection.State == ConnectionState.Closed)
                        await dbConnection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: Invoice list
                        while (await reader.ReadAsync())
                        {
                            response.Items.Add(new InvoiceListItemDto
                            {
                                OrderId = Guid.Parse(reader["OrderId"].ToString()!),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                OpenTime = reader["OpenTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["OpenTime"]),
                                PaymentTime = reader["PaymentTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["PaymentTime"]),
                                OrderCode = reader["OrderCode"].ToString()!,
                                PreTaxAmount = Convert.ToDecimal(reader["PreTaxAmount"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                CustomerInfo = reader["CustomerInfo"] == DBNull.Value ? null : reader["CustomerInfo"].ToString(),
                                CustomerCount = Convert.ToInt32(reader["CustomerCount"]),
                                ServiceType = reader["ServiceType"] == DBNull.Value ? null : reader["ServiceType"].ToString(),
                                AreaName = reader["AreaName"] == DBNull.Value ? null : reader["AreaName"].ToString(),
                                CashAmount = Convert.ToDecimal(reader["CashAmount"]),
                                TransferAmount = Convert.ToDecimal(reader["TransferAmount"]),
                                VoucherAmount = Convert.ToDecimal(reader["VoucherAmount"]),
                                TableInfo = reader["TableInfo"] == DBNull.Value ? null : reader["TableInfo"].ToString(),
                                CashierName = reader["CashierName"] == DBNull.Value ? null : reader["CashierName"].ToString(),
                                DeliveryPartner = reader["DeliveryPartner"] == DBNull.Value ? null : reader["DeliveryPartner"].ToString(),
                                WaiterName = reader["WaiterName"] == DBNull.Value ? null : reader["WaiterName"].ToString(),
                                Note = reader["Note"] == DBNull.Value ? null : reader["Note"].ToString(),
                                PromotionInfo = reader["PromotionInfo"] == DBNull.Value ? null : reader["PromotionInfo"].ToString()
                            });
                        }

                        // Result Set 2: Summary
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            response.Summary = new InvoiceListSummaryDto
                            {
                                TotalRecords = Convert.ToInt32(reader["TotalRecords"]),
                                TotalPreTaxAmount = Convert.ToDecimal(reader["TotalPreTaxAmount"]),
                                TotalTaxAmount = Convert.ToDecimal(reader["TotalTaxAmount"]),
                                TotalDiscountAmount = Convert.ToDecimal(reader["TotalDiscountAmount"]),
                                TotalTotalAmount = Convert.ToDecimal(reader["TotalTotalAmount"]),
                                TotalCashAmount = Convert.ToDecimal(reader["TotalCashAmount"]),
                                TotalTransferAmount = Convert.ToDecimal(reader["TotalTransferAmount"]),
                                TotalVoucherAmount = Convert.ToDecimal(reader["TotalVoucherAmount"]),
                                TotalCustomerCount = Convert.ToInt32(reader["TotalCustomerCount"])
                            };
                        }
                    }
                }

                var invoiceIds = response.Items.Select(x => x.OrderId).ToList();
                if (invoiceIds.Count > 0)
                {
                    var ratings = await _unit.GetRepository<SalesOrder>()
                        .Get(x => (invoiceIds.Contains(x.Id) || (x.MergedIntoOrderId.HasValue && invoiceIds.Contains(x.MergedIntoOrderId.Value)))
                            && x.RatingContent != null && x.RatingContent != "")
                        .Select(x => new { x.Id, x.MergedIntoOrderId, x.RatingContent, x.OpenTime })
                        .ToListAsync();
                    foreach (var invoice in response.Items)
                    {
                        var contents = ratings.Where(x => x.Id == invoice.OrderId || x.MergedIntoOrderId == invoice.OrderId)
                            .OrderBy(x => x.OpenTime)
                            .Select(x => x.RatingContent);
                        invoice.RatingContent = string.Join("\n", contents);
                    }
                }

                return new ResponseObject<InvoiceListResponseDto>(response, "Lấy danh sách hóa đơn thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetInvoiceListReport error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetInvoiceDetailsByCode(string invoiceNo)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var order = await orderRepo.Get(x => x.OrderCode == invoiceNo && x.Status == "Paid").FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, $"Không tìm thấy hóa đơn: {invoiceNo}");
                }

                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var items = await itemRepo.Get(x => x.OrderId == order.Id && x.IsCancelled == 0 && x.IsReturned == 0 && (x.Quantity - x.ReturnedQuantity - x.CancelledQuantity) > 0)
                    .OrderBy(x => x.LineNo).ToListAsync();

                var tableRepo = _unit.GetRepository<SalesOrderTable>();
                var tables = await tableRepo.Get(x => x.OrderId == order.Id).ToListAsync();

                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                var payments = await paymentRepo.Get(x => x.OrderId == order.Id).OrderBy(x => x.CreatedOnDate).ToListAsync();

                var tableNames = tables.Any() ? string.Join(", ", tables.Select(t => $"{t.TableCode} - {t.TableName}")) : "Mang về/Giao hàng";

                var dto = new InvoiceDetailPopupDto
                {
                    OrderCode = order.OrderCode,
                    OrderDate = order.OrderDate,
                    TableNames = tableNames,
                    ServiceType = order.ServiceType,
                    ShippingFeeNote = order.ShippingFeeNote,
                    SubTotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    VatAmount = order.VatAmount,
                    TotalAmount = order.TotalAmount,
                    CashierName = order.CashierUserName,
                    Note = order.Note,
                    Items = items.Select(i => new InvoiceDetailItemDto
                    {
                        ItemName = i.ItemName,
                        Quantity = i.Quantity - i.ReturnedQuantity - i.CancelledQuantity,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = i.DiscountAmount,
                        VatAmount = i.VatAmount,
                        TotalAmount = i.TotalAmount,
                        SelectedOptions = ReadSelectedOptions(i.SelectedOptionsJson)
                    }).ToList(),
                    Payments = payments.Select(p => new InvoiceDetailPaymentDto
                    {
                        PaymentType = p.PaymentType,
                        PaymentAmount = p.PaymentAmount
                    }).ToList()
                };

                return new ResponseObject<InvoiceDetailPopupDto>(dto, "Lấy chi tiết hóa đơn thành công", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetInvoiceDetailsByCode error", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        private static List<AnFood.Application.Bussiness.SalesBussiness.OrderSelectedOptionDto> ReadSelectedOptions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<AnFood.Application.Bussiness.SalesBussiness.OrderSelectedOptionDto>>(json) ?? new();
            }
            catch (System.Text.Json.JsonException)
            {
                return new();
            }
        }

        public async Task<byte[]?> ExportExcelAsync(RevenueReportFilterModel filter)
        {
            try
            {
                var branchId = NormalizeBranchId(filter.BranchId);

                using (var workbook = new XLWorkbook())
                {
                    // Default font settings to prevent Vietnamese font rendering issues
                    workbook.Style.Font.FontName = "Arial";
                    workbook.Style.Font.FontSize = 11;

                    if (filter.ReportType == 1)
                    {
                        var worksheet = workbook.Worksheets.Add("DoanhThuTheoNgay");
                        // 1. Fetch data
                        var dailyFilter = new DailyRevenueFilterModel
                        {
                            BranchId = branchId,
                            FromDate = filter.FromDate,
                            ToDate = filter.ToDate,
                            PageNumber = 1,
                            PageSize = 1000000
                        };
                        var res = await GetDailyRevenueReport(dailyFilter);
                        var data = (res as ResponseObject<DailyRevenueResponseDto>)?.Data;
                        var items = data?.Items ?? new List<DailyRevenueItemDto>();
                        var summary = data?.Summary;

                        // Headers
                        string[] headers = {
                            "STT", "Ngày", "Doanh thu trước thuế", "Thuế GTGT",
                            "Chiết khấu", "Tổng tiền thanh toán", "Tiền mặt",
                            "Chuyển khoản", "Voucher", "Thực thu", "Số khách"
                        };
                        for (int col = 0; col < headers.Length; col++)
                        {
                            var cell = worksheet.Cell(1, col + 1);
                            cell.Value = headers[col];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        int rowIdx = 2;
                        foreach (var item in items)
                        {
                            worksheet.Cell(rowIdx, 1).Value = item.STT;
                            worksheet.Cell(rowIdx, 2).Value = item.ReportDate.ToString("dd/MM/yyyy");
                            worksheet.Cell(rowIdx, 3).Value = item.PreTaxAmount;
                            worksheet.Cell(rowIdx, 4).Value = item.TaxAmount;
                            worksheet.Cell(rowIdx, 5).Value = item.DiscountAmount;
                            worksheet.Cell(rowIdx, 6).Value = item.TotalAmount;
                            worksheet.Cell(rowIdx, 7).Value = item.CashAmount;
                            worksheet.Cell(rowIdx, 8).Value = item.TransferAmount;
                            worksheet.Cell(rowIdx, 9).Value = item.VoucherAmount;
                            worksheet.Cell(rowIdx, 10).Value = item.ActualReceipt;
                            worksheet.Cell(rowIdx, 11).Value = item.CustomerCount;

                            // Number formatting
                            for (int col = 3; col <= 10; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Cell(rowIdx, 11).Style.NumberFormat.Format = "#,##0";
                            rowIdx++;
                        }

                        // Summary row
                        if (summary != null)
                        {
                            worksheet.Cell(rowIdx, 1).Value = "Tổng cộng";
                            worksheet.Cell(rowIdx, 1).Style.Font.Bold = true;
                            worksheet.Range(rowIdx, 1, rowIdx, 2).Merge();

                            worksheet.Cell(rowIdx, 3).Value = summary.TotalPreTaxAmount;
                            worksheet.Cell(rowIdx, 4).Value = summary.TotalTaxAmount;
                            worksheet.Cell(rowIdx, 5).Value = summary.TotalDiscountAmount;
                            worksheet.Cell(rowIdx, 6).Value = summary.TotalTotalAmount;
                            worksheet.Cell(rowIdx, 7).Value = summary.TotalCashAmount;
                            worksheet.Cell(rowIdx, 8).Value = summary.TotalTransferAmount;
                            worksheet.Cell(rowIdx, 9).Value = summary.TotalVoucherAmount;
                            worksheet.Cell(rowIdx, 10).Value = summary.TotalActualReceipt;
                            worksheet.Cell(rowIdx, 11).Value = summary.TotalCustomerCount;

                            for (int col = 3; col <= 11; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.Font.Bold = true;
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        }
                        worksheet.Columns().AdjustToContents();
                    }
                    else if (filter.ReportType == 2)
                    {
                        var worksheet = workbook.Worksheets.Add("DoanhThuTheoMatHang");
                        // 1. Fetch data
                        var itemFilter = new RevenueReportFilterModel
                        {
                            BranchId = branchId,
                            ReportType = 2,
                            FromDate = filter.FromDate,
                            ToDate = filter.ToDate,
                            PageNumber = 1,
                            PageSize = 1000000
                        };
                        var res = await GetRevenueReportByItem(itemFilter);
                        var data = (res as ResponseObject<RevenueReportResponseDto>)?.Data;
                        var items = data?.Items ?? new List<RevenueReportItemDto>();
                        var summary = data?.Summary;

                        // Header Row 1
                        worksheet.Cell(1, 1).Value = "STT";
                        worksheet.Range(1, 1, 2, 1).Merge();

                        worksheet.Cell(1, 2).Value = "Mã món";
                        worksheet.Range(1, 2, 2, 2).Merge();

                        worksheet.Cell(1, 3).Value = "Tên món";
                        worksheet.Range(1, 3, 2, 3).Merge();

                        worksheet.Cell(1, 4).Value = "ĐVT";
                        worksheet.Range(1, 4, 2, 4).Merge();

                        worksheet.Cell(1, 5).Value = "SL bán";
                        worksheet.Range(1, 5, 2, 5).Merge();

                        worksheet.Cell(1, 6).Value = "Đơn giá trung bình (giá món)";
                        worksheet.Range(1, 6, 2, 6).Merge();

                        // Group Header: Doanh thu
                        worksheet.Cell(1, 7).Value = "Doanh thu";
                        worksheet.Range(1, 7, 1, 9).Merge();

                        worksheet.Cell(2, 7).Value = "Tổng";
                        worksheet.Cell(2, 8).Value = "Tiền hàng";
                        worksheet.Cell(2, 9).Value = "Khuyến mại";

                        worksheet.Cell(1, 10).Value = "Loại mặt hàng";
                        worksheet.Range(1, 10, 2, 10).Merge();

                        worksheet.Cell(1, 11).Value = "Nhóm thực đơn";
                        worksheet.Range(1, 11, 2, 11).Merge();

                        // Style Headers
                        var headerRange = worksheet.Range(1, 1, 2, 11);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9"); // slate-100

                        var doanhThuHeader = worksheet.Cell(1, 7);
                        doanhThuHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7"); // light green

                        worksheet.Range(2, 7, 2, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FDF4");

                        int rowIdx = 3;
                        foreach (var item in items)
                        {
                            worksheet.Cell(rowIdx, 1).Value = item.STT;
                            worksheet.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            worksheet.Cell(rowIdx, 2).Value = item.ItemCode;
                            worksheet.Cell(rowIdx, 3).Value = item.ItemName;
                            worksheet.Cell(rowIdx, 3).Style.Font.Bold = true;

                            worksheet.Cell(rowIdx, 4).Value = item.UnitName ?? "—";
                            worksheet.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            worksheet.Cell(rowIdx, 5).Value = item.Quantity;
                            worksheet.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0";

                            worksheet.Cell(rowIdx, 6).Value = item.AvgPrice;
                            worksheet.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0\"đ\"";

                            worksheet.Cell(rowIdx, 7).Value = item.Total;
                            worksheet.Cell(rowIdx, 7).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0\"đ\"";

                            worksheet.Cell(rowIdx, 8).Value = item.ItemAmount;
                            worksheet.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0\"đ\"";

                            worksheet.Cell(rowIdx, 9).Value = item.DiscountAmount;
                            worksheet.Cell(rowIdx, 9).Style.NumberFormat.Format = "#,##0\"đ\"";
                            if (item.DiscountAmount > 0)
                            {
                                worksheet.Cell(rowIdx, 9).Style.Font.FontColor = XLColor.FromHtml("#EF4444"); // red
                            }

                            worksheet.Cell(rowIdx, 10).Value = item.ItemType;
                            worksheet.Cell(rowIdx, 11).Value = item.MenuGroup;

                            rowIdx++;
                        }

                        // Summary row
                        if (summary != null)
                        {
                            worksheet.Cell(rowIdx, 1).Value = "TỔNG CỘNG";
                            worksheet.Cell(rowIdx, 1).Style.Font.Bold = true;
                            worksheet.Range(rowIdx, 1, rowIdx, 4).Merge();

                            worksheet.Cell(rowIdx, 5).Value = summary.TotalQuantity;
                            worksheet.Cell(rowIdx, 5).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0";

                            worksheet.Cell(rowIdx, 6).Value = "—";
                            worksheet.Cell(rowIdx, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            worksheet.Cell(rowIdx, 7).Value = summary.TotalTotal;
                            worksheet.Cell(rowIdx, 7).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0\"đ\"";

                            worksheet.Cell(rowIdx, 8).Value = summary.TotalItemAmount;
                            worksheet.Cell(rowIdx, 8).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0\"đ\"";

                            worksheet.Cell(rowIdx, 9).Value = summary.TotalDiscountAmount;
                            worksheet.Cell(rowIdx, 9).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 9).Style.NumberFormat.Format = "#,##0\"đ\"";
                            if (summary.TotalDiscountAmount > 0)
                            {
                                worksheet.Cell(rowIdx, 9).Style.Font.FontColor = XLColor.FromHtml("#EF4444");
                            }

                            worksheet.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
                        }

                        // Borders for all cells
                        var allDataRange = worksheet.Range(1, 1, rowIdx, 11);
                        allDataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        allDataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        allDataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                        allDataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

                        worksheet.Columns().AdjustToContents();
                    }
                    else if (filter.ReportType == 3)
                    {
                        var worksheet = workbook.Worksheets.Add("BangKeHoaDon");
                        // 1. Fetch data
                        var invFilter = new InvoiceListFilterModel
                        {
                            BranchId = branchId,
                            FromDate = filter.FromDate,
                            ToDate = filter.ToDate,
                            PageNumber = 1,
                            PageSize = 1000000
                        };
                        var res = await GetInvoiceListReport(invFilter);
                        var data = (res as ResponseObject<InvoiceListResponseDto>)?.Data;
                        var items = data?.Items ?? new List<InvoiceListItemDto>();
                        var summary = data?.Summary;

                        // Headers
                        string[] headers = {
                            "STT", "Ngày hóa đơn", "Giờ vào", "Giờ thanh toán", "Số hóa đơn",
                            "Tiền hàng trước thuế", "Tiền thuế", "Chiết khấu", "Tổng tiền thanh toán",
                            "Bàn", "Số khách", "Hình thức phục vụ", "Khu vực", "Tiền mặt",
                            "Chuyển khoản", "Voucher", "Khách hàng", "Thu ngân", "Phục vụ",
                            "Đối tác giao hàng", "CTKM", "Ghi chú"
                        };
                        for (int col = 0; col < headers.Length; col++)
                        {
                            var cell = worksheet.Cell(1, col + 1);
                            cell.Value = headers[col];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        int rowIdx = 2;
                        foreach (var item in items)
                        {
                            worksheet.Cell(rowIdx, 1).Value = rowIdx - 1;
                            worksheet.Cell(rowIdx, 2).Value = item.OrderDate.ToString("dd/MM/yyyy");
                            worksheet.Cell(rowIdx, 3).Value = item.OpenTime?.ToString("HH:mm") ?? "";
                            worksheet.Cell(rowIdx, 4).Value = item.PaymentTime?.ToString("HH:mm") ?? "";
                            worksheet.Cell(rowIdx, 5).Value = item.OrderCode;
                            worksheet.Cell(rowIdx, 6).Value = item.PreTaxAmount;
                            worksheet.Cell(rowIdx, 7).Value = item.TaxAmount;
                            worksheet.Cell(rowIdx, 8).Value = item.DiscountAmount;
                            worksheet.Cell(rowIdx, 9).Value = item.TotalAmount;
                            worksheet.Cell(rowIdx, 10).Value = item.TableInfo;
                            worksheet.Cell(rowIdx, 11).Value = item.CustomerCount;
                            worksheet.Cell(rowIdx, 12).Value = item.ServiceType;
                            worksheet.Cell(rowIdx, 13).Value = item.AreaName;
                            worksheet.Cell(rowIdx, 14).Value = item.CashAmount;
                            worksheet.Cell(rowIdx, 15).Value = item.TransferAmount;
                            worksheet.Cell(rowIdx, 16).Value = item.VoucherAmount;
                            worksheet.Cell(rowIdx, 17).Value = item.CustomerInfo;
                            worksheet.Cell(rowIdx, 18).Value = item.CashierName;
                            worksheet.Cell(rowIdx, 19).Value = item.WaiterName;
                            worksheet.Cell(rowIdx, 20).Value = item.DeliveryPartner;
                            worksheet.Cell(rowIdx, 21).Value = item.PromotionInfo;
                            worksheet.Cell(rowIdx, 22).Value = item.Note;

                            // Number formatting
                            for (int col = 6; col <= 9; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Cell(rowIdx, 11).Style.NumberFormat.Format = "#,##0";
                            for (int col = 14; col <= 16; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            rowIdx++;
                        }

                        // Summary row
                        if (summary != null)
                        {
                            worksheet.Cell(rowIdx, 1).Value = "Tổng cộng";
                            worksheet.Cell(rowIdx, 1).Style.Font.Bold = true;
                            worksheet.Range(rowIdx, 1, rowIdx, 5).Merge();

                            worksheet.Cell(rowIdx, 6).Value = summary.TotalPreTaxAmount;
                            worksheet.Cell(rowIdx, 7).Value = summary.TotalTaxAmount;
                            worksheet.Cell(rowIdx, 8).Value = summary.TotalDiscountAmount;
                            worksheet.Cell(rowIdx, 9).Value = summary.TotalTotalAmount;
                            worksheet.Cell(rowIdx, 11).Value = summary.TotalCustomerCount;
                            worksheet.Cell(rowIdx, 14).Value = summary.TotalCashAmount;
                            worksheet.Cell(rowIdx, 15).Value = summary.TotalTransferAmount;
                            worksheet.Cell(rowIdx, 16).Value = summary.TotalVoucherAmount;

                            for (int col = 6; col <= 9; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.Font.Bold = true;
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Cell(rowIdx, 11).Style.Font.Bold = true;
                            worksheet.Cell(rowIdx, 11).Style.NumberFormat.Format = "#,##0";
                            for (int col = 14; col <= 16; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.Font.Bold = true;
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        }
                        worksheet.Columns().AdjustToContents();
                    }
                    else if (filter.ReportType == 4)
                    {
                        var worksheet = workbook.Worksheets.Add("ChiTietDoanhThuTheoMatHang");
                        // 1. Fetch data
                        var detailFilter = new DetailedReportFilterModel
                        {
                            BranchId = branchId,
                            FromDate = filter.FromDate,
                            ToDate = filter.ToDate,
                            PageNumber = 1,
                            PageSize = 1000000
                        };
                        var res = await GetDetailedRevenueReport(detailFilter);
                        var data = (res as ResponseObject<DetailedRevenueResponseDto>)?.Data;
                        var invoices = data?.Invoices ?? new List<DetailedRevenueInvoiceDto>();
                        var summary = data?.Summary;

                        // Header Row 1: Single & Group Headers
                        worksheet.Range(1, 1, 2, 1).Merge().Value = "Ngày";
                        worksheet.Range(1, 2, 2, 2).Merge().Value = "Giờ vào - ra";
                        worksheet.Range(1, 3, 2, 3).Merge().Value = "Số hóa đơn";
                        worksheet.Range(1, 4, 2, 4).Merge().Value = "Mã món";
                        worksheet.Range(1, 5, 2, 5).Merge().Value = "Tên món";
                        worksheet.Range(1, 6, 2, 6).Merge().Value = "ĐVT";
                        worksheet.Range(1, 7, 2, 7).Merge().Value = "SL bán";
                        worksheet.Range(1, 8, 2, 8).Merge().Value = "Đơn giá";

                        // Doanh thu Group Header
                        worksheet.Range(1, 9, 1, 13).Merge().Value = "Doanh thu";

                        worksheet.Range(1, 14, 2, 14).Merge().Value = "Loại mặt hàng";
                        worksheet.Range(1, 15, 2, 15).Merge().Value = "Nhóm thực đơn";
                        worksheet.Range(1, 16, 2, 16).Merge().Value = "Khách hàng";
                        worksheet.Range(1, 17, 2, 17).Merge().Value = "Số người";
                        worksheet.Range(1, 18, 2, 18).Merge().Value = "Hình thức PV";
                        worksheet.Range(1, 19, 2, 19).Merge().Value = "Khu vực";
                        worksheet.Range(1, 20, 2, 20).Merge().Value = "Bàn";
                        worksheet.Range(1, 21, 2, 21).Merge().Value = "Ca";
                        worksheet.Range(1, 22, 2, 22).Merge().Value = "Thu ngân";
                        worksheet.Range(1, 23, 2, 23).Merge().Value = "Phục vụ";

                        // Header Row 2: Sub-headers for Doanh thu group
                        worksheet.Cell(2, 9).Value = "Tổng";
                        worksheet.Cell(2, 10).Value = "Tiền hàng trước thuế";
                        worksheet.Cell(2, 11).Value = "Tiền thuế GTGT";
                        worksheet.Cell(2, 12).Value = "Khuyến mại trước thuế";
                        worksheet.Cell(2, 13).Value = "Thuế suất GTGT";

                        // Style Headers
                        var headerRange = worksheet.Range(1, 1, 2, 23);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        int rowIdx = 3;
                        foreach (var invoice in invoices)
                        {
                            // 1. Master Row
                            worksheet.Cell(rowIdx, 1).Value = invoice.OrderDate.ToString("dd/MM/yyyy");
                            worksheet.Cell(rowIdx, 2).Value = $"{invoice.OpenTime:HH:mm} - {(invoice.PaymentTime.HasValue ? invoice.PaymentTime.Value.ToString("HH:mm") : "")}";
                            worksheet.Cell(rowIdx, 3).Value = invoice.OrderCode;

                            // Cols 4-8 empty/dashed
                            for (int col = 4; col <= 8; col++)
                            {
                                worksheet.Cell(rowIdx, col).Value = "—";
                                worksheet.Cell(rowIdx, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }

                            // Doanh thu Master
                            worksheet.Cell(rowIdx, 9).Value = invoice.TotalAmount;
                            worksheet.Cell(rowIdx, 10).Value = invoice.PreTaxAmount;
                            worksheet.Cell(rowIdx, 11).Value = invoice.TaxAmount;
                            worksheet.Cell(rowIdx, 12).Value = invoice.DiscountPreTaxAmount;
                            worksheet.Cell(rowIdx, 13).Value = "—";
                            worksheet.Cell(rowIdx, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Meta Master
                            worksheet.Cell(rowIdx, 14).Value = "—";
                            worksheet.Cell(rowIdx, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Cell(rowIdx, 15).Value = "—";
                            worksheet.Cell(rowIdx, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            worksheet.Cell(rowIdx, 16).Value = string.IsNullOrEmpty(invoice.CustomerName) ? "—" : invoice.CustomerName;
                            worksheet.Cell(rowIdx, 17).Value = invoice.CustomerCount;
                            worksheet.Cell(rowIdx, 18).Value = MapServiceType(invoice.ServiceType);
                            worksheet.Cell(rowIdx, 19).Value = string.IsNullOrEmpty(invoice.FloorName) ? "—" : invoice.FloorName;
                            worksheet.Cell(rowIdx, 20).Value = string.IsNullOrEmpty(invoice.TableName) ? "—" : invoice.TableName;
                            worksheet.Cell(rowIdx, 21).Value = string.IsNullOrEmpty(invoice.ShiftName) ? "—" : invoice.ShiftName;
                            worksheet.Cell(rowIdx, 22).Value = string.IsNullOrEmpty(invoice.CashierName) ? "—" : invoice.CashierName;
                            worksheet.Cell(rowIdx, 23).Value = string.IsNullOrEmpty(invoice.ServerName) ? "—" : invoice.ServerName;

                            // Style Master Row
                            var masterRow = worksheet.Row(rowIdx);
                            masterRow.Style.Font.Bold = true;
                            masterRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF"); // light blue background

                            // Number formatting
                            for (int col = 9; col <= 12; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }
                            worksheet.Cell(rowIdx, 17).Style.NumberFormat.Format = "#,##0";
                            rowIdx++;

                            // 2. Detail Rows
                            foreach (var detail in invoice.Details)
                            {
                                worksheet.Cell(rowIdx, 1).Value = invoice.OrderDate.ToString("dd/MM/yyyy");
                                worksheet.Cell(rowIdx, 2).Value = $"{invoice.OpenTime:HH:mm} - {(invoice.PaymentTime.HasValue ? invoice.PaymentTime.Value.ToString("HH:mm") : "")}";
                                worksheet.Cell(rowIdx, 3).Value = invoice.OrderCode;

                                worksheet.Cell(rowIdx, 4).Value = detail.ItemCode;
                                worksheet.Cell(rowIdx, 5).Value = detail.ItemName;
                                worksheet.Cell(rowIdx, 6).Value = string.IsNullOrEmpty(detail.UnitName) ? "—" : detail.UnitName;
                                worksheet.Cell(rowIdx, 7).Value = detail.Quantity;
                                worksheet.Cell(rowIdx, 8).Value = detail.UnitPrice;

                                worksheet.Cell(rowIdx, 9).Value = detail.TotalAmount;
                                worksheet.Cell(rowIdx, 10).Value = detail.PreTaxAmount;
                                worksheet.Cell(rowIdx, 11).Value = detail.TaxAmount;
                                worksheet.Cell(rowIdx, 12).Value = detail.DiscountPreTaxAmount;
                                worksheet.Cell(rowIdx, 13).Value = detail.TaxRate > 0 ? $"{detail.TaxRate} %" : "0 %";
                                worksheet.Cell(rowIdx, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                worksheet.Cell(rowIdx, 14).Value = string.IsNullOrEmpty(detail.ItemType) ? "Món ăn" : detail.ItemType;
                                worksheet.Cell(rowIdx, 15).Value = string.IsNullOrEmpty(detail.MenuGroup) ? "—" : detail.MenuGroup;
                                worksheet.Cell(rowIdx, 16).Value = "—";
                                worksheet.Cell(rowIdx, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                worksheet.Cell(rowIdx, 17).Value = 0;
                                worksheet.Cell(rowIdx, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                worksheet.Cell(rowIdx, 18).Value = MapServiceType(invoice.ServiceType);
                                worksheet.Cell(rowIdx, 19).Value = "—";
                                worksheet.Cell(rowIdx, 19).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                worksheet.Cell(rowIdx, 20).Value = string.IsNullOrEmpty(invoice.TableName) ? "—" : invoice.TableName;
                                worksheet.Cell(rowIdx, 21).Value = string.IsNullOrEmpty(invoice.ShiftName) ? "—" : invoice.ShiftName;
                                worksheet.Cell(rowIdx, 22).Value = string.IsNullOrEmpty(invoice.CashierName) ? "—" : invoice.CashierName;
                                worksheet.Cell(rowIdx, 23).Value = "—";
                                worksheet.Cell(rowIdx, 23).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Formatting
                                worksheet.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0.00";
                                worksheet.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0";
                                for (int col = 9; col <= 12; col++)
                                {
                                    worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                                }
                                rowIdx++;
                            }
                        }

                        // 3. Global Summary Footer Row
                        if (summary != null)
                        {
                            worksheet.Range(rowIdx, 1, rowIdx, 8).Merge().Value = "TỔNG CỘNG HÓA ĐƠN";
                            worksheet.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            worksheet.Cell(rowIdx, 9).Value = summary.TotalTotalAmount;
                            worksheet.Cell(rowIdx, 10).Value = summary.TotalPreTaxAmount;
                            worksheet.Cell(rowIdx, 11).Value = summary.TotalTaxAmount;
                            worksheet.Cell(rowIdx, 12).Value = summary.TotalDiscountPreTaxAmount;

                            for (int col = 9; col <= 12; col++)
                            {
                                worksheet.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                            }

                            var summaryRow = worksheet.Row(rowIdx);
                            summaryRow.Style.Font.Bold = true;
                            summaryRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        }
                        worksheet.Columns().AdjustToContents();
                        foreach (var col in worksheet.ColumnsUsed())
                        {
                            col.Width = Math.Max(col.Width + 4, 15);
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.ExportExcelAsync error", ex);
                return null;
            }
        }

        public async Task<Response> GetDashboardSummary(string? branchCode, DateTime? date)
        {
            try
            {
                var targetDate = (date ?? DateTime.Today).Date;
                var yesterday = targetDate.AddDays(-1);

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                var itemRepo = _unit.GetRepository<SalesOrderItem>();
                var tableRepo = _unit.GetRepository<DiningTable>();

                var todayOrdersQuery = orderRepo.Get(x => x.OrderDate == targetDate);
                if (!string.IsNullOrWhiteSpace(branchCode))
                {
                    todayOrdersQuery = todayOrdersQuery.Where(x => x.BranchCode == branchCode);
                }

                var todayOrders = await todayOrdersQuery.AsNoTracking().ToListAsync();

                var todayRevenue = todayOrders
                    .Where(x => x.Status == "Paid" || x.Status == "Completed")
                    .Sum(x => x.TotalAmount);

                var yesterdayOrdersQuery = orderRepo.Get(x => x.OrderDate == yesterday);
                if (!string.IsNullOrWhiteSpace(branchCode))
                {
                    yesterdayOrdersQuery = yesterdayOrdersQuery.Where(x => x.BranchCode == branchCode);
                }
                var yesterdayOrders = await yesterdayOrdersQuery.AsNoTracking().ToListAsync();
                var yesterdayRevenue = yesterdayOrders
                    .Where(x => x.Status == "Paid" || x.Status == "Completed")
                    .Sum(x => x.TotalAmount);

                var todayOrderCount = todayOrders.Count;
                var servingOrderCount = todayOrders.Count(x => x.Status == "Serving");
                var waitingPaymentCount = todayOrders.Count(x => x.Status == "WaitingPayment");
                var qrOrderCount = todayOrders.Count(x => (x.ServiceType != null && x.ServiceType.Contains("QR")) || (x.Note != null && x.Note.Contains("QR")));

                var tablesQuery = tableRepo.Get(x => x.Status == 1);
                if (!string.IsNullOrWhiteSpace(branchCode) && Guid.TryParse(branchCode, out var branchGuid))
                {
                    tablesQuery = tablesQuery.Where(x => x.BranchId == branchGuid);
                }
                var tables = await tablesQuery.AsNoTracking().ToListAsync();
                var totalTables = tables.Count;

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var activeOrderIds = todayOrders.Where(x => x.Status == "Serving" || x.Status == "WaitingPayment").Select(x => x.Id).ToList();
                var occupiedTablesCount = await orderTableRepo.Get(x => activeOrderIds.Contains(x.OrderId))
                    .AsNoTracking()
                    .Select(x => x.TableId)
                    .Distinct()
                    .CountAsync();

                var hourlySalesList = new List<HourlySalesDto>();
                var hours = new[] { 8, 10, 12, 14, 16, 18, 20, 22 };

                foreach (var h in hours)
                {
                    var revToday = todayOrders
                        .Where(x => (x.Status == "Paid" || x.Status == "Completed") && x.OpenTime.Hour >= h && x.OpenTime.Hour < h + 2)
                        .Sum(x => x.TotalAmount);

                    var revYesterday = yesterdayOrders
                        .Where(x => (x.Status == "Paid" || x.Status == "Completed") && x.OpenTime.Hour >= h && x.OpenTime.Hour < h + 2)
                        .Sum(x => x.TotalAmount);

                    hourlySalesList.Add(new HourlySalesDto
                    {
                        HourLabel = $"{h:D2}:00",
                        RevenueToday = revToday,
                        RevenueYesterday = revYesterday
                    });
                }

                var orderIds = todayOrders.Select(x => x.Id).ToList();
                var payments = await paymentRepo.Get(x => orderIds.Contains(x.OrderId)).AsNoTracking().ToListAsync();
                var totalPaymentSum = payments.Sum(p => p.PaymentAmount);
                var paymentGroups = payments
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.PaymentType) ? "Khác" : p.PaymentType)
                    .Select(g => new PaymentMethodSummaryDto
                    {
                        MethodName = g.Key,
                        TotalAmount = g.Sum(x => x.PaymentAmount),
                        Percentage = totalPaymentSum > 0 ? (double)Math.Round((g.Sum(x => x.PaymentAmount) / totalPaymentSum) * 100, 1) : 0
                    })
                    .ToList();

                var recentTxs = todayOrders
                    .OrderByDescending(x => x.OpenTime)
                    .Take(10)
                    .Select(x => new DashboardRecentTransactionDto
                    {
                        Id = x.Id.ToString(),
                        OrderCode = x.OrderCode ?? $"#HD-{x.Id.ToString().Substring(0, 4)}",
                        TableName = x.FloorName ?? "Bàn Ăn",
                        Time = x.OpenTime.ToString("HH:mm"),
                        Amount = x.TotalAmount,
                        PaymentMethod = x.ServiceType ?? "Tại bàn",
                        Status = x.Status ?? "Paid"
                    })
                    .ToList();

                var topItems = await itemRepo.Get(x => orderIds.Contains(x.OrderId))
                    .AsNoTracking()
                    .GroupBy(x => new { x.ItemId, x.ItemName })
                    .Select(g => new
                    {
                        ItemName = g.Key.ItemName,
                        GroupName = "Thực đơn",
                        SoldQty = g.Sum(x => (int)x.Quantity),
                        TotalAmount = g.Sum(x => x.Amount)
                    })
                    .OrderByDescending(g => g.SoldQty)
                    .Take(5)
                    .ToListAsync();

                var topMenuItemsList = topItems.Select((x, idx) => new DashboardTopMenuItemDto
                {
                    Rank = idx + 1,
                    ItemName = x.ItemName,
                    GroupName = x.GroupName,
                    SoldQty = x.SoldQty,
                    TotalAmount = x.TotalAmount
                }).ToList();

                var dto = new DashboardSummaryDto
                {
                    TodayRevenue = todayRevenue,
                    YesterdayRevenue = yesterdayRevenue,
                    TodayOrderCount = todayOrderCount,
                    ServingOrderCount = servingOrderCount,
                    WaitingPaymentCount = waitingPaymentCount,
                    OccupiedTables = occupiedTablesCount,
                    TotalTables = totalTables,
                    QrOrderCount = qrOrderCount,
                    HourlySales = hourlySalesList,
                    PaymentMethods = paymentGroups,
                    RecentTransactions = recentTxs,
                    TopMenuItems = topMenuItemsList
                };

                return new ResponseObject<DashboardSummaryDto>(dto, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ReportHandler.GetDashboardSummary", ex);
                return new Response(Code.ServerError, ex.Message);
            }
        }

        private static string MapServiceType(string? serviceType)
        {
            if (string.IsNullOrEmpty(serviceType)) return "—";
            if (serviceType.Equals("DineIn", StringComparison.OrdinalIgnoreCase)) return "Ngồi tại bàn";
            if (serviceType.Equals("TakeAway", StringComparison.OrdinalIgnoreCase)) return "Mang về";
            if (serviceType.Equals("Delivery", StringComparison.OrdinalIgnoreCase)) return "Giao hàng";
            return serviceType;
        }

        private string MapItemType(string? rawType)
        {
            if (string.IsNullOrEmpty(rawType)) return "Chưa xác định";
            if (rawType == "1" || rawType.Equals("Món ăn", StringComparison.OrdinalIgnoreCase)) return "Món ăn";
            if (rawType == "2" || rawType.Equals("Đồ uống", StringComparison.OrdinalIgnoreCase)) return "Đồ uống";
            if (rawType == "3" || rawType.Equals("Combo", StringComparison.OrdinalIgnoreCase)) return "Combo";
            if (rawType == "4" || rawType.Equals("Khác", StringComparison.OrdinalIgnoreCase)) return "Khác";
            
            if (int.TryParse(rawType, out int typeVal))
            {
                return typeVal switch
                {
                    1 => "Món ăn",
                    2 => "Đồ uống",
                    3 => "Combo",
                    4 => "Khác",
                    _ => "Khác"
                };
            }
            return rawType;
        }
    }
}
