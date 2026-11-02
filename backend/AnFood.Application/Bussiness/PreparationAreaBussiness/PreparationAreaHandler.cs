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

namespace AnFood.Application.Bussiness.PreparationAreaBussiness
{
    public class PreparationAreaHandler : IPreparationAreaHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public PreparationAreaHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(PreparationAreaCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<PreparationArea>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ báº¿p/bar Ä‘Ã£ tá»“n táº¡i!");
                    }

                    var item = new PreparationArea
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Type = model.Type,
                        Name = model.Name.Trim(),
                        DeviceType = model.DeviceType,
                        LabelPrintTiming = model.LabelPrintTiming,
                        Description = model.Description,
                        ProcessingArea = model.ProcessingArea,
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);

                    var branchRepo = _unit.GetRepository<PreparationAreaBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new PreparationAreaBranch
                            {
                                Id = Guid.NewGuid(),
                                PreparationAreaId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("PreparationArea", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Táº¡o báº¿p/bar thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create PreparationArea Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(PreparationAreaUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<PreparationArea>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y báº¿p/bar!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ báº¿p/bar bá»‹ trÃ¹ng!");
                    }

                    var old = new PreparationArea
                    {
                        Code = item.Code,
                        Type = item.Type,
                        Name = item.Name,
                        DeviceType = item.DeviceType,
                        LabelPrintTiming = item.LabelPrintTiming,
                        Description = item.Description,
                        ProcessingArea = item.ProcessingArea,
                        Status = item.Status
                    };
                    item.Type = model.Type;
                    item.Name = model.Name.Trim();
                    item.DeviceType = model.DeviceType;
                    item.LabelPrintTiming = model.LabelPrintTiming;
                    item.Description = model.Description;
                    item.ProcessingArea = model.ProcessingArea;
                    item.Status = model.Status;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);

                    var branchRepo = _unit.GetRepository<PreparationAreaBranch>();
                    var oldBranches = await branchRepo.Get(x => x.PreparationAreaId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new PreparationAreaBranch
                            {
                                Id = Guid.NewGuid(),
                                PreparationAreaId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("PreparationArea", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cáº­p nháº­t báº¿p/bar thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update PreparationArea Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<PreparationArea>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y báº¿p/bar!");
                }

                // Check references in MenuGroup
                var menuGroupRepo = _unit.GetRepository<MenuGroup>();
                if (await menuGroupRepo.Get(x => x.PreparationAreaId == id && x.Status != 0).AnyAsync())
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a báº¿p/bar nÃ y vÃ¬ cÃ³ nhÃ³m thá»±c Ä‘Æ¡n Ä‘ang tham chiáº¿u tá»›i!");
                }

                // Check references in MenuItem
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                if (await menuItemRepo.Get(x => x.PreparationAreaId == id && x.Status != 0).AnyAsync())
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a báº¿p/bar nÃ y vÃ¬ cÃ³ thá»±c Ä‘Æ¡n Ä‘ang tham chiáº¿u tá»›i!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("PreparationArea", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a báº¿p/bar thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete PreparationArea Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<PreparationArea>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y báº¿p/bar!");
                }

                var branchRepo = _unit.GetRepository<PreparationAreaBranch>();
                var branches = await branchRepo.Get(x => x.PreparationAreaId == item.Id).Select(x => x.BranchId).ToListAsync();

                var model = new PreparationAreaDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Type = item.Type,
                    Name = item.Name,
                    DeviceType = item.DeviceType,
                    LabelPrintTiming = item.LabelPrintTiming,
                    Description = item.Description,
                    ProcessingArea = item.ProcessingArea,
                    Status = item.Status,
                    BranchIds = branches
                };

                return new ResponseObject<PreparationAreaDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(PreparationAreaFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<PreparationArea>();
                var branchRepo = _unit.GetRepository<PreparationAreaBranch>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.Type.HasValue) query = query.Where(x => x.Type == model.Type.Value);

                if (model.BranchId.HasValue)
                {
                    var areaIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value).Select(x => x.PreparationAreaId).ToListAsync();
                    query = query.Where(x => areaIdsInBranch.Contains(x.Id));
                }

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

                var areaIds = items.Select(x => x.Id).ToList();
                var branchesByArea = await branchRepo.Get(x => areaIds.Contains(x.PreparationAreaId))
                    .GroupBy(x => x.PreparationAreaId)
                    .Select(group => new { PreparationAreaId = group.Key, BranchIds = group.Select(x => x.BranchId).ToList() })
                    .ToListAsync();
                var branchIdsByArea = branchesByArea.ToDictionary(x => x.PreparationAreaId, x => x.BranchIds);

                var datas = items.Select(x => new PreparationAreaDetailModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Type = x.Type,
                    Name = x.Name,
                    DeviceType = x.DeviceType,
                    LabelPrintTiming = x.LabelPrintTiming,
                    Description = x.Description,
                    ProcessingArea = x.ProcessingArea,
                    Status = x.Status,
                    BranchIds = branchIdsByArea.TryGetValue(x.Id, out var branchIds) ? branchIds : new List<Guid>()
                }).ToList();

                var response = new Pagination<PreparationAreaDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<PreparationAreaDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




