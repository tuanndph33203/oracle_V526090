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

namespace AnFood.Application.Bussiness.FloorPlanBussiness
{
    public class FloorPlanHandler : IFloorPlanHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public FloorPlanHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(FloorPlanCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<FloorPlan>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.BranchId == model.BranchId && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã sơ đồ đã tồn tại ở chi nhánh này!");
                }

                var item = new FloorPlan
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    BranchId = model.BranchId,
                    Description = model.Description,
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("FloorPlan", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o sÆ¡ Ä‘á»“ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create FloorPlan Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(FloorPlanUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<FloorPlan>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sÆ¡ Ä‘á»“!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.BranchId == model.BranchId && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã sơ đồ đã tồn tại ở chi nhánh này!");
                }

                var old = new FloorPlan
                {
                    Code = item.Code,
                    Name = item.Name,
                    BranchId = item.BranchId,
                    Description = item.Description,
                    Status = item.Status
                };
                item.Name = model.Name.Trim();
                item.BranchId = model.BranchId;
                item.Description = model.Description;
                item.Status = model.Status == 0 ? item.Status : model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("FloorPlan", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t sÆ¡ Ä‘á»“ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update FloorPlan Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<FloorPlan>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sÆ¡ Ä‘á»“!");
                }

                var branchRepo = _unit.GetRepository<Branchs>();
                var branch = await branchRepo.Get(x => x.Id == item.BranchId && x.Status != 0).FirstOrDefaultAsync();
                if (branch == null)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng tÃ¬m tháº¥y chi nhÃ¡nh cá»§a sÆ¡ Ä‘á»“ nÃ y!");
                }
                var branchCode = branch.Code;

                var tableRepo = _unit.GetRepository<DiningTable>();
                var activeTables = await tableRepo.Get(x => x.FloorPlanId == id && x.BranchId == item.BranchId && x.Status != 0).ToListAsync();

                if (activeTables.Any())
                {
                    var tableIds = activeTables.Select(t => t.Id).ToList();
                    var orderTableRepo = _unit.GetRepository<SalesOrderTable>();
                    var orderRepo = _unit.GetRepository<SalesOrder>();
                    var activeStatuses = new[] { "WaitingService", "Reserved", "Serving", "WaitingPayment", "Payment" };

                    var hasActiveOrders = await orderTableRepo.Get(ot => tableIds.Contains(ot.TableId) && ot.BranchCode == branchCode)
                        .Join(orderRepo.Get(o => o.BranchCode == branchCode && activeStatuses.Contains(o.Status)),
                              ot => ot.OrderId,
                              o => o.Id,
                              (ot, o) => o)
                        .AnyAsync();

                    if (hasActiveOrders)
                    {
                        return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a sÆ¡ Ä‘á»“ nÃ y vÃ¬ cÃ³ bÃ n Äƒn Ä‘ang hoáº¡t Ä‘á»™ng tham chiáº¿u tá»›i!");
                    }
                }

                // Soft delete active tables belonging to this floor plan
                foreach (var table in activeTables)
                {
                    table.Status = 0;
                    var suffix = $"_del_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    table.Code = table.Code.Length + suffix.Length > 50 
                        ? table.Code.Substring(0, 50 - suffix.Length) + suffix 
                        : table.Code + suffix;
                    table.UpdatedOnDate = DateTime.UtcNow;
                    table.UpdatedByUserCode = userCode ?? "system";
                    table.UpdatedByUserName = userName ?? "system";
                    tableRepo.Update(table);
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("FloorPlan", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a sÆ¡ Ä‘á»“ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete FloorPlan Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<FloorPlan>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sÆ¡ Ä‘á»“!");
                }

                var bRepo = _unit.GetRepository<Branchs>();
                var branch = await bRepo.Get(x => x.Id == item.BranchId).FirstOrDefaultAsync();

                var model = new FloorPlanDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    BranchId = item.BranchId,
                    BranchName = branch?.Name,
                    Description = item.Description,
                    Status = item.Status
                };

                return new ResponseObject<FloorPlanDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(FloorPlanFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<FloorPlan>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.BranchId.HasValue) query = query.Where(x => x.BranchId == model.BranchId.Value);

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
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var bRepo = _unit.GetRepository<Branchs>();
                var datas = new List<FloorPlanDetailModel>();
                foreach (var x in items)
                {
                    var branch = await bRepo.Get(b => b.Id == x.BranchId).FirstOrDefaultAsync();
                    datas.Add(new FloorPlanDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        BranchId = x.BranchId,
                        BranchName = branch?.Name,
                        Description = x.Description,
                        Status = x.Status
                    });
                }

                var response = new Pagination<FloorPlanDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<FloorPlanDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



