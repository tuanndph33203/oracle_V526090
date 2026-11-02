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

namespace AnFood.Application.Bussiness.DiningTableBussiness
{
    public class DiningTableHandler : IDiningTableHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public DiningTableHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(DiningTableCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                // Verify FloorPlan belongs to the selected Branch
                var fpRepo = _unit.GetRepository<FloorPlan>();
                var fp = await fpRepo.Get(x => x.Id == model.FloorPlanId && x.Status != 0).FirstOrDefaultAsync();
                if (fp == null)
                {
                    return new Response(Code.NotFound, "SÆ¡ Ä‘á»“ phÃ²ng khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ xÃ³a!");
                }
                if (fp.BranchId != model.BranchId)
                {
                    return new Response(Code.BadRequest, "SÆ¡ Ä‘á»“ phÃ²ng khÃ´ng thuá»™c chi nhÃ¡nh Ä‘Ã£ chá»n!");
                }

                var repo = _unit.GetRepository<DiningTable>();
                var codeLower = model.Code.Trim().ToLower();
                
                // Find all tables with the same code in this branch
                var existingTables = await repo.Get(x => x.BranchId == model.BranchId && x.Code.ToLower() == codeLower).ToListAsync();
                var activeExists = existingTables.Any(x => x.Status != 0);
                if (activeExists)
                {
                    return new Response(Code.Conflict, "MÃ£ bÃ n Ä‘Ã£ tá»“n táº¡i á»Ÿ chi nhÃ¡nh nÃ y!");
                }

                // Clean up soft-deleted conflicts by renaming them
                var softDeleted = existingTables.Where(x => x.Status == 0).ToList();
                foreach (var sd in softDeleted)
                {
                    var suffix = $"_del_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    sd.Code = sd.Code.Length + suffix.Length > 50 
                        ? sd.Code.Substring(0, 50 - suffix.Length) + suffix 
                        : sd.Code + suffix;
                    sd.UpdatedOnDate = DateTime.UtcNow;
                    sd.UpdatedByUserCode = model.CreatedByUserCode ?? "system";
                    sd.UpdatedByUserName = model.CreatedByUserName ?? "system";
                    repo.Update(sd);
                }

                var item = new DiningTable
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    FloorPlanId = model.FloorPlanId,
                    BranchId = model.BranchId,
                    Capacity = model.Capacity,
                    X = model.X,
                    Y = model.Y,
                    Width = model.Width > 0 ? model.Width : 100,
                    Height = model.Height > 0 ? model.Height : 100,
                    Rotate = model.Rotate,
                    SortOrder = model.SortOrder,
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("DiningTable", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o bÃ n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create DiningTable Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(DiningTableUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                // Verify FloorPlan belongs to the selected Branch
                var fpRepo = _unit.GetRepository<FloorPlan>();
                var fp = await fpRepo.Get(x => x.Id == model.FloorPlanId && x.Status != 0).FirstOrDefaultAsync();
                if (fp == null)
                {
                    return new Response(Code.NotFound, "SÆ¡ Ä‘á»“ phÃ²ng khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ xÃ³a!");
                }
                if (fp.BranchId != model.BranchId)
                {
                    return new Response(Code.BadRequest, "SÆ¡ Ä‘á»“ phÃ²ng khÃ´ng thuá»™c chi nhÃ¡nh Ä‘Ã£ chá»n!");
                }

                var repo = _unit.GetRepository<DiningTable>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y bÃ n!");
                }

                                var requestedCode = model.Code.Trim();
                if (!string.Equals(item.Code, requestedCode, StringComparison.OrdinalIgnoreCase))
                {
                    return new Response(Code.BadRequest, "Ma ban khong duoc phep chinh sua sau khi tao!");
                }

                var old = new DiningTable
                {
                    Code = item.Code,
                    Name = item.Name,
                    FloorPlanId = item.FloorPlanId,
                    BranchId = item.BranchId,
                    Capacity = item.Capacity,
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    Rotate = item.Rotate,
                    SortOrder = item.SortOrder,
                    Status = item.Status
                };
                item.Name = model.Name.Trim();
                item.FloorPlanId = model.FloorPlanId;
                item.BranchId = model.BranchId;
                item.Capacity = model.Capacity;
                item.X = model.X;
                item.Y = model.Y;
                item.Width = model.Width;
                item.Height = model.Height;
                item.Rotate = model.Rotate;
                item.SortOrder = model.SortOrder;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("DiningTable", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t bÃ n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update DiningTable Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<DiningTable>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y bÃ n!");
                }

                item.Status = 0;
                var suffix = $"_del_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                item.Code = item.Code.Length + suffix.Length > 50 
                    ? item.Code.Substring(0, 50 - suffix.Length) + suffix 
                    : item.Code + suffix;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("DiningTable", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a bÃ n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete DiningTable Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<DiningTable>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y bÃ n!");
                }

                var fpRepo = _unit.GetRepository<FloorPlan>();
                var bRepo = _unit.GetRepository<Branchs>();

                var fp = await fpRepo.Get(x => x.Id == item.FloorPlanId).FirstOrDefaultAsync();
                var branch = await bRepo.Get(x => x.Id == item.BranchId).FirstOrDefaultAsync();

                var model = new DiningTableDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    FloorPlanId = item.FloorPlanId,
                    FloorPlanName = fp?.Name,
                    BranchId = item.BranchId,
                    BranchName = branch?.Name,
                    Status = item.Status,
                    Capacity = item.Capacity,
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    Rotate = item.Rotate,
                    SortOrder = item.SortOrder
                };

                return new ResponseObject<DiningTableDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(DiningTableFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<DiningTable>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.BranchId.HasValue) query = query.Where(x => x.BranchId == model.BranchId.Value);
                if (model.FloorPlanId.HasValue) query = query.Where(x => x.FloorPlanId == model.FloorPlanId.Value);

                if (model.IsInactive.HasValue)
                {
                    int targetStatus = model.IsInactive.Value ? 2 : 1;
                    query = query.Where(x => x.Status == targetStatus);
                }

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var items = await query
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var fpRepo = _unit.GetRepository<FloorPlan>();
                var bRepo = _unit.GetRepository<Branchs>();

                var datas = new List<DiningTableDetailModel>();
                foreach (var x in items)
                {
                    var fp = await fpRepo.Get(f => f.Id == x.FloorPlanId).FirstOrDefaultAsync();
                    var branch = await bRepo.Get(b => b.Id == x.BranchId).FirstOrDefaultAsync();
                    datas.Add(new DiningTableDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        FloorPlanId = x.FloorPlanId,
                        FloorPlanName = fp?.Name,
                        BranchId = x.BranchId,
                        BranchName = branch?.Name,
                        Status = x.Status,
                        Capacity = x.Capacity,
                        X = x.X,
                        Y = x.Y,
                        Width = x.Width,
                        Height = x.Height,
                        Rotate = x.Rotate,
                        SortOrder = x.SortOrder
                    });
                }

                var response = new Pagination<DiningTableDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<DiningTableDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> SaveFloorLayout(FloorLayoutUpdateModel model, string? userCode, string? userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.BranchCode))
                {
                    return new Response(Code.BadRequest, "BranchCode khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                // Resolve BranchCode to Guid BranchId
                var branch = await _unit.GetRepository<Branchs>().Get(x => x.Code == model.BranchCode).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.NotFound, $"KhÃ´ng tÃ¬m tháº¥y chi nhÃ¡nh vá»›i mÃ£ {model.BranchCode}!");
                }

                // Verify FloorPlan
                var fp = await _unit.GetRepository<FloorPlan>().Get(x => x.Id == model.FloorPlanId && x.Status != 0).FirstOrDefaultAsync();
                if (fp == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sÆ¡ Ä‘á»“ phÃ²ng!");
                }
                if (fp.BranchId != branch.Id)
                {
                    return new Response(Code.BadRequest, "SÆ¡ Ä‘á»“ phÃ²ng khÃ´ng thuá»™c chi nhÃ¡nh!");
                }

                using var transaction = await _unit.BeginTransactionAsync();
                var repo = _unit.GetRepository<DiningTable>();

                // Load all existing tables for this branch (including soft-deleted ones)
                var allBranchTables = await repo.Get(x => x.BranchId == branch.Id).ToListAsync();

                // 1. Check duplicate codes in the incoming list
                var incomingDups = model.Tables
                    .GroupBy(x => x.Code.Trim().ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();
                if (incomingDups != null)
                {
                    return new Response(Code.BadRequest, $"MÃ£ bÃ n '{incomingDups}' bá»‹ trÃ¹ng láº·p trong sÆ¡ Ä‘á»“ gá»­i lÃªn!");
                }

                // 2. Validate active table conflicts and resolve soft-deleted table conflicts
                foreach (var t in model.Tables)
                {
                    var codeTrimmed = t.Code.Trim();
                    var codeLower = codeTrimmed.ToLower();
                    
                    var conflicts = allBranchTables
                        .Where(x => x.Id != t.Id && x.Code.Trim().ToLower() == codeLower)
                        .ToList();

                    foreach (var conflict in conflicts)
                    {
                        if (conflict.Status != 0)
                        {
                            var conflictFp = await _unit.GetRepository<FloorPlan>().Get(f => f.Id == conflict.FloorPlanId).FirstOrDefaultAsync();
                            var floorMsg = conflictFp != null ? $" táº¡i máº·t sÃ n '{conflictFp.Name}'" : "";
                            return new Response(Code.Conflict, $"MÃ£ bÃ n '{codeTrimmed}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng{floorMsg}!");
                        }
                        else
                        {
                            // Soft-deleted conflict: rename it to free up the code
                            var suffix = $"_del_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                            conflict.Code = conflict.Code.Length + suffix.Length > 50
                                ? conflict.Code.Substring(0, 50 - suffix.Length) + suffix
                                : conflict.Code + suffix;
                            conflict.UpdatedOnDate = DateTime.UtcNow;
                            conflict.UpdatedByUserCode = userCode ?? "system";
                            conflict.UpdatedByUserName = userName ?? "system";
                            repo.Update(conflict);
                        }
                    }
                }

                // 3. Save layout (Create or Update)
                foreach (var t in model.Tables)
                {
                    var table = allBranchTables.FirstOrDefault(x => x.Id == t.Id);
                    if (table == null)
                    {
                        // Create a new table if it doesn't exist
                        table = new DiningTable
                        {
                            Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                            Code = t.Code.Trim(),
                            Name = t.Name.Trim(),
                            FloorPlanId = model.FloorPlanId,
                            BranchId = branch.Id,
                            Capacity = t.Capacity,
                            X = t.X,
                            Y = t.Y,
                            Width = t.Width > 0 ? t.Width : 100,
                            Height = t.Height > 0 ? t.Height : 100,
                            Rotate = t.Rotate,
                            SortOrder = t.SortOrder,
                            Status = 1,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedByUserCode = userCode ?? "system",
                            CreatedByUserName = userName ?? "system"
                        };
                        repo.Add(table);
                    }
                    else
                    {
                        // Ensure it belongs to the correct floor and branch
                        if (table.FloorPlanId != model.FloorPlanId || table.BranchId != branch.Id)
                        {
                            transaction.Rollback();
                            return new Response(Code.BadRequest, $"BÃ n {table.Name} khÃ´ng thuá»™c Ä‘Ãºng sÆ¡ Ä‘á»“ hoáº·c chi nhÃ¡nh!");
                        }

                        table.Code = t.Code.Trim();
                        table.Name = t.Name.Trim();
                        table.Capacity = t.Capacity;
                        table.X = t.X;
                        table.Y = t.Y;
                        table.Width = t.Width;
                        table.Height = t.Height;
                        table.Rotate = t.Rotate;
                        table.SortOrder = t.SortOrder;
                        table.UpdatedOnDate = DateTime.UtcNow;
                        table.UpdatedByUserCode = userCode ?? "system";
                        table.UpdatedByUserName = userName ?? "system";
                        repo.Update(table);
                    }
                }

                await _unit.SaveAsync();
                transaction.Commit();

                return new Response(Code.Success, "LÆ°u layout bÃ n Äƒn thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("SaveFloorLayout Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> OpenTable(Guid tableId, string branchCode)
        {
            // TODO: Implement actual business logic for opening a table
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] BÃ n {tableId} Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ thÃ nh cÃ´ng táº¡i chi nhÃ¡nh {branchCode}.");
        }

        public async Task<Response> CreateOrder(Guid tableId, string branchCode)
        {
            // TODO: Implement actual business logic for creating an order
            await Task.CompletedTask;
            var mockOrderId = Guid.NewGuid();
            return new ResponseObject<object>(new { OrderId = mockOrderId, TableId = tableId, Status = "Created" }, $"[MOCK] ÄÃ£ táº¡o order {mockOrderId} cho bÃ n {tableId}.");
        }

        public async Task<Response> AddOrderItem(Guid orderId, object item)
        {
            // TODO: Implement actual business logic for adding items
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] Đã thêm món vào order {orderId}.");
        }

        public async Task<Response> UpdateOrderItem(Guid orderId, object item)
        {
            // TODO: Implement actual business logic for updating items
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] Đã cập nhật món trong order {orderId}.");
        }

        public async Task<Response> RemoveOrderItem(Guid orderId, Guid itemId)
        {
            // TODO: Implement actual business logic for removing items
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] Đã xóa món {itemId} khỏi order {orderId}.");
        }

        public async Task<Response> UpdateBill(Guid tableId, string branchCode)
        {
            // TODO: Implement actual business logic for updating the bill
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] Đã cập nhật hóa đơn cho bàn {tableId} tại chi nhánh {branchCode}.");
        }

        public async Task<Response> GetCurrentBill(Guid tableId, string branchCode)
        {
            // TODO: Implement actual business logic for getting the current bill
            await Task.CompletedTask;
            var mockBill = new
            {
                TableId = tableId,
                BranchCode = branchCode,
                BillId = Guid.NewGuid(),
                TotalAmount = 150000.0,
                Items = new List<object>
                {
                    new { FoodName = "Cà phê sữa đá", Quantity = 2, Price = 35000.0 },
                    new { FoodName = "Bánh mì kẹp thịt", Quantity = 1, Price = 45000.0 },
                    new { FoodName = "Trà đào cam sả", Quantity = 1, Price = 35000.0 }
                }
            };
            return new ResponseObject<object>(mockBill, $"[MOCK] Lấy hóa đơn hiện tại của bàn {tableId}.");
        }

        public async Task<Response> CloseBill(Guid tableId, string branchCode)
        {
            // TODO: Implement actual business logic for closing the bill
            await Task.CompletedTask;
            return new Response(Code.Success, $"[MOCK] ÄÃ£ thanh toÃ¡n vÃ  Ä‘Ã³ng hÃ³a Ä‘Æ¡n cho bÃ n {tableId} táº¡i chi nhÃ¡nh {branchCode}.");
        }
    }
}


