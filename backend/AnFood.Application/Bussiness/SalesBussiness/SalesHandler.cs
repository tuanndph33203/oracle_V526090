using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Domain.Enums;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AnFood.Application.Bussiness.KitchenBussiness;

namespace AnFood.Application.Bussiness.SalesBussiness
{
    public class SalesHandler : ISalesHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly AnFood.Application.Bussiness.MenuVisibility.MenuVisibilityService _visibility;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;
        private readonly IKitchenHubSender _kitchenHubSender;
        private readonly IConfiguration? _config;

        public SalesHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit, IKitchenHubSender kitchenHubSender, AnFood.Application.Bussiness.MenuVisibility.MenuVisibilityService visibility, IConfiguration? config = null)
        {
            _unit = unit;
            _visibility = visibility;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
            _kitchenHubSender = kitchenHubSender;
            _config = config;
        }

        private async Task<bool> IsRequireKitchenSendEnabled()
        {
            var setting = await _unit.GetRepository<SystemSetting>()
                .Get(x => x.Code.ToLower() == Constant.SystemSettingCodes.SaleRequireKitchen.ToLower())
                .FirstOrDefaultAsync();
            if (setting == null)
                return true;
            return Constant.SystemSettingAllowsAction(setting.Value) && setting.IsActive;
        }

        private Task BroadcastSalesEventAsync(string branchCode, AnFood.Application.Bussiness.SalesBussiness.SalesRealtimeEventDto eventData)
        {
            return Task.CompletedTask;
        }

        public async Task<Response> GetTableMap(string branchCode, Guid floorId, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                {
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");
                }

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại hoặc đã bị khóa!");
                }

                var floorRepo = _unit.GetRepository<FloorPlan>();
                var floor = await floorRepo.Get(x => x.Id == floorId && x.Status != 0).FirstOrDefaultAsync();
                if (floor == null)
                {
                    return new Response(Code.NotFound, "Sơ đồ phòng không tồn tại hoặc đã bị xóa!");
                }
                if (floor.BranchId != branch.Id)
                {
                    return new Response(Code.BadRequest, "Sơ đồ phòng không thuộc chi nhánh đã chọn!");
                }

                var tableRepo = _unit.GetRepository<DiningTable>();
                var allTables = await tableRepo.Get(x => x.BranchId == branch.Id && x.FloorPlanId == floorId && x.Status != 0).ToListAsync();

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var activeOrders = await orderRepo.Get(x => x.BranchCode == branchCode 
                                                          && x.Status != OrderStatusConstants.Paid 
                                                          && x.Status != OrderStatusConstants.Completed 
                                                          && x.Status != OrderStatusConstants.Cancelled 
                                                          && x.Status != OrderStatusConstants.ApprovedCancelled
                                                          && x.Status != OrderStatusConstants.Merged
                                                          && x.Status != OrderStatusConstants.WaitingConfirm).ToListAsync();
                
                var activeOrderIds = activeOrders.Select(o => o.Id).ToList();
                
                var orderTables = await orderTableRepo.Get(x => x.BranchCode == branchCode 
                                                             && activeOrderIds.Contains(x.OrderId)).ToListAsync();
                
                var orderItems = await orderItemRepo.Get(x => x.BranchCode == branchCode 
                                                           && activeOrderIds.Contains(x.OrderId)).ToListAsync();

                var waitingConfirmOrders = await orderRepo.Get(x => x.BranchCode == branchCode 
                                                                  && x.Status == OrderStatusConstants.WaitingConfirm).ToListAsync();
                var waitingConfirmOrderIds = waitingConfirmOrders.Select(o => o.Id).ToList();
                var waitingConfirmTables = await orderTableRepo.Get(x => x.BranchCode == branchCode 
                                                                      && waitingConfirmOrderIds.Contains(x.OrderId)).ToListAsync();

                var response = new SaleTableMapResponse
                {
                    BranchCode = branchCode,
                    FloorId = floorId,
                    StatusFilter = status
                };

                foreach (var t in allTables)
                {
                    var linkedWaitingConfirmOrderIds = waitingConfirmTables.Where(ot => ot.TableId == t.Id).Select(ot => ot.OrderId).ToList();
                    var waitingConfirmCount = waitingConfirmOrders.Count(o => linkedWaitingConfirmOrderIds.Contains(o.Id));

                    var linkedOrderIds = orderTables.Where(ot => ot.TableId == t.Id).Select(ot => ot.OrderId).ToList();
                    var bills = new List<SaleBillDetailModel>();

                    foreach (var orderId in linkedOrderIds)
                    {
                        var order = activeOrders.FirstOrDefault(o => o.Id == orderId);
                        if (order == null) continue;

                        var items = orderItems.Where(oi => oi.OrderId == orderId && oi.IsCancelled == 0)
                                              .OrderBy(oi => oi.LineNo)
                                              .Select(oi => new SaleBillItemModel
                                              {
                                                  Id = oi.Id,
                                                  ItemId = oi.ItemId,
                                                  ItemCode = oi.ItemCode,
                                                  ItemName = oi.ItemName,
                                                  UnitName = oi.UnitName,
                                                  Quantity = oi.Quantity,
                                                  Price = oi.UnitPrice,
                                                  SelectedOptionsJson = oi.SelectedOptionsJson,
                                                  Amount = oi.Amount,
                                                  ServedQuantity = oi.Quantity,
                                                  RemainingQuantity = 0,
                                                  Note = oi.Note,
                                                  DiscountRate = oi.DiscountRate,
                                                  DiscountAmount = oi.DiscountAmount,
                                                  VatRate = oi.VatRate,
                                                  VatAmount = oi.VatAmount,
                                                  TotalAmount = oi.TotalAmount,
                                                  IsSentToKitchen = oi.IsSentToKitchen,
                                                  KitchenStatus = oi.KitchenStatus,
                                                  SentQty = oi.SentQty,
                                                  ServedQty = oi.ServedQty,
                                                  SoldOutQty = oi.SoldOutQty,
                                                  RemainingQty = oi.RemainingQty
                                              }).ToList();

                        var orderMapTables = orderTables.Where(ot => ot.OrderId == orderId).ToList();
                        var combinedTableName = string.Join(" + ", orderMapTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                        var combinedTableCode = string.Join(" + ", orderMapTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableCode));

                        bills.Add(new SaleBillDetailModel
                        {
                            BillId = order.Id.ToString(),
                            BillCode = order.OrderCode,
                            Status = order.Status,
                            CustomerCount = order.CustomerCount,
                            OpenTime = order.OpenTime,
                            WaiterId = order.WaiterId,
                            WaiterName = order.WaiterName,
                            Items = items,
                            SubTotal = order.SubTotal > 0 ? order.SubTotal : items.Sum(i => (decimal)i.Quantity * i.Price),
                            DiscountAmount = order.DiscountAmount,
                            VatAmount = order.VatAmount,
                            TotalAmount = order.TotalAmount > 0 ? order.TotalAmount : items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : ((decimal)i.Quantity * i.Price)),
                            TableName = combinedTableName,
                            TableCode = combinedTableCode,
                            TableIds = orderMapTables.Select(ot => ot.TableId).ToList(),
                            TableNames = orderMapTables.Select(ot => ot.TableName).ToList(),
                            SourceTableName = combinedTableName,
                            OriginalCustomerCount = order.CustomerCount,
                            SelectedPromotionCode = order.SelectedPromotionCode,
                            TherapyVoucherCode = order.TherapyVoucherCode
                        });
                    }

                    var earliestActiveBill = bills
                        .OrderBy(b => b.OpenTime)
                        .FirstOrDefault();
                    var activeCustomerCount = bills.Sum(b => b.CustomerCount);

                    var tableDetail = new SaleTableDetailModel
                    {
                        TableId = t.Id,
                        TableCode = t.Code,
                        TableName = t.Name,
                        Status = earliestActiveBill?.Status ?? "Empty",
                        CustomerCount = earliestActiveBill?.CustomerCount ?? 0,
                        OpenTime = earliestActiveBill?.OpenTime,
                        X = t.X,
                        Y = t.Y,
                        Width = t.Width,
                        Height = t.Height,
                        Rotate = t.Rotate,
                        CurrentBills = bills,
                        Capacity = t.Capacity,
                        ActiveOrderCount = bills.Count,
                        ActiveCustomerCount = activeCustomerCount,
                        RemainingCapacity = Math.Max(0, t.Capacity - activeCustomerCount),
                        ActiveOrders = bills.Select(b => new ActiveOrderMapInfo
                        {
                            OrderId = Guid.Parse(b.BillId),
                            OrderCode = b.BillCode,
                            Status = b.Status,
                            CustomerCount = b.CustomerCount,
                            OpenTime = b.OpenTime
                        }).ToList(),
                        WaitingConfirmCount = waitingConfirmCount
                    };

                    bool matchesFilter = status.ToLower() switch
                    {
                        "all" => true,
                        "empty" => tableDetail.Status == "Empty",
                        "waitingservice" => tableDetail.Status == "WaitingService",
                        "serving" => tableDetail.Status == "Serving",
                        "reserved" => tableDetail.Status == "Reserved",
                        "waitingpayment" => tableDetail.Status == "WaitingPayment",
                        _ => true
                    };

                    if (matchesFilter)
                    {
                        response.Tables.Add(tableDetail);
                    }
                }

                return new ResponseObject<SaleTableMapResponse>
                {
                    Code = Code.Success,
                    Message = "Lấy sơ đồ bàn thành công!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.GetTableMap", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> OpenTable(OpenTableRequest request)
        {
            var dineInReq = new DineInOpenTableRequest
            {
                TableIds = request.TableIds,
                BranchCode = request.BranchCode,
                CustomerCount = request.CustomerCount,
                Force = true
            };
            return await OpenDineInTable(dineInReq);
        }

        public async Task<Response> OpenDineInTable(DineInOpenTableRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BranchCode))
                {
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");
                }

                if (!OrderStatusConstants.CanTransition(null, OrderStatusConstants.WaitingService, "Save"))
                {
                    return new Response(Code.BadRequest, "Không thể chuyển trạng thái sang Chờ phục vụ!");
                }

                bool isTakeAwayOrDelivery = request.ServiceType == "TakeAway" || request.ServiceType == "Delivery";
                bool hasTables = !isTakeAwayOrDelivery && request.TableIds != null && request.TableIds.Any();
                request.TableIds = request.TableIds ?? new List<Guid>();

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == request.BranchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");
                }

                var tableRepo = _unit.GetRepository<DiningTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();

                if (hasTables && !request.Force)
                {
                    var tableIds = request.TableIds;
                    var activeOrderTable = await orderTableRepo.Get(x => tableIds.Contains(x.TableId) && x.BranchCode == request.BranchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged && o.Status != OrderStatusConstants.WaitingConfirm),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => ot)
                        .FirstOrDefaultAsync();

                    if (activeOrderTable != null)
                    {
                        return new Response((Code)409, "Bàn đang có khách, vui lòng xác nhận mở thêm bill mới");
                    }
                }

                var now = DateTime.Now;
                var openTime = request.OpenTime ?? request.OrderDate ?? now;
                if (openTime.Kind == DateTimeKind.Utc) openTime = openTime.ToLocalTime();
                var orderId = Guid.NewGuid();
                var orderCode = await GenerateOrderCode(request.BranchCode, openTime);
                
                Guid floorId = Guid.Empty;
                if (hasTables)
                {
                    var firstTableId = request.TableIds.First();
                    var firstTable = await tableRepo.Get(x => x.Id == firstTableId && x.Status != 0).FirstOrDefaultAsync();
                    if (firstTable == null)
                    {
                        return new Response(Code.NotFound, "Bàn không tồn tại hoặc đã bị xóa!");
                    }
                    floorId = firstTable.FloorPlanId;
                }
                if (floorId == Guid.Empty)
                {
                    var defaultFloor = await _unit.GetRepository<FloorPlan>().Get(x => x.BranchId == branch.Id && x.Status != 0).OrderBy(x => x.Code).FirstOrDefaultAsync();
                    if (defaultFloor != null)
                    {
                        floorId = defaultFloor.Id;
                    }
                }

                var salesOrder = new SalesOrder
                {
                    Id = orderId,
                    BranchCode = request.BranchCode,
                    OrderCode = orderCode,
                    OrderDate = openTime.Date,
                    OpenTime = openTime,
                    WaiterId = request.WaiterId,
                    WaiterName = request.WaiterName,
                    FloorId = floorId,
                    CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : 1,
                    ServiceType = isTakeAwayOrDelivery ? request.ServiceType! : "DineIn",
                    Status = OrderStatusConstants.WaitingService,
                    CreatedOnDate = now,
                    CreatedByUserCode = "system",
                    CreatedByUserName = "System"
                };

                orderRepo.Add(salesOrder);

                if (hasTables)
                {
                    foreach (var tableId in request.TableIds)
                    {
                        var table = await tableRepo.Get(x => x.Id == tableId && x.Status != 0).FirstOrDefaultAsync();
                        if (table == null) continue;

                        var salesOrderTable = new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = request.BranchCode,
                            OrderId = orderId,
                            TableId = tableId,
                            TableCode = table.Code,
                            TableName = table.Name,
                            FloorId = table.FloorPlanId,
                            CreatedOnDate = DateTime.UtcNow
                        };
                        orderTableRepo.Add(salesOrderTable);
                    }
                }

                await _unit.SaveAsync();

                await _logsapp.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=Save, OldStatus=Draft, NewStatus={OrderStatusConstants.WaitingService}");

                var createdBill = new SaleBillDetailModel
                {
                    BillId = orderId.ToString(),
                    BillCode = orderCode,
                    Status = OrderStatusConstants.WaitingService,
                    CustomerCount = salesOrder.CustomerCount,
                    OpenTime = salesOrder.OpenTime,
                    WaiterId = salesOrder.WaiterId,
                    WaiterName = salesOrder.WaiterName,
                    Items = new List<SaleBillItemModel>()
                };

                await _logsaudit.LogCreateAsync("Sales_OpenDineInTable", orderId.ToString(), createdBill, "system", "system");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableOpened",
                    OrderId = orderId,
                    OrderCode = orderCode,
                    NewStatus = OrderStatusConstants.WaitingService,
                    ServiceType = isTakeAwayOrDelivery ? request.ServiceType! : "DineIn"
                });

                return new ResponseObject<SaleBillDetailModel>
                {
                    Code = Code.Success,
                    Message = "Mở bàn thành công!",
                    Data = createdBill
                };
            }
            catch (Exception ex)
            {
                var detailMessage = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                await _logsapp.ErrorAsync("SalesHandler.OpenDineInTable", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + detailMessage);
            }
        }

        public async Task<Response> MergeTablesPreview(MergeTablesRequest request)
        {
            try
            {
                if (request.TableIds == null || request.TableIds.Count < 2)
                {
                    return new Response(Code.BadRequest, "Yêu cầu gộp bàn phải chọn từ 2 bàn trở lên!");
                }

                var tableRepo = _unit.GetRepository<DiningTable>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var tableNames = new List<string>();
                var mergedItems = new List<SaleBillItemModel>();
                int totalCustomerCount = 0;

                foreach (var id in request.TableIds)
                {
                    var table = await tableRepo.Get(x => x.Id == id && x.Status != 0).FirstOrDefaultAsync();
                    if (table == null)
                    {
                        return new Response(Code.NotFound, $"Bàn ID {id} không tồn tại!");
                    }

                    tableNames.Add(table.Name);

                    var activeOrderTable = await orderTableRepo.Get(x => x.TableId == id && x.BranchCode == request.BranchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => new { ot, o })
                        .FirstOrDefaultAsync();

                    if (activeOrderTable == null)
                    {
                        return new Response(Code.BadRequest, $"Bàn {table.Name} đang trống, không thể gộp!");
                    }

                    var order = activeOrderTable.o;
                    totalCustomerCount += order.CustomerCount;

                    var items = await orderItemRepo.Get(x => x.OrderId == order.Id && x.BranchCode == request.BranchCode).ToListAsync();
                    foreach (var item in items)
                    {
                        var existing = mergedItems.FirstOrDefault(x => x.ItemId == item.ItemId && x.SelectedOptionsJson == item.SelectedOptionsJson && x.Price == item.UnitPrice);
                        if (existing != null)
                        {
                            existing.Quantity += item.Quantity;
                            existing.Amount += item.Amount;
                            existing.TotalAmount += item.TotalAmount;
                            existing.SentQty += item.SentQty;
                            existing.ServedQty += item.ServedQty;
                            existing.SoldOutQty += item.SoldOutQty;
                            existing.RemainingQty += item.RemainingQty;
                            existing.IsSentToKitchen = existing.SentQty > 0 ? 1 : 0;
                            existing.KitchenStatus = existing.ServedQty == existing.Quantity ? "FullyServed" : (existing.SentQty == 0 ? "NotSent" : (existing.ServedQty > 0 ? "PartiallyServed" : "Sent"));
                        }
                        else
                        {
                            mergedItems.Add(new SaleBillItemModel
                            {
                                Id = item.Id,
                                ItemId = item.ItemId,
                                ItemCode = item.ItemCode,
                                ItemName = item.ItemName,
                                UnitName = item.UnitName,
                                Price = item.UnitPrice,
                                Quantity = item.Quantity,
                                Amount = item.Amount,
                                ServedQuantity = item.Quantity,
                                RemainingQuantity = 0,
                                Note = item.Note,
                                SelectedOptionsJson = item.SelectedOptionsJson,
                                DiscountRate = item.DiscountRate,
                                DiscountAmount = item.DiscountAmount,
                                VatRate = item.VatRate,
                                VatAmount = item.VatAmount,
                                TotalAmount = item.TotalAmount,
                                IsSentToKitchen = item.IsSentToKitchen,
                                KitchenStatus = item.KitchenStatus,
                                SentQty = item.SentQty,
                                ServedQty = item.ServedQty,
                                SoldOutQty = item.SoldOutQty,
                                RemainingQty = item.RemainingQty
                            });
                        }
                    }
                }

                var preview = new MergePreviewModel
                {
                    SourceTableIds = request.TableIds,
                    TableNames = tableNames,
                    MergedItems = mergedItems,
                    TotalCustomerCount = totalCustomerCount,
                    TotalAmount = mergedItems.Sum(x => x.TotalAmount)
                };

                return new ResponseObject<MergePreviewModel>
                {
                    Code = Code.Success,
                    Message = "Lấy thông tin xem trước gộp bàn thành công!",
                    Data = preview
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.MergeTablesPreview", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> MergeTables(MergeTablesRequest request)
        {
            try
            {
                if (request.TableIds == null || request.TableIds.Count < 2)
                {
                    return new Response(Code.BadRequest, "Yêu cầu gộp bàn phải chọn từ 2 bàn trở lên!");
                }

                var previewResponse = await MergeTablesPreview(request);
                if (previewResponse.Code != Code.Success)
                {
                    return previewResponse;
                }

                if (previewResponse is ResponseObject<MergePreviewModel> objResp)
                {
                    var preview = objResp.Data;
                    var targetTableId = request.TargetTableId != Guid.Empty ? request.TargetTableId : request.TableIds[0];

                    if (!request.TableIds.Contains(targetTableId))
                    {
                        return new Response(Code.BadRequest, "Bàn đích phải nằm trong danh sách bàn được chọn để gộp!");
                    }

                    var orderRepo = _unit.GetRepository<SalesOrder>();
                    var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                    var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                    var orderIdsToMerge = new List<Guid>();
                    Guid? targetOrderId = null;

                    foreach (var id in request.TableIds)
                    {
                        var activeMapping = await orderTableRepo.Get(x => x.TableId == id && x.BranchCode == request.BranchCode)
                            .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                                  ot => ot.OrderId,
                                  o => o.Id,
                                  (ot, o) => o)
                            .FirstOrDefaultAsync();

                        if (activeMapping != null)
                        {
                            orderIdsToMerge.Add(activeMapping.Id);
                            if (id == targetTableId)
                            {
                                targetOrderId = activeMapping.Id;
                            }
                        }
                    }

                    if (targetOrderId == null)
                    {
                        return new Response(Code.BadRequest, "Không tìm thấy hóa đơn hoạt động của bàn đích.");
                    }

                    var targetOrder = await orderRepo.Get(x => x.Id == targetOrderId.Value).FirstOrDefaultAsync();
                    if (targetOrder == null)
                    {
                        return new Response(Code.BadRequest, "Không tìm thấy hóa đơn hoạt động của bàn đích.");
                    }
                    targetOrder.CustomerCount = preview.TotalCustomerCount;
                    
                    var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                    var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();

                    var mergedRequestIds = await kitchenRequestRepo.Get(r => orderIdsToMerge.Contains(r.OrderId) && r.BranchCode == request.BranchCode)
                        .Select(r => r.Id)
                        .ToListAsync();

                    var oldItemsOfTarget = await orderItemRepo.Get(x => x.OrderId == targetOrderId.Value).ToListAsync();
                    foreach (var oi in oldItemsOfTarget)
                    {
                        orderItemRepo.Delete(oi);
                    }

                    decimal subTotal = 0;
                    decimal totalDiscount = 0;
                    decimal totalVat = 0;
                    decimal totalAmount = 0;

                    for (int i = 0; i < preview.MergedItems.Count; i++)
                    {
                        var item = preview.MergedItems[i];
                        var unitVat578 = Math.Round(item.Price * (decimal)(item.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                        var unitGross578 = item.Price + unitVat578;
                        var unitDisc578 = item.DiscountRate > 0 ? Math.Round(unitGross578 * (decimal)(item.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                        var unitTotal578 = Math.Max(0, unitGross578 - unitDisc578);

                        var amt = Math.Round((decimal)item.Quantity * item.Price, 0, MidpointRounding.AwayFromZero);
                        var discAmt = Math.Round((decimal)item.Quantity * unitDisc578, 0, MidpointRounding.AwayFromZero);
                        var vatAmt = Math.Round((decimal)item.Quantity * unitVat578, 0, MidpointRounding.AwayFromZero);
                        var itemTot = Math.Round((decimal)item.Quantity * unitTotal578, 0, MidpointRounding.AwayFromZero);

                        subTotal += amt;
                        totalDiscount += discAmt;
                        totalVat += vatAmt;
                        totalAmount += itemTot;

                        var oi = new SalesOrderItem
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = request.BranchCode,
                            OrderId = targetOrder.Id,
                            LineNo = i + 1,
                            ItemId = item.ItemId,
                            ItemCode = item.ItemCode,
                            ItemName = item.ItemName,
                            UnitName = item.UnitName,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price,
                            SelectedOptionsJson = item.SelectedOptionsJson,
                            Amount = amt,
                            DiscountRate = item.DiscountRate,
                            DiscountAmount = discAmt,
                            VatRate = item.VatRate,
                            VatAmount = vatAmt,
                            TotalAmount = itemTot,
                            IsSentToKitchen = item.IsSentToKitchen,
                            KitchenStatus = item.KitchenStatus,
                            SentQty = item.SentQty,
                            ServedQty = item.ServedQty,
                            SoldOutQty = item.SoldOutQty,
                            RemainingQty = item.RemainingQty,
                            CreatedOnDate = DateTime.Now,
                            UpdatedOnDate = DateTime.Now
                        };
                        orderItemRepo.Add(oi);

                        // Update all request details for this ItemId belonging to any of the merged orders to point to the new oi.Id
                        var detailsToUpdate = await kitchenRequestDetailRepo.Get(x => x.MenuItemId == item.ItemId && mergedRequestIds.Contains(x.KitchenRequestId)).ToListAsync();
                        foreach (var det in detailsToUpdate)
                        {
                            det.OrderDetailId = oi.Id;
                            kitchenRequestDetailRepo.Update(det);
                        }
                    }

                    var targetOldStatus = targetOrder.Status;
                    targetOrder.SubTotal = subTotal;
                    targetOrder.DiscountAmount = totalDiscount;
                    targetOrder.VatAmount = totalVat;
                    targetOrder.TotalAmount = totalAmount;
                    targetOrder.Status = OrderStatusConstants.Serving;
                    orderRepo.Update(targetOrder);

                    foreach (var orderId in orderIdsToMerge)
                    {
                        if (orderId == targetOrderId.Value) continue;

                        var otherOrder = await orderRepo.Get(x => x.Id == orderId).FirstOrDefaultAsync();
                        if (otherOrder != null)
                        {
                            var otherOldStatus = otherOrder.Status;
                            otherOrder.Status = OrderStatusConstants.Merged;
                            otherOrder.UpdatedOnDate = DateTime.Now;
                            orderRepo.Update(otherOrder);
                            await _logsapp.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={request.BranchCode}, Action=Merge, OldStatus={otherOldStatus}, NewStatus={OrderStatusConstants.Merged}");
                        }

                        var sourceTableMappings = await orderTableRepo.Get(x => x.OrderId == orderId).ToListAsync();
                        foreach (var m in sourceTableMappings)
                        {
                            m.OrderId = targetOrderId.Value;
                            orderTableRepo.Update(m);
                        }
                    }

                    // Resolve final target TableName
                    var finalTableName = "Bàn";
                    if (targetOrder.ServiceType == "DineIn")
                    {
                        var targetTables = await orderTableRepo.Get(x => x.OrderId == targetOrderId.Value && x.BranchCode == request.BranchCode).ToListAsync();
                        if (targetTables.Any())
                        {
                            finalTableName = string.Join(" + ", targetTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                        }
                        else
                        {
                            finalTableName = "Tại bàn";
                        }
                    }
                    else if (targetOrder.ServiceType == "TakeAway")
                    {
                        finalTableName = "Mang về";
                    }
                    else if (targetOrder.ServiceType == "Delivery")
                    {
                        finalTableName = "Giao hàng";
                    }

                    // Update kitchen requests OrderId, OrderCode, and TableName
                    var kitchenRequestsToUpdate = await kitchenRequestRepo.Get(x => mergedRequestIds.Contains(x.Id)).ToListAsync();
                    foreach (var req in kitchenRequestsToUpdate)
                    {
                        req.OrderId = targetOrderId.Value;
                        req.OrderCode = targetOrder.OrderCode;
                        req.TableName = finalTableName;
                        kitchenRequestRepo.Update(req);
                    }

                    await _unit.SaveAsync();

                    if (targetOrder.Status != targetOldStatus)
                    {
                        await _logsapp.InfoAsync($"Order status changed: OrderId={targetOrder.Id}, BranchCode={request.BranchCode}, Action=Merge, OldStatus={targetOldStatus}, NewStatus={OrderStatusConstants.Serving}");
                    }

                    // Broadcast kitchen updates via SignalR
                    await _kitchenHubSender.SendRequestsUpdatedAsync(request.BranchCode);

                    await _logsaudit.LogCreateAsync("Sales_MergeTables", targetTableId.ToString(), targetOrder, "system", "system");

                    await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                    {
                        EventType = "TablesMerged",
                        OrderId = targetOrderId,
                        OrderCode = targetOrder.OrderCode,
                        NewStatus = OrderStatusConstants.Serving,
                        ServiceType = targetOrder.ServiceType
                    });

                    return new Response(Code.Success, "Gộp bàn thành công!");
                }

                return new Response(Code.BadRequest, "Dữ liệu xem trước gộp không hợp lệ.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.MergeTables", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> SplitTablePreview(Guid tableId, string branchCode)
        {
            try
            {
                var tableRepo = _unit.GetRepository<DiningTable>();
                var table = await tableRepo.Get(x => x.Id == tableId && x.Status != 0).FirstOrDefaultAsync();
                if (table == null)
                {
                    return new Response(Code.NotFound, "Bàn không tồn tại!");
                }

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var activeOrderTable = await orderTableRepo.Get(x => x.TableId == tableId && x.BranchCode == branchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == branchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => o)
                    .FirstOrDefaultAsync();

                if (activeOrderTable == null)
                {
                    return new Response(Code.BadRequest, "Bàn đang trống, không thể tách!");
                }

                var items = await orderItemRepo.Get(x => x.OrderId == activeOrderTable.Id && x.BranchCode == branchCode)
                                               .OrderBy(x => x.LineNo)
                                               .Select(oi => new SaleBillItemModel
                                               {
                                                   Id = oi.Id,
                                                   ItemId = oi.ItemId,
                                                   ItemCode = oi.ItemCode,
                                                   ItemName = oi.ItemName,
                                                   UnitName = oi.UnitName,
                                                   Quantity = oi.Quantity,
                                                   Price = oi.UnitPrice,
                                                   SelectedOptionsJson = oi.SelectedOptionsJson,
                                                   Amount = oi.Amount,
                                                   ServedQuantity = oi.Quantity,
                                                   RemainingQuantity = 0,
                                                   Note = oi.Note,
                                                   DiscountRate = oi.DiscountRate,
                                                   DiscountAmount = oi.DiscountAmount,
                                                   VatRate = oi.VatRate,
                                                   VatAmount = oi.VatAmount,
                                                   TotalAmount = oi.TotalAmount,
                                                   IsSentToKitchen = oi.IsSentToKitchen,
                                                   KitchenStatus = oi.KitchenStatus,
                                                   SentQty = oi.SentQty,
                                                   ServedQty = oi.ServedQty,
                                                   SoldOutQty = oi.SoldOutQty,
                                                   RemainingQty = oi.RemainingQty
                                               }).ToListAsync();

                var preview = new SplitPreviewModel
                {
                    SourceTableId = tableId,
                    SourceTableName = table.Name,
                    OriginalItems = items,
                    OriginalCustomerCount = activeOrderTable.CustomerCount
                };

                return new ResponseObject<SplitPreviewModel>
                {
                    Code = Code.Success,
                    Message = "Lấy thông tin tách bàn thành công!",
                    Data = preview
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.SplitTablePreview", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> SplitTable(SplitTableRequest request)
        {
            try
            {
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var activeSourceOrder = await orderTableRepo.Get(x => x.TableId == request.TableId && x.BranchCode == request.BranchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => o)
                    .FirstOrDefaultAsync();

                if (activeSourceOrder == null)
                {
                    return new Response(Code.BadRequest, "Bàn nguồn không có hóa đơn để tách!");
                }

                if (request.ItemsToSplit == null || request.ItemsToSplit.Count == 0)
                {
                    return new Response(Code.BadRequest, "Chưa chọn món nào để tách!");
                }

                var sourceItems = await orderItemRepo.Get(x => x.OrderId == activeSourceOrder.Id && x.BranchCode == request.BranchCode).ToListAsync();
                var splitItems = new List<SalesOrderItem>();

                foreach (var splitReq in request.ItemsToSplit)
                {
                    var originalItem = sourceItems.FirstOrDefault(x => splitReq.Id.HasValue ? x.Id == splitReq.Id.Value : x.ItemId == splitReq.ItemId);
                    if (originalItem == null)
                    {
                        return new Response(Code.BadRequest, $"Món có ID {splitReq.ItemId} không tồn tại trong hóa đơn!");
                    }

                    if (splitReq.Quantity <= 0 || splitReq.Quantity > originalItem.Quantity)
                    {
                        return new Response(Code.BadRequest, $"Số lượng tách của {originalItem.ItemName} không hợp lệ (yêu cầu tách {splitReq.Quantity}, hiện có {originalItem.Quantity})!");
                    }

                    double origQty = originalItem.Quantity;
                    double origSent = originalItem.SentQty;
                    double origServed = originalItem.ServedQty;
                    double origSoldOut = originalItem.SoldOutQty;

                    double q = (double)splitReq.Quantity;
                    
                    // Prioritize served quantity
                    double splitServed = Math.Min(q, origServed);
                    
                    // Prioritize sent (but not served) quantity
                    double origSentNotServed = Math.Max(0, origSent - origServed);
                    double splitSentNotServed = Math.Min(q - splitServed, origSentNotServed);
                    
                    double splitUnsent = q - splitServed - splitSentNotServed;

                    // Update original item
                    originalItem.Quantity = origQty - q;
                    originalItem.ServedQty = origServed - splitServed;
                    originalItem.SentQty = origSent - (splitServed + splitSentNotServed);
                    originalItem.RemainingQty = Math.Max(0, originalItem.Quantity - originalItem.ServedQty - originalItem.SoldOutQty);
                    originalItem.IsSentToKitchen = originalItem.SentQty > 0 ? 1 : 0;
                    originalItem.KitchenStatus = originalItem.Quantity <= 0 ? "NotSent" : (originalItem.ServedQty == originalItem.Quantity ? "FullyServed" : (originalItem.SentQty == 0 ? "NotSent" : (originalItem.ServedQty > 0 ? "PartiallyServed" : "Sent")));

                    originalItem.Amount = Math.Round((decimal)originalItem.Quantity * originalItem.UnitPrice, 0, MidpointRounding.AwayFromZero);

                    var menuItemRepo1 = _unit.GetRepository<MenuItem>();
                    var origMi = await menuItemRepo1.Get(x => x.Id == originalItem.ItemId).FirstOrDefaultAsync();
                    decimal unitVat853 = 0;
                    if (origMi != null && origMi.FixedTaxAmount > 0)
                    {
                        unitVat853 = Math.Round(origMi.FixedTaxAmount, 0, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        unitVat853 = Math.Round(originalItem.UnitPrice * (decimal)originalItem.VatRate / 100, 0, MidpointRounding.AwayFromZero);
                    }

                    var unitGross853 = originalItem.UnitPrice + unitVat853;
                    // Fix rounding: tính discount tại cấp unit trước từ giá sau thuế (unitGross)
                    var unitDisc853 = originalItem.DiscountRate > 0 ? Math.Round(unitGross853 * (decimal)originalItem.DiscountRate / 100, 0, MidpointRounding.AwayFromZero) : 0m;
                    originalItem.DiscountAmount = unitDisc853 * (decimal)originalItem.Quantity;

                    var unitTotal853 = Math.Max(0, unitGross853 - unitDisc853);
                    originalItem.VatAmount = Math.Round(unitVat853 * (decimal)originalItem.Quantity, 0, MidpointRounding.AwayFromZero);
                    originalItem.TotalAmount = Math.Round(unitTotal853 * (decimal)originalItem.Quantity, 0, MidpointRounding.AwayFromZero);
                    originalItem.UpdatedOnDate = DateTime.Now;

                    if (originalItem.Quantity <= 0)
                    {
                        orderItemRepo.Delete(originalItem);
                    }
                    else
                    {
                        orderItemRepo.Update(originalItem);
                    }

                    var unitVat889 = Math.Round(originalItem.UnitPrice * (decimal)(originalItem.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                    var unitGross889 = originalItem.UnitPrice + unitVat889;
                    var unitDisc889 = originalItem.DiscountRate > 0 ? Math.Round(unitGross889 * (decimal)(originalItem.DiscountRate / 100), 0, MidpointRounding.AwayFromZero) : 0m;
                    var unitTotal889 = Math.Max(0, unitGross889 - unitDisc889);

                    var amt = Math.Round((decimal)q * originalItem.UnitPrice, 0, MidpointRounding.AwayFromZero);
                    var discAmt = Math.Round((decimal)q * unitDisc889, 0, MidpointRounding.AwayFromZero);
                    var vatAmt = Math.Round((decimal)q * unitVat889, 0, MidpointRounding.AwayFromZero);
                    var totAmt = Math.Round((decimal)q * unitTotal889, 0, MidpointRounding.AwayFromZero);

                    var sItem = new SalesOrderItem
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        ItemId = originalItem.ItemId,
                        ItemCode = originalItem.ItemCode,
                        ItemName = originalItem.ItemName,
                        UnitName = originalItem.UnitName,
                        Quantity = q,
                        UnitPrice = originalItem.UnitPrice,
                        SelectedOptionsJson = originalItem.SelectedOptionsJson,
                        Amount = amt,
                        DiscountRate = originalItem.DiscountRate,
                        DiscountAmount = discAmt,
                        VatRate = originalItem.VatRate,
                        VatAmount = vatAmt,
                        TotalAmount = totAmt,
                        IsSentToKitchen = (splitServed + splitSentNotServed) > 0 ? 1 : 0,
                        KitchenStatus = splitServed == q ? "FullyServed" : ((splitServed + splitSentNotServed) == 0 ? "NotSent" : (splitServed > 0 ? "PartiallyServed" : "Sent")),
                        SentQty = splitServed + splitSentNotServed,
                        ServedQty = splitServed,
                        SoldOutQty = 0,
                        RemainingQty = q - splitServed,
                        CreatedOnDate = DateTime.Now,
                        UpdatedOnDate = DateTime.Now
                    };
                    splitItems.Add(sItem);
                }

                var remainingItems = await orderItemRepo.Get(x => x.OrderId == activeSourceOrder.Id && x.BranchCode == request.BranchCode).ToListAsync();
                activeSourceOrder.SubTotal = remainingItems.Sum(x => x.Amount);
                activeSourceOrder.DiscountAmount = remainingItems.Sum(x => x.DiscountAmount);
                activeSourceOrder.VatAmount = remainingItems.Sum(x => x.VatAmount);
                activeSourceOrder.TotalAmount = remainingItems.Sum(x => x.TotalAmount);
                activeSourceOrder.UpdatedOnDate = DateTime.Now;
                orderRepo.Update(activeSourceOrder);

                bool isTargetOrderNewlyCreated = false;
                SalesOrder targetOrder = null;
                if (request.IsNewBill)
                {
                    isTargetOrderNewlyCreated = true;
                    var tableRepo = _unit.GetRepository<DiningTable>();
                    var targetTable = await tableRepo.Get(x => x.Id == request.TargetTableId && x.Status != 0).FirstOrDefaultAsync();
                    if (targetTable == null)
                    {
                        return new Response(Code.NotFound, "Bàn đích không tồn tại!");
                    }

                    targetOrder = new SalesOrder
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        OrderCode = await GenerateOrderCode(request.BranchCode),
                        OrderDate = DateTime.Now.Date,
                        OpenTime = DateTime.Now,
                        FloorId = targetTable.FloorPlanId,
                        CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : 1,
                        ServiceType = "DineIn",
                        Status = "Serving",
                        CreatedOnDate = DateTime.Now,
                        CreatedByUserCode = "system",
                        CreatedByUserName = "System"
                    };

                    orderRepo.Add(targetOrder);

                    var salesOrderTable = new SalesOrderTable
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        OrderId = targetOrder.Id,
                        TableId = request.TargetTableId,
                        TableCode = targetTable.Code,
                        TableName = targetTable.Name,
                        FloorId = targetTable.FloorPlanId,
                        CreatedOnDate = DateTime.UtcNow
                    };
                    orderTableRepo.Add(salesOrderTable);
                }
                else
                {
                    targetOrder = await orderTableRepo.Get(x => x.TableId == request.TargetTableId && x.BranchCode == request.BranchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => o)
                        .FirstOrDefaultAsync();

                    if (targetOrder == null)
                    {
                        isTargetOrderNewlyCreated = true;
                        var tableRepo = _unit.GetRepository<DiningTable>();
                        var targetTable = await tableRepo.Get(x => x.Id == request.TargetTableId && x.Status != 0).FirstOrDefaultAsync();
                        if (targetTable == null)
                        {
                            return new Response(Code.NotFound, "Bàn đích không tồn tại!");
                        }

                        targetOrder = new SalesOrder
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = request.BranchCode,
                            OrderCode = await GenerateOrderCode(request.BranchCode),
                            OrderDate = DateTime.Now.Date,
                            OpenTime = DateTime.Now,
                            FloorId = targetTable.FloorPlanId,
                            CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : 1,
                            ServiceType = "DineIn",
                            Status = "Serving",
                            CreatedOnDate = DateTime.Now,
                            CreatedByUserCode = "system",
                            CreatedByUserName = "System"
                        };

                        orderRepo.Add(targetOrder);

                        var salesOrderTable = new SalesOrderTable
                        {
                            Id = Guid.NewGuid(),
                            BranchCode = request.BranchCode,
                            OrderId = targetOrder.Id,
                            TableId = request.TargetTableId,
                            TableCode = targetTable.Code,
                            TableName = targetTable.Name,
                            FloorId = targetTable.FloorPlanId,
                            CreatedOnDate = DateTime.UtcNow
                        };
                        orderTableRepo.Add(salesOrderTable);
                    }
                }

                var targetItems = await orderItemRepo.Get(x => x.OrderId == targetOrder.Id && x.BranchCode == request.BranchCode).ToListAsync();
                int nextLineNo = targetItems.Count + 1;

                foreach (var sItem in splitItems)
                {
                    var existingTargetItem = targetItems.FirstOrDefault(x => x.ItemId == sItem.ItemId && x.SelectedOptionsJson == sItem.SelectedOptionsJson && x.UnitPrice == sItem.UnitPrice);
                    if (existingTargetItem != null)
                    {
                        existingTargetItem.Quantity += sItem.Quantity;
                        existingTargetItem.SentQty += sItem.SentQty;
                        existingTargetItem.ServedQty += sItem.ServedQty;
                        existingTargetItem.SoldOutQty += sItem.SoldOutQty;
                        existingTargetItem.RemainingQty = Math.Max(0, existingTargetItem.Quantity - existingTargetItem.ServedQty - existingTargetItem.SoldOutQty);
                        existingTargetItem.IsSentToKitchen = existingTargetItem.SentQty > 0 ? 1 : 0;
                        existingTargetItem.KitchenStatus = existingTargetItem.ServedQty == existingTargetItem.Quantity ? "FullyServed" : (existingTargetItem.SentQty == 0 ? "NotSent" : (existingTargetItem.ServedQty > 0 ? "PartiallyServed" : "Sent"));

                        existingTargetItem.Amount = Math.Round((decimal)existingTargetItem.Quantity * existingTargetItem.UnitPrice, 0, MidpointRounding.AwayFromZero);
                        var menuItemRepo2 = _unit.GetRepository<MenuItem>();
                        var targetMi = await menuItemRepo2.Get(x => x.Id == existingTargetItem.ItemId).FirstOrDefaultAsync();
                        decimal unitVat1031 = 0;
                        if (targetMi != null && targetMi.FixedTaxAmount > 0)
                        {
                            unitVat1031 = Math.Round(targetMi.FixedTaxAmount, 0, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            unitVat1031 = Math.Round(existingTargetItem.UnitPrice * (decimal)existingTargetItem.VatRate / 100, 0, MidpointRounding.AwayFromZero);
                        }

                        var unitGross1031 = existingTargetItem.UnitPrice + unitVat1031;
                        var unitDisc1031 = Math.Round(unitGross1031 * (decimal)existingTargetItem.DiscountRate / 100, 0, MidpointRounding.AwayFromZero);
                        existingTargetItem.DiscountAmount = unitDisc1031 * (decimal)existingTargetItem.Quantity;
                        var unitTotal1031 = Math.Max(0, unitGross1031 - unitDisc1031);

                        existingTargetItem.VatAmount = Math.Round(unitVat1031 * (decimal)existingTargetItem.Quantity, 0, MidpointRounding.AwayFromZero);
                        existingTargetItem.TotalAmount = Math.Round(unitTotal1031 * (decimal)existingTargetItem.Quantity, 0, MidpointRounding.AwayFromZero);
                        existingTargetItem.UpdatedOnDate = DateTime.Now;
                        orderItemRepo.Update(existingTargetItem);
                    }
                    else
                    {
                        sItem.OrderId = targetOrder.Id;
                        sItem.LineNo = nextLineNo++;
                        orderItemRepo.Add(sItem);
                    }
                }

                var newTargetItems = await orderItemRepo.Get(x => x.OrderId == targetOrder.Id && x.BranchCode == request.BranchCode).ToListAsync();
                targetOrder.SubTotal = newTargetItems.Sum(x => x.Amount);
                targetOrder.DiscountAmount = newTargetItems.Sum(x => x.DiscountAmount);
                targetOrder.VatAmount = newTargetItems.Sum(x => x.VatAmount);
                targetOrder.TotalAmount = newTargetItems.Sum(x => x.TotalAmount);
                targetOrder.UpdatedOnDate = DateTime.Now;
                if (!isTargetOrderNewlyCreated)
                {
                    orderRepo.Update(targetOrder);
                }

                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("Sales_SplitTable", request.TableId.ToString(), targetOrder, "system", "system");
                return new Response(Code.Success, "Tách hóa đơn bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.SplitTable", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> CancelTable(CancelTableRequest request)
        {
            try
            {
                if (request.TableId == Guid.Empty)
                {
                    return new Response(Code.BadRequest, "Bàn ID không hợp lệ!");
                }

                var tableRepo = _unit.GetRepository<DiningTable>();
                var table = await tableRepo.Get(x => x.Id == request.TableId && x.Status != 0).FirstOrDefaultAsync();
                if (table == null)
                {
                    return new Response(Code.NotFound, "Bàn không tồn tại!");
                }

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();

                var activeOrder = await orderTableRepo.Get(x => x.TableId == request.TableId && x.BranchCode == request.BranchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => o)
                    .FirstOrDefaultAsync();

                if (activeOrder == null)
                {
                    return new Response(Code.BadRequest, "Bàn đang trống, không có hóa đơn để hủy!");
                }

                var oldStatus = activeOrder.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Cancelled, "Cancel"))
                {
                    return new Response(Code.BadRequest, $"Không thể hủy hóa đơn ở trạng thái '{oldStatus}'!");
                }

                activeOrder.Status = OrderStatusConstants.Cancelled;
                activeOrder.CancelReason = request.Reason;
                activeOrder.UpdatedOnDate = DateTime.Now;
                orderRepo.Update(activeOrder);

                await _unit.SaveAsync();

                await _logsapp.InfoAsync($"Order status changed: OrderId={activeOrder.Id}, BranchCode={request.BranchCode}, Action=Cancel, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Cancelled}");

                await _logsaudit.LogDeleteAsync("Sales_CancelTable", request.TableId.ToString(), null, "system", "system");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderCancelled",
                    OrderId = activeOrder.Id,
                    OrderCode = activeOrder.OrderCode,
                    NewStatus = OrderStatusConstants.Cancelled,
                    ServiceType = activeOrder.ServiceType
                });

                return new Response(Code.Success, "Hủy bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.CancelTable", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> TransferTable(TransferTableRequest request)
        {
            try
            {
                var tableRepo = _unit.GetRepository<DiningTable>();
                var sourceTable = await tableRepo.Get(x => x.Id == request.SourceTableId && x.Status != 0).FirstOrDefaultAsync();
                var targetTable = await tableRepo.Get(x => x.Id == request.TargetTableId && x.Status != 0).FirstOrDefaultAsync();

                if (sourceTable == null || targetTable == null)
                {
                    return new Response(Code.NotFound, "Bàn nguồn hoặc bàn đích không tồn tại!");
                }

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == request.BranchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");
                }

                if (sourceTable.BranchId != branch.Id || targetTable.BranchId != branch.Id)
                {
                    return new Response(Code.BadRequest, "Cả bàn nguồn và bàn đích phải thuộc chi nhánh đã chọn!");
                }

                if (request.SourceTableId == request.TargetTableId)
                {
                    return new Response(Code.BadRequest, "Bàn đích phải khác bàn nguồn!");
                }

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();

                var sourceActiveOrder = await orderTableRepo.Get(x => x.TableId == request.SourceTableId && x.BranchCode == request.BranchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => o)
                    .FirstOrDefaultAsync();

                if (sourceActiveOrder == null)
                {
                    return new Response(Code.BadRequest, "Bàn nguồn đang trống, không thể chuyển!");
                }

                var targetActiveOrder = await orderTableRepo.Get(x => x.TableId == request.TargetTableId && x.BranchCode == request.BranchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == request.BranchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => o)
                    .FirstOrDefaultAsync();

                if (targetActiveOrder != null)
                {
                    return new Response(Code.BadRequest, "Bàn đích đang có người sử dụng, không thể chuyển bàn!");
                }

                var tableMappings = await orderTableRepo.Get(x => x.OrderId == sourceActiveOrder.Id && x.TableId == request.SourceTableId).ToListAsync();
                foreach (var mapping in tableMappings)
                {
                    mapping.TableId = request.TargetTableId;
                    mapping.TableCode = targetTable.Code;
                    mapping.TableName = targetTable.Name;
                    mapping.FloorId = targetTable.FloorPlanId;
                    orderTableRepo.Update(mapping);
                }

                // Cập nhật tên bàn trên các bill bếp (KitchenRequest) chưa hoàn thành
                var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                var activeKitchenRequests = await kitchenRequestRepo
                    .Get(kr => kr.OrderId == sourceActiveOrder.Id && kr.Status != "Completed" && kr.Status != "Cancelled")
                    .ToListAsync();

                foreach (var kr in activeKitchenRequests)
                {
                    kr.TableId = request.TargetTableId;
                    kr.TableName = targetTable.Name;
                    kitchenRequestRepo.Update(kr);
                }

                var sourceTableCode = !string.IsNullOrEmpty(sourceTable.Code) ? sourceTable.Code : sourceTable.Name;
                var autoNote = $"Chuyển từ bàn {sourceTableCode} {sourceActiveOrder.OrderCode}".Trim();
                var transferNoteToSave = string.IsNullOrWhiteSpace(request.Note) ? autoNote : request.Note;

                var existingNote = sourceActiveOrder.Note ?? "";
                var noteParts = existingNote.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => !p.StartsWith("Chuyển từ bàn") && !p.StartsWith("Chuyển từ"))
                    .ToList();

                var baseNote = string.Join(" | ", noteParts);
                var updatedNote = string.IsNullOrWhiteSpace(baseNote)
                    ? transferNoteToSave
                    : $"{baseNote} | {transferNoteToSave}";

                if (updatedNote.Length > 1000)
                {
                    updatedNote = updatedNote.Substring(0, 1000);
                }
                sourceActiveOrder.Note = updatedNote;
                orderRepo.Update(sourceActiveOrder);
                await _unit.SaveAsync();

                try
                {
                    await _kitchenHubSender.SendRequestsUpdatedAsync(request.BranchCode);
                }
                catch (Exception kitchenEx)
                {
                    await _logsapp.ErrorAsync($"TransferTable SendRequestsUpdatedAsync error: {kitchenEx.Message}", kitchenEx);
                }

                await _logsaudit.LogUpdateAsync("Sales_TransferTable", request.SourceTableId.ToString(), request.SourceTableId.ToString(), request.TargetTableId.ToString(), "system", "system");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableTransferred",
                    OrderId = sourceActiveOrder.Id,
                    OrderCode = sourceActiveOrder.OrderCode,
                    NewStatus = sourceActiveOrder.Status,
                    ServiceType = sourceActiveOrder.ServiceType
                });

                return new Response(Code.Success, "Chuyển bàn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.TransferTable", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetAvailableTargetTables(string branchCode, Guid? floorId)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");
                }

                var tableRepo = _unit.GetRepository<DiningTable>();
                var query = tableRepo.Get(x => x.BranchId == branch.Id && x.Status != 0);
                if (floorId.HasValue && floorId.Value != Guid.Empty)
                {
                    query = query.Where(x => x.FloorPlanId == floorId.Value);
                }
                var allTables = await query.ToListAsync();

                var floorRepo = _unit.GetRepository<FloorPlan>();
                var floors = await floorRepo.Get(x => x.BranchId == branch.Id && x.Status != 0).ToListAsync();
                var floorDict = floors.ToDictionary(f => f.Id, f => f.Name);

                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();

                var activeOrderTables = await orderTableRepo.Get(x => x.BranchCode == branchCode)
                    .Join(orderRepo.Get(o => o.BranchCode == branchCode && o.Status != OrderStatusConstants.Paid && o.Status != OrderStatusConstants.Completed && o.Status != OrderStatusConstants.Cancelled && o.Status != OrderStatusConstants.ApprovedCancelled && o.Status != OrderStatusConstants.Merged),
                          ot => ot.OrderId,
                          o => o.Id,
                          (ot, o) => ot.TableId)
                    .ToListAsync();

                var availableTables = new List<SaleTableDetailModel>();
                foreach (var t in allTables)
                {
                    if (!activeOrderTables.Contains(t.Id))
                    {
                        availableTables.Add(new SaleTableDetailModel
                        {
                            TableId = t.Id,
                            TableCode = t.Code,
                            TableName = t.Name,
                            Status = "Empty",
                            FloorPlanId = t.FloorPlanId,
                            FloorPlanName = floorDict.TryGetValue(t.FloorPlanId, out var floorName) ? floorName : "Khác"
                        });
                    }
                }

                return new ResponseObject<List<SaleTableDetailModel>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách bàn trống khả dụng thành công!",
                    Data = availableTables
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.GetAvailableTargetTables", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> CheckItems(Guid tableId, string branchCode)
        {
            try
            {
                var currentBillResp = await GetCurrentBill(tableId, branchCode);
                if (currentBillResp.Code != Code.Success)
                {
                    return currentBillResp;
                }

                if (currentBillResp is ResponseObject<SaleBillDetailModel> objResp && objResp.Data != null)
                {
                    return new ResponseObject<SaleBillDetailModel>
                    {
                        Code = Code.Success,
                        Message = "Lấy danh sách kiểm đồ thành công!",
                        Data = objResp.Data
                    };
                }

                return new Response(Code.BadRequest, "Bàn đang trống!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.CheckItems", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetCurrentBill(Guid tableId, string branchCode)
        {
            try
            {
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var activeOrderTable = await orderTableRepo.Get(x => x.TableId == tableId && x.BranchCode == branchCode).OrderByDescending(x => x.CreatedOnDate).FirstOrDefaultAsync();
                if (activeOrderTable == null)
                {
                    return new ResponseObject<SaleBillDetailModel>
                    {
                        Code = Code.Success,
                        Message = "Bàn đang trống!",
                        Data = null
                    };
                }

                var order = await orderRepo.Get(x => x.Id == activeOrderTable.OrderId 
                                                  && x.BranchCode == branchCode 
                                                  && x.Status != "Paid" 
                                                  && x.Status != "Cancelled" 
                                                  && x.Status != "Merged").FirstOrDefaultAsync();
                
                if (order == null)
                {
                    return new ResponseObject<SaleBillDetailModel>
                    {
                        Code = Code.Success,
                        Message = "Bàn đang trống!",
                        Data = null
                    };
                }

                var items = await orderItemRepo.Get(x => x.OrderId == order.Id && x.BranchCode == branchCode && x.IsCancelled == 0)
                                               .OrderBy(x => x.LineNo)
                                               .Select(oi => new SaleBillItemModel
                                               {
                                                   Id = oi.Id,
                                                   ItemId = oi.ItemId,
                                                   ItemCode = oi.ItemCode,
                                                   ItemName = oi.ItemName,
                                                   UnitName = oi.UnitName,
                                                   Quantity = oi.Quantity,
                                                   Price = oi.UnitPrice,
                                                   SelectedOptionsJson = oi.SelectedOptionsJson,
                                                   Amount = oi.Amount,
                                                   ServedQuantity = oi.Quantity,
                                                   RemainingQuantity = 0,
                                                   Note = oi.Note,
                                                   DiscountRate = oi.DiscountRate,
                                                   DiscountAmount = oi.DiscountAmount,
                                                   VatRate = oi.VatRate,
                                                   VatAmount = oi.VatAmount,
                                                   TotalAmount = oi.TotalAmount,
                                                   IsSentToKitchen = oi.IsSentToKitchen,
                                                   KitchenStatus = oi.KitchenStatus,
                                                   SentQty = oi.SentQty,
                                                   ServedQty = oi.ServedQty,
                                                   SoldOutQty = oi.SoldOutQty,
                                                   RemainingQty = oi.RemainingQty
                                               }).ToListAsync();

                var orderTables = await orderTableRepo.Get(x => x.OrderId == order.Id && x.BranchCode == branchCode).ToListAsync();
                var combinedTableName = string.Join(" + ", orderTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableName));
                var combinedTableCode = string.Join(" + ", orderTables.OrderBy(ot => ot.TableName).Select(ot => ot.TableCode));

                var bill = new SaleBillDetailModel
                {
                    BillId = order.Id.ToString(),
                    BillCode = order.OrderCode,
                    Status = order.Status,
                    CustomerCount = order.CustomerCount,
                    OpenTime = order.OpenTime,
                    WaiterId = order.WaiterId,
                    WaiterName = order.WaiterName,
                    Items = items,
                    SubTotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    VatAmount = order.VatAmount,
                    TotalAmount = order.TotalAmount,
                    TableName = combinedTableName,
                    TableCode = combinedTableCode,
                    TableIds = orderTables.Select(ot => ot.TableId).ToList(),
                    TableNames = orderTables.Select(ot => ot.TableName).ToList(),
                    SourceTableName = combinedTableName,
                    OriginalCustomerCount = order.CustomerCount,
                    Note = order.Note,
                    SelectedPromotionCode = order.SelectedPromotionCode,
                    TherapyVoucherCode = order.TherapyVoucherCode
                };

                return new ResponseObject<SaleBillDetailModel>
                {
                    Code = Code.Success,
                    Message = "Lấy hóa đơn hiện tại thành công!",
                    Data = bill
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.GetCurrentBill", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> SaveDineInOrder(DineInOrderSaveRequest request)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var order = await orderRepo.Get(x => x.Id == request.OrderId && x.BranchCode == request.BranchCode).FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy hóa đơn cần lưu!");
                }

                if (order.Status == OrderStatusConstants.Paid || order.Status == OrderStatusConstants.Completed || order.Status == OrderStatusConstants.Cancelled || order.Status == OrderStatusConstants.ApprovedCancelled || order.Status == OrderStatusConstants.Merged)
                {
                    return new Response(Code.BadRequest, "Hóa đơn đã đóng, không thể chỉnh sửa!");
                }

                order.CustomerCount = request.CustomerCount > 0 ? request.CustomerCount : order.CustomerCount;
                order.WaiterId = request.WaiterId;
                order.WaiterName = request.WaiterName;
                order.UpdatedOnDate = DateTime.Now;
                order.UpdatedByUserCode = "system";
                order.UpdatedByUserName = "System";

                var existingItems = await orderItemRepo.Get(x => x.OrderId == order.Id && x.BranchCode == request.BranchCode).ToListAsync();
                
                foreach (var existingItem in existingItems)
                {
                    orderItemRepo.Delete(existingItem);
                }

                decimal subTotal = 0;
                decimal totalDiscount = 0;
                decimal totalVat = 0;
                decimal totalAmount = 0;

                for (int i = 0; i < request.Items.Count; i++)
                {
                    var item = request.Items[i];
                    if (!item.Id.HasValue && existingItems.Any(x => x.ItemId == item.ItemId && !string.IsNullOrWhiteSpace(x.SelectedOptionsJson)))
                        return new Response(Code.BadRequest, "Cần mã dòng món để cập nhật món có tùy chọn.");
                    var existing = existingItems.FirstOrDefault(x => item.Id.HasValue
                        ? x.Id == item.Id.Value
                        : x.ItemId == item.ItemId && x.UnitPrice == item.UnitPrice);
                    if (existing != null && !string.IsNullOrWhiteSpace(existing.SelectedOptionsJson))
                        item.UnitPrice = existing.UnitPrice;

                    var amount = Math.Round((decimal)item.Quantity * item.UnitPrice, 0, MidpointRounding.AwayFromZero);
                    var menuItemRepo3 = _unit.GetRepository<MenuItem>();
                    var itemMi = await menuItemRepo3.Get(x => x.Id == item.ItemId).FirstOrDefaultAsync();
                    if (existing == null && itemMi != null && !string.IsNullOrWhiteSpace(itemMi.OptionGroupsJson) && itemMi.OptionGroupsJson != "[]")
                        return new Response(Code.BadRequest, "Vui lòng thêm món có tùy chọn qua màn hình gọi món mới.");
                    decimal unitVat1489 = 0;
                    if (itemMi != null && itemMi.FixedTaxAmount > 0)
                    {
                        unitVat1489 = Math.Round(itemMi.FixedTaxAmount, 0, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        unitVat1489 = Math.Round(item.UnitPrice * (decimal)item.VatRate / 100, 0, MidpointRounding.AwayFromZero);
                    }

                    var unitGross1489 = item.UnitPrice + unitVat1489;
                    var unitDisc1489 = Math.Round(unitGross1489 * (decimal)item.DiscountRate / 100, 0, MidpointRounding.AwayFromZero);
                    var discountAmount = unitDisc1489 * (decimal)item.Quantity;
                    var unitTotal1489 = Math.Max(0, unitGross1489 - unitDisc1489);

                    decimal vatAmount = 0;
                    decimal itemTotal = 0;
                    if (item.DiscountRate >= 100 || unitTotal1489 <= 0)
                    {
                        vatAmount = 0;
                        itemTotal = 0;
                    }
                    else
                    {
                        vatAmount = Math.Round(unitVat1489 * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                        itemTotal = Math.Round(unitTotal1489 * (decimal)item.Quantity, 0, MidpointRounding.AwayFromZero);
                    }

                    subTotal += amount;
                    totalDiscount += discountAmount;
                    totalVat += vatAmount;
                    totalAmount += itemTotal;

                    var orderItem = new SalesOrderItem
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = request.BranchCode,
                        OrderId = order.Id,
                        LineNo = i + 1,
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        UnitName = item.UnitName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        SelectedOptionsJson = existing?.SelectedOptionsJson,
                        Amount = amount,
                        DiscountRate = item.DiscountRate,
                        DiscountAmount = discountAmount,
                        VatRate = item.VatRate,
                        VatAmount = vatAmount,
                        TotalAmount = itemTotal,
                        IsSentToKitchen = existing?.IsSentToKitchen ?? 0,
                        KitchenStatus = existing?.KitchenStatus ?? "NotSent",
                        SentQty = existing?.SentQty ?? 0,
                        ServedQty = existing?.ServedQty ?? 0,
                        SoldOutQty = existing?.SoldOutQty ?? 0,
                        RemainingQty = existing?.RemainingQty ?? 0,
                        CreatedOnDate = existing?.CreatedOnDate ?? DateTime.Now,
                        UpdatedOnDate = DateTime.Now
                    };

                    orderItemRepo.Add(orderItem);
                }

                if (totalAmount <= 0)
                {
                    totalAmount = 0;
                    totalVat = 0;
                }

                order.SubTotal = subTotal;
                order.DiscountAmount = totalDiscount;
                order.VatAmount = totalVat;
                order.TotalAmount = totalAmount;

                orderRepo.Update(order);

                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("Sales_SaveDineInOrder", order.Id.ToString(), null, request, "system", "system");

                return new Response(Code.Success, "Lưu hóa đơn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.SaveDineInOrder", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }

        public async Task<Response> SendDineInOrderToKitchen(Guid orderId, string branchCode)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();

                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy hóa đơn!");
                }

                var oldStatus = order.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Serving, "SendKitchenBar"))
                {
                    return new Response(Code.BadRequest, $"Không thể gửi bếp/bar cho hóa đơn ở trạng thái '{oldStatus}'!");
                }

                var items = await orderItemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();
                foreach (var item in items)
                {
                    if (item.IsSentToKitchen == 0 && item.IsCancelled == 0)
                    {
                        item.IsSentToKitchen = 1;
                        item.KitchenStatus = "Sent";
                        item.SentQty = item.Quantity;
                        item.UpdatedOnDate = DateTime.Now;
                        orderItemRepo.Update(item);
                    }
                }

                bool statusChanged = false;
                if (order.Status != OrderStatusConstants.Serving)
                {
                    order.Status = OrderStatusConstants.Serving;
                    order.UpdatedOnDate = DateTime.Now;
                    orderRepo.Update(order);
                    statusChanged = true;
                }

                await _unit.SaveAsync();

                if (statusChanged)
                {
                    await _logsapp.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=SendKitchenBar, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Serving}");
                }

                await _logsaudit.LogUpdateAsync("Sales_SendDineInOrderToKitchen", orderId.ToString(), null, "Sent to kitchen", "system", "system");

                return new Response(Code.Success, "Gửi bếp thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.SendDineInOrderToKitchen", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> PrintTemporaryBill(Guid orderId, string branchCode)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();

                var order = await orderRepo.Get(x => x.Id == orderId && x.BranchCode == branchCode).FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy hóa đơn!");
                }

                var oldStatus = order.Status;

                // Nếu hóa đơn đã ở trạng thái Đã thanh toán (Paid) hoặc Hoàn thành (Completed), đây là thao tác in lại hóa đơn
                if (oldStatus == OrderStatusConstants.Paid || oldStatus == OrderStatusConstants.Completed)
                {
                    await _logsaudit.LogUpdateAsync("Sales_PrintBillReprint", orderId.ToString(), null, "Printed paid bill", "system", "system");
                    return new ResponseObject<string>(string.Empty, "In lại hóa đơn thành công!", Code.Success);
                }

                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.WaitingPayment, "PrintTemporaryBill"))
                {
                    return new Response(Code.BadRequest, $"Không thể in tạm tính cho hóa đơn ở trạng thái '{oldStatus}'!");
                }

                bool requireKitchenPrint = await IsRequireKitchenSendEnabled();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();
                var activeItems = await orderItemRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode && x.IsCancelled == 0).ToListAsync();

                // If there are items not sent to kitchen/bar or quantity is greater than SentQty:
                var unsentItems = activeItems.Where(x => x.Quantity > x.SentQty).ToList();
                if (unsentItems.Any())
                {
                    if (requireKitchenPrint)
                    {
                        var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                        var orderTables = await orderTableRepo.Get(x => x.OrderId == orderId && x.BranchCode == branchCode).ToListAsync();
                        var kitchenRequestRepo = _unit.GetRepository<KitchenRequest>();
                        var kitchenRequestDetailRepo = _unit.GetRepository<KitchenRequestDetail>();
                        var menuItemRepo = _unit.GetRepository<MenuItem>();

                        var now = DateTime.Now;

                        // Group by preparation area
                        var diffItems = new List<(SalesOrderItem Item, double Diff, Guid PrepAreaId)>();
                        foreach (var item in unsentItems)
                        {
                            var diff = item.Quantity - item.SentQty;
                            if (diff > 0)
                            {
                                var menuItem = await menuItemRepo.Get(x => x.Id == item.ItemId).FirstOrDefaultAsync();
                                var areaId = menuItem?.PreparationAreaId ?? Guid.Empty;
                                diffItems.Add((item, diff, areaId));
                            }
                        }

                        if (diffItems.Any())
                        {
                            var groupedDiffs = diffItems.GroupBy(x => x.PrepAreaId);
                            foreach (var group in groupedDiffs)
                            {
                                var areaId = group.Key;
                                var nextSeq = await GetNextKitchenRequestSequence(branchCode, order.OrderCode);
                                var kdRequestCode = $"{order.OrderCode}-{nextSeq:D3}";
                                var newHeaderId = Guid.NewGuid();

                                var kitchenRequest = new KitchenRequest
                                {
                                    Id = newHeaderId,
                                    BranchCode = branchCode,
                                    OrderId = orderId,
                                    OrderCode = order.OrderCode,
                                    KitchenAreaId = areaId,
                                    ServiceType = order.ServiceType,
                                    TableId = orderTables.FirstOrDefault()?.TableId,
                                    TableName = orderTables.FirstOrDefault()?.TableName,
                                    RequestCode = kdRequestCode,
                                    RequestedAt = now,
                                    RequestedBy = "In tạm tính",
                                    Status = "Pending"
                                };
                                kitchenRequestRepo.Add(kitchenRequest);

                                foreach (var diffInfo in group)
                                {
                                    kitchenRequestDetailRepo.Add(new KitchenRequestDetail
                                    {
                                        Id = Guid.NewGuid(),
                                        KitchenRequestId = newHeaderId,
                                        OrderDetailId = diffInfo.Item.Id,
                                        MenuItemId = diffInfo.Item.ItemId,
                                        MenuItemName = diffInfo.Item.ItemName,
                                        RequestQty = diffInfo.Diff,
                                        ServedQty = 0,
                                        RemainingQty = diffInfo.Diff,
                                        Status = "Pending"
                                    });

                                    // Update item status
                                    diffInfo.Item.IsSentToKitchen = 1;
                                    diffInfo.Item.KitchenStatus = "Sent";
                                    diffInfo.Item.SentQty = diffInfo.Item.Quantity;
                                    diffInfo.Item.UpdatedOnDate = now;
                                    orderItemRepo.Update(diffInfo.Item);
                                }
                            }
                        }
                    }
                    else
                    {
                        var now = DateTime.Now;
                        foreach (var item in unsentItems)
                        {
                            item.IsSentToKitchen = 1;
                            item.KitchenStatus = "FullyServed";
                            item.SentQty = item.Quantity;
                            item.ServedQty = item.Quantity;
                            item.RemainingQty = 0;
                            item.UpdatedOnDate = now;
                            orderItemRepo.Update(item);
                        }
                    }
                }
                order.Status = OrderStatusConstants.WaitingPayment;
                order.UpdatedOnDate = DateTime.Now;
                orderRepo.Update(order);

                await _unit.SaveAsync();

                var fullLandingUrl = string.Empty;

                await _logsapp.InfoAsync($"Order status changed: OrderId={orderId}, BranchCode={branchCode}, Action=PrintTemporaryBill, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.WaitingPayment}");
                await _logsaudit.LogUpdateAsync("Sales_PrintTemporaryBill", orderId.ToString(), null, "Printed temporary bill", "system", "system");

                await BroadcastSalesEventAsync(branchCode, new SalesRealtimeEventDto
                {
                    EventType = "TableStatusChanged",
                    OrderId = orderId,
                    NewStatus = OrderStatusConstants.WaitingPayment,
                    ServiceType = order.ServiceType
                });

                return new ResponseObject<string>(fullLandingUrl, "In tạm tính thành công (đã chuyển trạng thái bàn sang Chờ thanh toán)!", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.PrintTemporaryBill", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> PayDineInOrder(DineInPaymentRequest request)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();

                var order = await orderRepo.Get(x => x.Id == request.OrderId && x.BranchCode == request.BranchCode).FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy hóa đơn thanh toán!");
                }

                var oldStatus = order.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Paid, "Payment"))
                {
                    return new Response(Code.BadRequest, $"Không thể thanh toán order ở trạng thái '{oldStatus}'!");
                }

                var payment = new SalesOrderPayment
                {
                    Id = Guid.NewGuid(),
                    BranchCode = request.BranchCode,
                    OrderId = request.OrderId,
                    TransactionId = request.TransactionId,
                    PaymentType = request.PaymentType,
                    PaymentAmount = request.PaymentAmount,
                    CreatedOnDate = DateTime.UtcNow
                };

                paymentRepo.Add(payment);

                order.Status = OrderStatusConstants.Paid;
                order.PaymentTime = DateTime.Now;
                order.UpdatedOnDate = DateTime.Now;
                order.UpdatedByUserCode = "system";
                order.UpdatedByUserName = "System";

                orderRepo.Update(order);

                await _unit.SaveAsync();

                await _logsapp.InfoAsync($"Order status changed: OrderId={request.OrderId}, BranchCode={request.BranchCode}, Action=Payment, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Paid}");

                await _logsaudit.LogCreateAsync("Sales_PayDineInOrder", request.OrderId.ToString(), payment, "system", "system");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderPaid",
                    OrderId = request.OrderId,
                    OrderCode = order.OrderCode,
                    NewStatus = OrderStatusConstants.Paid,
                    ServiceType = order.ServiceType
                });

                return new Response(Code.Success, "Thanh toán hóa đơn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.PayDineInOrder", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> CancelDineInOrder(CancelDineInOrderRequest request)
        {
            try
            {
                var orderRepo = _unit.GetRepository<SalesOrder>();

                var order = await orderRepo.Get(x => x.Id == request.OrderId && x.BranchCode == request.BranchCode).FirstOrDefaultAsync();
                if (order == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy hóa đơn cần hủy!");
                }

                if (order.Status == OrderStatusConstants.Cancelled || order.Status == OrderStatusConstants.ApprovedCancelled)
                {
                    return new Response(Code.BadRequest, "Hóa đơn đã được hủy trước đó!");
                }

                var oldStatus = order.Status;
                if (!OrderStatusConstants.CanTransition(oldStatus, OrderStatusConstants.Cancelled, "Cancel"))
                {
                    return new Response(Code.BadRequest, $"Không thể hủy hóa đơn ở trạng thái '{oldStatus}'!");
                }

                order.Status = OrderStatusConstants.Cancelled;
                order.CancelReason = request.Reason;
                order.UpdatedOnDate = DateTime.Now;
                order.UpdatedByUserCode = "system";
                order.UpdatedByUserName = "System";

                orderRepo.Update(order);

                await _unit.SaveAsync();

                await _logsapp.InfoAsync($"Order status changed: OrderId={request.OrderId}, BranchCode={request.BranchCode}, Action=Cancel, OldStatus={oldStatus}, NewStatus={OrderStatusConstants.Cancelled}");

                await _logsaudit.LogUpdateAsync("Sales_CancelDineInOrder", request.OrderId.ToString(), null, request.Reason, "system", "system");

                await BroadcastSalesEventAsync(request.BranchCode, new SalesRealtimeEventDto
                {
                    EventType = "OrderCancelled",
                    OrderId = request.OrderId,
                    OrderCode = order.OrderCode,
                    NewStatus = OrderStatusConstants.Cancelled,
                    ServiceType = order.ServiceType
                });

                return new Response(Code.Success, "Hủy hóa đơn thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.CancelDineInOrder", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetMenuItems(string branchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchCode))
                {
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");
                }

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Code == branchCode && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, "Chi nhánh không tồn tại!");
                }

                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var menuItemBranchRepo = _unit.GetRepository<MenuItemBranch>();
                var unitRepo = _unit.GetRepository<Unit>();

                var taxRepo = _unit.GetRepository<TaxRate>();
                var taxes = await taxRepo.Get(x => x.Status == 1).ToListAsync();
                var taxDict = taxes.ToDictionary(t => t.Id, t => (decimal)t.Rate);

                var hiddenIds = await _visibility.GetHiddenIdsAsync(branchCode);
                var rawItems = await (from mi in menuItemRepo.Get(x => x.Status == 1 && !hiddenIds.Contains(x.Id))
                                      join u in unitRepo.Get() on mi.UnitId equals u.Id into units
                                      from unit in units.DefaultIfEmpty()
                                      join mib in menuItemBranchRepo.Get() on mi.Id equals mib.MenuItemId into mibJoin
                                      where !mibJoin.Any() || mibJoin.Any(x => x.BranchId == branch.Id)
                                      select new
                                      {
                                          mi.Id,
                                          mi.Code,
                                          mi.Name,
                                          UnitName = unit != null ? unit.Name : null,
                                          mi.SalePrice,
                                          mi.FixedTaxAmount,
                                          mi.Amount,
                                          mi.TaxRateId,
                                          mi.Description,
                                          mi.ImageUrl,
                                          mi.IsAvailable
                                      }).ToListAsync();

                var menuItemsList = rawItems.Select(mi => {
                    decimal finalPrice = mi.Amount > 0 
                        ? mi.Amount 
                        : (mi.FixedTaxAmount > 0 
                            ? mi.SalePrice + mi.FixedTaxAmount 
                            : (mi.TaxRateId.HasValue && taxDict.TryGetValue(mi.TaxRateId.Value, out var rate) 
                                ? Math.Round(mi.SalePrice * (1 + rate / 100), 2) 
                                : mi.SalePrice));

                    return new SaleBillItemModel
                    {
                        ItemId = mi.Id,
                        ItemCode = mi.Code,
                        ItemName = mi.Name,
                        UnitName = mi.UnitName,
                        Quantity = 0,
                        Price = finalPrice,
                        Amount = 0,
                        ServedQuantity = 0,
                        RemainingQuantity = 0,
                        Note = null,
                        Description = mi.Description,
                        ImageUrl = mi.ImageUrl,
                        IsAvailable = mi.IsAvailable
                    };
                }).ToList();

                return new ResponseObject<List<SaleBillItemModel>>
                {
                    Code = Code.Success,
                    Message = "Lấy danh sách thực đơn thành công!",
                    Data = menuItemsList
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.GetMenuItems", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> UpdateBill(UpdateBillRequest request)
        {
            try
            {
                if (request.TableId == Guid.Empty)
                {
                    return new Response(Code.BadRequest, "Mã bàn không hợp lệ!");
                }

                var currentBillResp = await GetCurrentBill(request.TableId, request.BranchCode);
                if (currentBillResp.Code != Code.Success)
                {
                    return currentBillResp;
                }

                if (currentBillResp is ResponseObject<SaleBillDetailModel> objResp && objResp.Data != null)
                {
                    var bill = objResp.Data;
                    var saveReq = new DineInOrderSaveRequest
                    {
                        OrderId = Guid.Parse(bill.BillId),
                        BranchCode = request.BranchCode,
                        CustomerCount = request.CustomerCount,
                        WaiterId = bill.WaiterId,
                        WaiterName = bill.WaiterName,
                        Items = request.Items.Select(x => new DineInOrderItemRequest
                        {
                            ItemId = x.ItemId,
                            ItemCode = x.ItemCode,
                            ItemName = x.ItemName,
                            UnitName = x.UnitName,
                            Quantity = x.Quantity,
                            UnitPrice = x.Price,
                            DiscountRate = x.DiscountRate,
                            VatRate = x.VatRate,
                            Note = x.Note
                        }).ToList()
                    };
                    return await SaveDineInOrder(saveReq);
                }
                else
                {
                    var openResp = await OpenDineInTable(new DineInOpenTableRequest
                    {
                        TableIds = new List<Guid> { request.TableId },
                        BranchCode = request.BranchCode,
                        CustomerCount = request.CustomerCount,
                        Force = true
                    });

                    if (openResp.Code != Code.Success)
                    {
                        return openResp;
                    }

                    if (openResp is ResponseObject<SaleBillDetailModel> openObj && openObj.Data != null)
                    {
                        var bill = openObj.Data;
                        var saveReq = new DineInOrderSaveRequest
                        {
                            OrderId = Guid.Parse(bill.BillId),
                            BranchCode = request.BranchCode,
                            CustomerCount = request.CustomerCount,
                            Items = request.Items.Select(x => new DineInOrderItemRequest
                            {
                                ItemId = x.ItemId,
                                ItemCode = x.ItemCode,
                                ItemName = x.ItemName,
                                UnitName = x.UnitName,
                                Quantity = x.Quantity,
                                UnitPrice = x.Price,
                                DiscountRate = x.DiscountRate,
                                VatRate = x.VatRate,
                                Note = x.Note
                            }).ToList()
                        };
                        return await SaveDineInOrder(saveReq);
                    }

                    return new Response(Code.BadRequest, "Không thể khởi tạo và cập nhật hóa đơn.");
                }
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.UpdateBill", ex);
                return new Response(Code.ServerError, "Lỗi: " + ex.Message);
            }
        }

        public async Task<Response> GetOrderHistory(OrderHistoryFilterModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.BranchCode))
                {
                    return new Response(Code.BadRequest, "Mã chi nhánh không được để trống!");
                }

                var orderRepo = _unit.GetRepository<SalesOrder>();
                var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                var orderItemRepo = _unit.GetRepository<SalesOrderItem>();
                var paymentRepo = _unit.GetRepository<SalesOrderPayment>();

                var query = orderRepo.Get(x => x.BranchCode == model.BranchCode);

                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    if (model.Status == OrderStatusConstants.Cancelled)
                    {
                        query = query.Where(x => x.Status == OrderStatusConstants.Cancelled || x.Status == OrderStatusConstants.ApprovedCancelled);
                    }
                    else
                    {
                        query = query.Where(x => x.Status == model.Status);
                    }
                }

                if (model.FromDate.HasValue)
                {
                    var fromDate = model.FromDate.Value.Date;
                    query = query.Where(x => x.OpenTime >= fromDate);
                }

                if (model.ToDate.HasValue)
                {
                    var toDate = model.ToDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.OpenTime < toDate);
                }

                if (!string.IsNullOrWhiteSpace(model.TextSearch))
                {
                    var search = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.OrderCode.ToLower().Contains(search) 
                                          || (x.WaiterName != null && x.WaiterName.ToLower().Contains(search)));
                }

                var totalElements = await query.CountAsync();

                model.Size ??= 20;
                var page = model.Page ?? 1;
                var excludedRows = Math.Max((page - 1) * model.Size.Value, 0);

                var orders = await query
                    .OrderByDescending(x => x.OpenTime)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var orderIds = orders.Select(x => x.Id).ToList();

                var orderTables = await orderTableRepo.Get(x => x.BranchCode == model.BranchCode && orderIds.Contains(x.OrderId)).ToListAsync();
                var orderItems = await orderItemRepo.Get(x => x.BranchCode == model.BranchCode && orderIds.Contains(x.OrderId)).ToListAsync();
                var orderPayments = await paymentRepo.Get(x => x.BranchCode == model.BranchCode && orderIds.Contains(x.OrderId)).ToListAsync();

                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var historyItemIds = orderItems.Select(x => x.ItemId).Distinct().ToList();
                var fixedTaxDict = await menuItemRepo.Get(x => historyItemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FixedTaxAmount);

                var details = new List<OrderHistoryDetailModel>();

                foreach (var order in orders)
                {
                    var tablesForOrder = orderTables.Where(x => x.OrderId == order.Id).OrderBy(x => x.TableName).Select(x => x.TableName).ToList();
                    var tableNames = string.Join(" + ", tablesForOrder);

                    var items = orderItems.Where(x => x.OrderId == order.Id)
                        .OrderBy(x => x.LineNo)
                        .Select(oi =>
                        {
                            bool isCancelled = oi.IsCancelled == 1 || (oi.Quantity > 0 && ((oi.SoldOutQty + oi.CancelledQuantity + oi.ReturnedQuantity) >= oi.Quantity));
                            double billableQty = Math.Max(0, oi.Quantity - oi.SoldOutQty - oi.CancelledQuantity - oi.ReturnedQuantity);
                            if (oi.IsCancelled == 1) billableQty = 0;

                            decimal fTax = fixedTaxDict.TryGetValue(oi.ItemId, out var tax) ? tax : 0;

                            decimal effAmount = isCancelled ? 0 : Math.Round((decimal)billableQty * oi.UnitPrice, 0, MidpointRounding.AwayFromZero);
                            decimal unitPrice = oi.UnitPrice;
                            decimal unitVat = fTax > 0
                                ? Math.Round(fTax, 0, MidpointRounding.AwayFromZero)
                                : Math.Round(unitPrice * (decimal)(oi.VatRate / 100), 0, MidpointRounding.AwayFromZero);
                            decimal unitGross = unitPrice + unitVat;

                            decimal rawDiscountAmount = isCancelled ? 0 : Math.Round(oi.DiscountAmount, 0, MidpointRounding.AwayFromZero);
                            decimal effDiscountAmount = 0;
                            if (!isCancelled && billableQty > 0)
                            {
                                if (oi.DiscountAmount > 0 && oi.Quantity > 0)
                                {
                                    decimal unitDisc = Math.Round(rawDiscountAmount / (decimal)oi.Quantity, 0, MidpointRounding.AwayFromZero);
                                    effDiscountAmount = unitDisc * (decimal)billableQty;
                                }
                                else if (oi.DiscountRate > 0)
                                {
                                    decimal unitDisc = Math.Round(unitGross * (decimal)(oi.DiscountRate / 100), 0, MidpointRounding.AwayFromZero);
                                    effDiscountAmount = unitDisc * (decimal)billableQty;
                                }
                            }

                            decimal effVatAmount = 0;
                            decimal effTotalAmount = 0;
                            if (!isCancelled && billableQty > 0)
                            {
                                decimal unitDiscount = Math.Round(effDiscountAmount / (decimal)billableQty, 0, MidpointRounding.AwayFromZero);
                                decimal unitTotal = Math.Max(0, unitGross - unitDiscount);

                                if (unitTotal <= 0 || oi.DiscountRate >= 100)
                                {
                                    effVatAmount = 0;
                                    effTotalAmount = 0;
                                }
                                else
                                {
                                    effVatAmount = Math.Round(unitVat * (decimal)billableQty, 0, MidpointRounding.AwayFromZero);
                                    effTotalAmount = Math.Round(unitTotal * (decimal)billableQty, 0, MidpointRounding.AwayFromZero);
                                }
                            }

                            return new SaleBillItemModel
                            {
                                Id = oi.Id,
                                ItemId = oi.ItemId,
                                ItemCode = oi.ItemCode,
                                ItemName = oi.ItemName,
                                UnitName = oi.UnitName,
                                Quantity = oi.Quantity,
                                Price = oi.UnitPrice,
                                SelectedOptionsJson = oi.SelectedOptionsJson,
                                Amount = effAmount,
                                ServedQuantity = oi.ServedQty,
                                RemainingQuantity = oi.RemainingQty,
                                Note = oi.Note,
                                PromotionCode = oi.PromotionCode,
                                DiscountRate = isCancelled ? 0 : oi.DiscountRate,
                                DiscountAmount = effDiscountAmount,
                                VatRate = isCancelled ? 0 : oi.VatRate,
                                VatAmount = effVatAmount,
                                TotalAmount = effTotalAmount,
                                IsSentToKitchen = oi.IsSentToKitchen,
                                KitchenStatus = oi.KitchenStatus,
                                SentQty = oi.SentQty,
                                ServedQty = oi.ServedQty,
                                SoldOutQty = oi.SoldOutQty,
                                RemainingQty = oi.RemainingQty,
                                CancelledQuantity = oi.CancelledQuantity,
                                ActualQuantity = oi.ActualQuantity,
                                ReturnedQuantity = oi.ReturnedQuantity,
                                IsChecked = oi.IsChecked,
                                IsCancelled = oi.IsCancelled
                            };
                        }).ToList();

                    decimal subTotal = items.Any() ? items.Sum(x => x.Amount) : order.SubTotal;
                    decimal discountAmount = items.Any() ? items.Sum(x => x.DiscountAmount) : order.DiscountAmount;
                    decimal vatAmount = items.Any() ? items.Sum(x => x.VatAmount) : order.VatAmount;
                    decimal totalAmount = items.Any() ? items.Sum(x => x.TotalAmount) : order.TotalAmount;
                    if (totalAmount <= 0)
                    {
                        totalAmount = 0;
                        vatAmount = 0;
                    }

                    var payments = orderPayments.Where(x => x.OrderId == order.Id)
                        .Select(p => new DineInPaymentDetailModel
                        {
                            Id = p.Id,
                            PaymentType = p.PaymentType,
                            PaymentAmount = p.PaymentAmount,
                            TransactionId = p.TransactionId,
                            CreatedOnDate = p.CreatedOnDate
                        }).ToList();

                    details.Add(new OrderHistoryDetailModel
                    {
                        OrderId = order.Id,
                        OrderCode = order.OrderCode,
                        OrderDate = order.OrderDate,
                        OpenTime = order.OpenTime,
                        PaymentTime = order.PaymentTime,
                        Status = order.Status,
                        WaiterId = order.WaiterId,
                        WaiterName = order.WaiterName,
                        CustomerCount = order.CustomerCount,
                        TableNames = tableNames,
                        SubTotal = subTotal,
                        DiscountAmount = discountAmount,
                        VatAmount = vatAmount,
                        TotalAmount = totalAmount,
                        CancelReason = order.CancelReason,
                        ServiceType = order.ServiceType,
                        Note = order.Note,
                        SelectedPromotionCode = order.SelectedPromotionCode,
                        Items = items,
                        Payments = payments
                    });
                }

                var pagination = new Pagination<OrderHistoryDetailModel>
                {
                    Content = details,
                    NumberOfElements = details.Count,
                    Page = page,
                    Size = model.Size.Value,
                    TotalElements = totalElements,
                    TotalPages = (int)Math.Ceiling((double)totalElements / model.Size.Value)
                };

                return new ResponsePagination<OrderHistoryDetailModel>(pagination);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SalesHandler.GetOrderHistory", ex);
                return new Response(Code.ServerError, "Đã xảy ra lỗi: " + ex.Message);
            }
        }
        private async Task<string> GenerateOrderCode(string branchCode, DateTime? orderDate = null)
        {
            var targetDate = (orderDate ?? DateTime.Today).Date;
            var prefix = $"{branchCode}{targetDate:yyyyMMdd}-";
            var orderRepo = _unit.GetRepository<SalesOrder>();
            
            var allCodes = await orderRepo.Get(x => x.BranchCode == branchCode 
                                                && x.OrderCode.StartsWith(prefix))
                                          .Select(x => x.OrderCode)
                                          .ToListAsync();

            int maxSeq = 0;
            foreach (var code in allCodes)
            {
                if (!string.IsNullOrEmpty(code) && code.Length >= prefix.Length)
                {
                    var seqPart = code.Substring(prefix.Length);
                    if (int.TryParse(seqPart, out int parsedNum))
                    {
                        if (parsedNum > maxSeq)
                        {
                            maxSeq = parsedNum;
                        }
                    }
                }
            }

            return $"{prefix}{(maxSeq + 1):D4}";
        }

        private async Task<int> GetNextKitchenRequestSequence(string branchCode, string orderCode)
        {
            var requestRepo = _unit.GetRepository<KitchenRequest>();
            var prefix = $"{orderCode}-";

            var allCodes = await requestRepo.Get(x => x.BranchCode == branchCode && x.RequestCode.StartsWith(prefix))
                                            .Select(x => x.RequestCode)
                                            .ToListAsync();

            int maxSeq = 0;
            foreach (var code in allCodes)
            {
                if (!string.IsNullOrEmpty(code) && code.Length >= prefix.Length)
                {
                    var seqPart = code.Substring(prefix.Length);
                    if (int.TryParse(seqPart, out int parsedNum))
                    {
                        if (parsedNum > maxSeq)
                        {
                            maxSeq = parsedNum;
                        }
                    }
                }
            }

            return maxSeq + 1;
        }

        public async Task<Response> ValidateTherapyVoucher(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return new Response(Code.BadRequest, "Mã phiếu không hợp lệ!");

                var documentNumber = code.Trim();
                var baseUrl = _config?["TherapyVoucher:ValidateUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return new Response(Code.BadRequest, "Chưa cấu hình đường dẫn API kiểm tra phiếu!");

                var apiUrl = baseUrl.Contains("?")
                    ? $"{baseUrl}&documentNumber={Uri.EscapeDataString(documentNumber)}"
                    : $"{baseUrl}?documentNumber={Uri.EscapeDataString(documentNumber)}";

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                    return new Response(Code.BadRequest, "Mã phiếu không có trên AnPOS");

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                    return new Response(Code.BadRequest, "Mã phiếu không có trên AnPOS");

                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Kiểm tra Code nếu API AnPOS trả về lỗi
                if (root.TryGetProperty("Code", out var codeProp) || root.TryGetProperty("code", out codeProp))
                {
                    if (codeProp.GetInt32() != 200)
                        return new Response(Code.BadRequest, "Mã phiếu không có trên AnPOS");
                }

                if ((!root.TryGetProperty("Data", out var dataProp) && !root.TryGetProperty("data", out dataProp)) || dataProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    return new Response(Code.BadRequest, "Mã phiếu không có trên AnPOS");
                }

                // Trích xuất danh sách dịch vụ
                var services = ExtractTherapyServices(dataProp, documentNumber);

                return new ResponseObject<object>
                {
                    Code = Code.Success,
                    Message = "Mã phiếu hợp lệ trên AnPOS!",
                    Data = new
                    {
                        Code = documentNumber,
                        Services = services
                    }
                };
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("ValidateTherapyVoucher Error", ex, code);
                return new Response(Code.BadRequest, "Mã phiếu không có trên AnPOS");
            }
        }

        private static List<object> ExtractTherapyServices(System.Text.Json.JsonElement dataProp, string defaultDocNumber)
        {
            var services = new List<object>();

            void AddItem(System.Text.Json.JsonElement elem, string fallbackDoc)
            {
                var sCode = GetJsonPropString(elem, "ServiceCode", "serviceCode", "Code", "code");
                var sName = GetJsonPropString(elem, "ServiceName", "serviceName", "Name", "name");
                var docNum = GetJsonPropString(elem, "DocumentNumber", "documentNumber") ?? fallbackDoc;

                if (!string.IsNullOrWhiteSpace(sCode) || !string.IsNullOrWhiteSpace(sName))
                {
                    var label = string.Join(" - ", new[] { docNum, sCode, sName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    services.Add(new
                    {
                        DocumentNumber = docNum,
                        ServiceCode = sCode ?? string.Empty,
                        ServiceName = sName ?? string.Empty,
                        DisplayLabel = label
                    });
                }
            }

            if (dataProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in dataProp.EnumerateArray()) AddItem(item, defaultDocNumber);
            }
            else if (dataProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var docNum = GetJsonPropString(dataProp, "DocumentNumber", "documentNumber") ?? defaultDocNumber;

                // 1. Trường hợp object đơn
                AddItem(dataProp, docNum);

                // 2. Trường hợp object có mảng con (Details / Services)
                foreach (var prop in new[] { "Details", "details", "Services", "services" })
                {
                    if (dataProp.TryGetProperty(prop, out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in arr.EnumerateArray()) AddItem(item, docNum);
                    }
                }
            }

            return services;
        }

        private static string? GetJsonPropString(System.Text.Json.JsonElement element, params string[] propertyNames)
        {
            foreach (var p in propertyNames)
            {
                if (element.TryGetProperty(p, out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.String) return val.GetString();
                    if (val.ValueKind == System.Text.Json.JsonValueKind.Number) return val.ToString();
                }
            }
            return null;
        }
    }
}
