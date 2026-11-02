using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnFood.Application.Bussiness.ShiftBussiness
{
    public class ShiftHandler : IShiftHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public ShiftHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        private static bool IsTimeInShift(TimeSpan start, TimeSpan end, TimeSpan current)
        {
            if (start <= end)
            {
                return current >= start && current <= end;
            }
            else
            {
                return current >= start || current <= end;
            }
        }

        public async Task<Response> OpenShift(ShiftOpenModel model, string userCode, string userName)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    var shiftRepo = _unit.GetRepository<Shift>();
                    // 1. Check if employee already has an active shift in any branch
                    var cashierActiveShift = await shiftRepo.Get(x => x.CashierId == userCode && x.Status == 1).FirstOrDefaultAsync();
                    if (cashierActiveShift != null)
                    {
                        if (cashierActiveShift.BranchCode != model.BranchCode)
                        {
                            var branchRepo = _unit.GetRepository<Branchs>();
                            var activeBranch = await branchRepo.Get(b => b.Code == cashierActiveShift.BranchCode).FirstOrDefaultAsync();
                            var activeBranchName = activeBranch?.Name ?? cashierActiveShift.BranchCode;
                            return new Response(Code.BadRequest, $"Tài khoản của bạn đang có ca mở tại chi nhánh '{activeBranchName}'. Vui lòng chuyển sang chi nhánh '{activeBranchName}' để đóng ca trước.");
                        }
                        return new Response(Code.Conflict, "Tài khoản của bạn đang có ca làm việc, bạn có muốn đóng ca hiện tại?");
                    }

                    // 2. Check if machine already has an active shift in current branch
                    if (!string.IsNullOrWhiteSpace(model.MachineCode))
                    {
                        var machineActiveShift = await shiftRepo.Get(x => x.MachineCode == model.MachineCode && x.BranchCode == model.BranchCode && x.Status == 1).FirstOrDefaultAsync();
                        if (machineActiveShift != null)
                        {
                            return new Response(Code.Conflict, $"Máy POS '{model.MachineCode}' tại chi nhánh này đang có ca mở của thu ngân '{machineActiveShift.CashierName}'. Bạn có muốn đóng ca hiện tại để mở ca mới?");
                        }
                    }

                    // 3. Validate WorkShift config
                    var workShiftRepo = _unit.GetRepository<WorkShift>();
                    var workShift = await workShiftRepo.Get(x => x.Id == model.ShiftId && x.Status != 0).FirstOrDefaultAsync();
                    if (workShift == null)
                    {
                        return new Response(Code.NotFound, "Không tìm thấy ca cấu hình!");
                    }

                    // 4. Determine actual open time
                    var localTime = DateTime.Now;
                    DateTime actualOpenTime = localTime;

                    // 5. Create Shift session
                    var newShift = new Shift
                    {
                        Id = Guid.NewGuid(),
                        BranchCode = model.BranchCode,
                        MachineCode = model.MachineCode,
                        CashierId = userCode,
                        CashierName = userName,
                        ShiftId = workShift.Id,
                        ShiftName = workShift.Name,
                        StartTime = workShift.StartTime,
                        EndTime = workShift.EndTime,
                        ExtendedMinutes = 0,
                        OpenTime = actualOpenTime,
                        OpeningCash = model.OpeningCash,
                        ExpectedCash = model.OpeningCash, // initially expected = opening cash
                        ActualCash = 0,
                        DifferenceCash = 0,
                        TransferCash = 0,
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = userCode,
                        CreatedByUserName = userName
                    };

                    shiftRepo.Add(newShift);
                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("Shift", newShift.Id.ToString(), newShift, userCode, userName);
                    return new ResponseObject<Guid>(newShift.Id, "Mở ca thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Open Shift Error", ex, model);
                    return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        public async Task<Response> GetCurrentShift(string branchCode, string? machineCode, string userCode)
        {
            try
            {
                var shiftRepo = _unit.GetRepository<Shift>();
                var shift = await shiftRepo.Get(x => x.CashierId == userCode && x.BranchCode == branchCode && x.Status == 1)
                    .FirstOrDefaultAsync();

                if (shift == null)
                {
                    // If cashier has no shift, but let's check by machine code as well
                    if (!string.IsNullOrWhiteSpace(machineCode))
                    {
                        shift = await shiftRepo.Get(x => x.MachineCode == machineCode && x.BranchCode == branchCode && x.Status == 1)
                            .FirstOrDefaultAsync();
                    }
                }

                if (shift == null)
                {
                    // Check last closed shift actual cash to default opening cash
                    var lastClosedShift = await shiftRepo.Get(x => x.BranchCode == branchCode && x.Status == 2)
                        .OrderByDescending(x => x.CloseTime)
                        .FirstOrDefaultAsync();

                    var defaultOpeningCash = lastClosedShift?.ActualCash ?? 0;

                    return new ResponseObject<object>(new { 
                        DefaultOpeningCash = defaultOpeningCash,
                        LastShiftCloseTime = lastClosedShift?.CloseTime,
                        LastShiftName = lastClosedShift?.ShiftName
                    }, "Không có ca nào đang hoạt động", Code.NotFound);
                }

                // Calculate expiry
                var localNow = DateTime.Now;
                DateTime expiryTime;
                if (shift.EndTime.HasValue)
                {
                    var baseEndTime = shift.OpenTime.Date.Add(shift.EndTime.Value);
                    if (shift.StartTime.HasValue && shift.EndTime.Value < shift.StartTime.Value)
                    {
                        // Overnight shift
                        if (shift.OpenTime.TimeOfDay >= shift.StartTime.Value)
                        {
                            baseEndTime = baseEndTime.AddDays(1);
                        }
                    }
                    expiryTime = baseEndTime.AddMinutes(shift.ExtendedMinutes);
                }
                else
                {
                    expiryTime = shift.OpenTime.AddHours(8).AddMinutes(shift.ExtendedMinutes);
                }

                var isExpired = localNow > expiryTime;

                var detail = new ShiftDetailModel
                {
                    Id = shift.Id,
                    BranchCode = shift.BranchCode,
                    MachineCode = shift.MachineCode,
                    CashierId = shift.CashierId,
                    CashierName = shift.CashierName,
                    ShiftId = shift.ShiftId,
                    ShiftName = shift.ShiftName,
                    StartTime = shift.StartTime?.ToString(@"hh\:mm"),
                    EndTime = shift.EndTime?.ToString(@"hh\:mm"),
                    ExtendedMinutes = shift.ExtendedMinutes,
                    OpenTime = shift.OpenTime,
                    CloseTime = shift.CloseTime,
                    OpeningCash = shift.OpeningCash,
                    ExpectedCash = shift.ExpectedCash,
                    ActualCash = shift.ActualCash,
                    DifferenceCash = shift.DifferenceCash,
                    TransferCash = shift.TransferCash,
                    ReceiverEmployeeId = shift.ReceiverEmployeeId,
                    Status = shift.Status,
                    IsExpired = isExpired,
                    ExpiryTime = expiryTime
                };

                // Get cash counts
                var ccRepo = _unit.GetRepository<ShiftCashCount>();
                var cashCounts = await ccRepo.Get(x => x.ShiftId == shift.Id).ToListAsync();
                detail.CashCounts = cashCounts.Select(c => new ShiftCashCountItemModel
                {
                    Denomination = c.Denomination,
                    Quantity = c.Quantity
                }).ToList();

                return new ResponseObject<ShiftDetailModel>(detail, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetCurrentShift Error", ex, new { branchCode, machineCode, userCode });
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> ExtendShift(ShiftExtendModel model, string userCode, string userName)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    var shiftRepo = _unit.GetRepository<Shift>();
                    var shift = await shiftRepo.Get(x => x.Id == model.ShiftId && x.Status == 1).FirstOrDefaultAsync();
                    if (shift == null)
                    {
                        return new Response(Code.NotFound, "Không tìm thấy ca đang hoạt động để gia hạn!");
                    }

                    shift.ExtendedMinutes += model.ExtendedMinutes;
                    shift.UpdatedOnDate = DateTime.UtcNow;
                    shift.UpdatedByUserCode = userCode;
                    shift.UpdatedByUserName = userName;

                    shiftRepo.Update(shift);

                    var historyRepo = _unit.GetRepository<ShiftExtensionHistory>();
                    var history = new ShiftExtensionHistory
                    {
                        Id = Guid.NewGuid(),
                        ShiftId = shift.Id,
                        ExtendedMinutes = model.ExtendedMinutes,
                        Reason = model.Reason,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = userCode,
                        CreatedByUserName = userName
                    };
                    historyRepo.Add(history);

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("Shift", shift.Id.ToString(), shift, userCode, userName);
                    return new Response(Code.Success, $"Gia hạn ca làm việc thêm {model.ExtendedMinutes} phút thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("ExtendShift Error", ex, model);
                    return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        public async Task<Response> GetShiftSummary(Guid id)
        {
            try
            {
                var shiftRepo = _unit.GetRepository<Shift>();
                var shift = await shiftRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (shift == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy ca làm việc!");
                }

                var endTimeLimit = shift.CloseTime ?? DateTime.Now;

                // Query all Paid Orders in this branch during this shift session
                var orderRepo = _unit.GetRepository<SalesOrder>();
                var allBranchPaidOrders = await orderRepo.Get(x => 
                        x.BranchCode == shift.BranchCode && 
                        x.Status == "Paid")
                    .ToListAsync();

                // Filter paid orders within shift time window AND cashier
                var paidOrders = allBranchPaidOrders.Where(x => {
                    var time = x.PaymentTime ?? x.CreatedOnDate ?? x.OrderDate;
                    bool inTimeWindow = time >= shift.OpenTime && time <= endTimeLimit.AddMinutes(5);
                    bool matchCashier = string.IsNullOrEmpty(shift.CashierId) || 
                                        x.CashierUserId == shift.CashierId || 
                                        x.CreatedByUserCode == shift.CashierId ||
                                        (!string.IsNullOrEmpty(x.CashierUserName) && x.CashierUserName.Equals(shift.CashierName, StringComparison.OrdinalIgnoreCase));
                    return inTimeWindow && matchCashier;
                }).ToList();

                var orderIds = paidOrders.Select(o => o.Id).ToList();

                // Get payments
                decimal cashRevenue = 0;
                decimal transferRevenue = 0;
                decimal cardRevenue = 0;
                decimal promoRevenue = 0;

                if (orderIds.Count > 0)
                {
                    var paymentRepo = _unit.GetRepository<SalesOrderPayment>();
                    var payments = await paymentRepo.Get(x => orderIds.Contains(x.OrderId) && x.BranchCode == shift.BranchCode)
                        .ToListAsync();

                    foreach (var p in payments)
                    {
                        var pt = p.PaymentType?.ToLower() ?? "";
                        if (pt.Contains("cash") || pt.Contains("tiền mặt") || pt.Contains("tienmat"))
                        {
                            cashRevenue += p.PaymentAmount;
                        }
                        else if (pt.Contains("transfer") || pt.Contains("chuyển khoản") || pt.Contains("chuyenkhoan") || pt.Contains("bank") || pt.Contains("qr"))
                        {
                            transferRevenue += p.PaymentAmount;
                        }
                        else if (pt.Contains("card") || pt.Contains("thẻ") || pt.Contains("cathe") || pt.Contains("credit"))
                        {
                            cardRevenue += p.PaymentAmount;
                        }
                        else if (pt.Contains("promo") || pt.Contains("voucher") || pt.Contains("discount") || pt.Contains("khuyến mãi") || pt.Contains("khuyenmai"))
                        {
                            promoRevenue += Math.Abs(p.PaymentAmount);
                        }
                        else
                        {
                            // Mặc định cho là tiền mặt nếu không khớp hình thức khác
                            cashRevenue += p.PaymentAmount;
                        }
                    }

                    // Fallback for paid orders with no explicit payment records -> Treat as Cash
                    var paidOrderIdsWithPayments = payments.Select(p => p.OrderId).ToHashSet();
                    var unrecordedOrders = paidOrders.Where(o => !paidOrderIdsWithPayments.Contains(o.Id)).ToList();
                    if (unrecordedOrders.Any())
                    {
                        cashRevenue += unrecordedOrders.Sum(o => o.TotalAmount);
                    }
                }
                else
                {
                    // Nếu chưa có bảng SalesOrderPayment riêng, tính tổng tiền các đơn Paid trong ca
                    cashRevenue = paidOrders.Sum(o => o.TotalAmount);
                }

                // Sum Promo revenue also from direct DiscountAmount on SalesOrder if larger
                var totalDirectDiscount = paidOrders.Sum(o => o.DiscountAmount);
                if (promoRevenue < totalDirectDiscount)
                {
                    promoRevenue = totalDirectDiscount;
                }

                decimal totalRevenue = cashRevenue + transferRevenue + cardRevenue;

                // Tiền mặt phải có = Tiền mặt đầu ca + Tiền mặt thu thêm
                decimal expectedCash = shift.OpeningCash + cashRevenue;

                // Unpaid/Open Orders created during this shift session
                var unpaidCount = await orderRepo.Get(x => 
                        x.BranchCode == shift.BranchCode && 
                        (x.CreatedOnDate >= shift.OpenTime || x.OrderDate >= shift.OpenTime.Date) &&
                        (x.Status == "WaitingService" || x.Status == "Serving" || x.Status == "Reserved" || x.Status == "WaitingPayment"))
                    .CountAsync();

                // Get Receiver name if set
                string? receiverName = null;
                if (shift.ReceiverEmployeeId.HasValue)
                {
                    var empRepo = _unit.GetRepository<Employee>();
                    var receiver = await empRepo.Get(x => x.Id == shift.ReceiverEmployeeId.Value).FirstOrDefaultAsync();
                    receiverName = receiver?.Name;
                }

                var summary = new ShiftSummaryModel
                {
                    Id = shift.Id,
                    BranchCode = shift.BranchCode,
                    CashierId = shift.CashierId,
                    CashierName = shift.CashierName,
                    ShiftId = shift.ShiftId,
                    ShiftName = shift.ShiftName,
                    OpenTime = shift.OpenTime,
                    CloseTime = shift.CloseTime,
                    OpeningCash = shift.OpeningCash,
                    CashRevenue = cashRevenue,
                    TransferRevenue = transferRevenue,
                    CardRevenue = cardRevenue,
                    PromoRevenue = promoRevenue,
                    TotalRevenue = totalRevenue,
                    ExpectedCash = expectedCash,
                    ActualCash = shift.ActualCash,
                    DifferenceCash = expectedCash - shift.ActualCash,
                    TransferCash = shift.TransferCash > 0 ? shift.TransferCash : (shift.ActualCash > 0 ? shift.ActualCash : expectedCash),
                    ReceiverEmployeeName = receiverName,
                    PaidInvoicesCount = paidOrders.Count,
                    UnpaidOrdersCount = unpaidCount,
                    Status = shift.Status
                };

                return new ResponseObject<ShiftSummaryModel>(summary, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetShiftSummary Error", ex, id);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> SubmitCashCount(Guid id, List<ShiftCashCountItemModel> items, string userCode, string userName)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    var shiftRepo = _unit.GetRepository<Shift>();
                    var shift = await shiftRepo.Get(x => x.Id == id && x.Status == 1).FirstOrDefaultAsync();
                    if (shift == null)
                    {
                        return new Response(Code.NotFound, "Không tìm thấy ca làm việc đang hoạt động!");
                    }

                    // Delete existing cash counts
                    var ccRepo = _unit.GetRepository<ShiftCashCount>();
                    var existingCounts = await ccRepo.Get(x => x.ShiftId == id).ToListAsync();
                    foreach (var ec in existingCounts)
                    {
                        ccRepo.Delete(ec);
                    }

                    // Add new counts and compute sum
                    decimal totalActualCash = 0;
                    foreach (var item in items)
                    {
                        decimal amount = item.Denomination * item.Quantity;
                        totalActualCash += amount;

                        ccRepo.Add(new ShiftCashCount
                        {
                            Id = Guid.NewGuid(),
                            ShiftId = id,
                            Denomination = item.Denomination,
                            Quantity = item.Quantity,
                            Amount = amount,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedByUserCode = userCode,
                            CreatedByUserName = userName
                        });
                    }

                    // Compute summary for expected cash
                    var summaryRes = await GetShiftSummary(id);
                    decimal expectedCash = shift.OpeningCash;
                    if (summaryRes is ResponseObject<ShiftSummaryModel> summaryObj && summaryObj.Data != null)
                    {
                        expectedCash = summaryObj.Data.ExpectedCash;
                    }

                    // Update shift properties
                    shift.ActualCash = totalActualCash;
                    shift.DifferenceCash = expectedCash - totalActualCash;
                    shift.ExpectedCash = expectedCash;
                    shift.UpdatedOnDate = DateTime.UtcNow;
                    shift.UpdatedByUserCode = userCode;
                    shift.UpdatedByUserName = userName;

                    shiftRepo.Update(shift);

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("Shift", shift.Id.ToString(), shift, userCode, userName);
                    return new Response(Code.Success, "Kiểm kê tiền mặt ca thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("SubmitCashCount Error", ex, new { id, items });
                    return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        public async Task<Response> CloseShift(ShiftCloseModel model, string userCode, string userName)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    var shiftRepo = _unit.GetRepository<Shift>();
                    var shift = await shiftRepo.Get(x => x.Id == model.ShiftId && x.Status == 1).FirstOrDefaultAsync();
                    if (shift == null)
                    {
                        return new Response(Code.NotFound, "Không tìm thấy ca làm việc hoặc ca đã đóng!");
                    }

                    // Check if counted (ActualCash must have been submitted)
                    var ccRepo = _unit.GetRepository<ShiftCashCount>();
                    var hasCounts = await ccRepo.Get(x => x.ShiftId == model.ShiftId).AnyAsync();
                    if (!hasCounts)
                    {
                        return new Response(Code.BadRequest, "Vui lòng thực hiện kiểm kê tiền mặt trước khi đóng ca!");
                    }

                    // Re-calculate expected cash
                    var summaryRes = await GetShiftSummary(model.ShiftId);
                    decimal expectedCash = shift.OpeningCash;
                    if (summaryRes is ResponseObject<ShiftSummaryModel> summaryObj && summaryObj.Data != null)
                    {
                        expectedCash = summaryObj.Data.ExpectedCash;
                    }

                    shift.CloseTime = DateTime.Now;
                    shift.ReceiverEmployeeId = model.ReceiverEmployeeId;
                    shift.TransferCash = model.TransferCash;
                    shift.ExpectedCash = expectedCash;
                    shift.DifferenceCash = expectedCash - shift.ActualCash;
                    shift.Status = 2; // Closed
                    shift.UpdatedOnDate = DateTime.UtcNow;
                    shift.UpdatedByUserCode = userCode;
                    shift.UpdatedByUserName = userName;

                    shiftRepo.Update(shift);
                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("Shift", shift.Id.ToString(), shift, userCode, userName);
                    return new Response(Code.Success, "Đóng ca làm việc thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("CloseShift Error", ex, model);
                    return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        public async Task<Response> GetShiftHistory(ShiftFilterModel model)
        {
            try
            {
                var shiftRepo = _unit.GetRepository<Shift>();
                var query = shiftRepo.Get(x => x.BranchCode == model.BranchCode);

                if (!string.IsNullOrWhiteSpace(model.CashierId))
                {
                    query = query.Where(x => x.CashierId == model.CashierId);
                }

                if (model.FromDate.HasValue)
                {
                    query = query.Where(x => x.OpenTime >= model.FromDate.Value);
                }

                if (model.ToDate.HasValue)
                {
                    query = query.Where(x => x.OpenTime <= model.ToDate.Value);
                }

                query = query.OrderByDescending(x => x.OpenTime);

                var totalCount = await query.CountAsync();
                var items = await query.Skip((model.Page - 1) * model.Size).Take(model.Size).ToListAsync();

                var list = new List<ShiftSummaryModel>();
                foreach (var shift in items)
                {
                    // Fetch summary for each to get revenue aggregates
                    var summaryRes = await GetShiftSummary(shift.Id);
                    if (summaryRes is ResponseObject<ShiftSummaryModel> summaryObj && summaryObj.Data != null)
                    {
                        list.Add(summaryObj.Data);
                    }
                }

                return new ResponseObject<List<ShiftSummaryModel>>(list, "Success", Code.Success, 200, list.Count, totalCount);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetShiftHistory Error", ex, model);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetShiftById(Guid id)
        {
            try
            {
                var shiftRepo = _unit.GetRepository<Shift>();
                var shift = await shiftRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (shift == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy ca làm việc!");
                }

                var detail = new ShiftDetailModel
                {
                    Id = shift.Id,
                    BranchCode = shift.BranchCode,
                    MachineCode = shift.MachineCode,
                    CashierId = shift.CashierId,
                    CashierName = shift.CashierName,
                    ShiftId = shift.ShiftId,
                    ShiftName = shift.ShiftName,
                    StartTime = shift.StartTime?.ToString(@"hh\:mm"),
                    EndTime = shift.EndTime?.ToString(@"hh\:mm"),
                    ExtendedMinutes = shift.ExtendedMinutes,
                    OpenTime = shift.OpenTime,
                    CloseTime = shift.CloseTime,
                    OpeningCash = shift.OpeningCash,
                    ExpectedCash = shift.ExpectedCash,
                    ActualCash = shift.ActualCash,
                    DifferenceCash = shift.DifferenceCash,
                    TransferCash = shift.TransferCash,
                    ReceiverEmployeeId = shift.ReceiverEmployeeId,
                    Status = shift.Status,
                    IsExpired = false,
                    ExpiryTime = null
                };

                // Get cash counts
                var ccRepo = _unit.GetRepository<ShiftCashCount>();
                var cashCounts = await ccRepo.Get(x => x.ShiftId == shift.Id).ToListAsync();
                detail.CashCounts = cashCounts.Select(c => new ShiftCashCountItemModel
                {
                    Denomination = c.Denomination,
                    Quantity = c.Quantity
                }).ToList();

                return new ResponseObject<ShiftDetailModel>(detail, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetShiftById Error", ex, id);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}
