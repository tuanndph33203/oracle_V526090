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

namespace AnFood.Application.Bussiness.WarehouseBussiness
{
    public class WarehouseHandler : IWarehouseHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public WarehouseHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(WarehouseCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<Warehouse>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ kho Ä‘Ã£ tá»“n táº¡i!");
                    }

                    if (model.ParentId.HasValue)
                    {
                        var parentExists = await repo.Get(x => x.Id == model.ParentId.Value && x.Status != 0).AnyAsync();
                        if (!parentExists)
                        {
                            return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kho cha!");
                        }
                    }

                    var item = new Warehouse
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Name = model.Name.Trim(),
                        ParentId = model.ParentId,
                        Description = model.Description,
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);

                    var branchRepo = _unit.GetRepository<WarehouseBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new WarehouseBranch
                            {
                                Id = Guid.NewGuid(),
                                WarehouseId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("Warehouse", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Táº¡o kho thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create Warehouse Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(WarehouseUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<Warehouse>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kho!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ kho bá»‹ trÃ¹ng!");
                    }

                    if (model.ParentId.HasValue)
                    {
                        if (model.ParentId.Value == model.Id)
                        {
                            return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ chá»n kho cha lÃ  chÃ­nh nÃ³!");
                        }

                        // Cycle check
                        var currentParentId = model.ParentId;
                        while (currentParentId.HasValue)
                        {
                            if (currentParentId.Value == model.Id)
                            {
                                return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ chá»n kho cha gÃ¢y ra vÃ²ng láº·p vÃ´ háº¡n!");
                            }
                            var parentNode = await repo.Get(x => x.Id == currentParentId.Value).FirstOrDefaultAsync();
                            currentParentId = parentNode?.ParentId;
                        }
                    }

                    var old = new Warehouse
                    {
                        Code = item.Code,
                        Name = item.Name,
                        ParentId = item.ParentId,
                        Description = item.Description,
                        Status = item.Status
                    };
                    item.Name = model.Name.Trim();
                    item.ParentId = model.ParentId;
                    item.Description = model.Description;
                    item.Status = model.Status;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);

                    var branchRepo = _unit.GetRepository<WarehouseBranch>();
                    var oldBranches = await branchRepo.Get(x => x.WarehouseId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new WarehouseBranch
                            {
                                Id = Guid.NewGuid(),
                                WarehouseId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("Warehouse", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cáº­p nháº­t thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update Warehouse Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<Warehouse>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kho!");
                }

                // Check child nodes
                var hasChildren = await repo.Get(x => x.ParentId == id && x.Status != 0).AnyAsync();
                if (hasChildren)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a kho nÃ y vÃ¬ cÃ³ cÃ¡c kho con Ä‘ang hoáº¡t Ä‘á»™ng!");
                }

                // Check referenced Materials
                var materialRepo = _unit.GetRepository<Material>();
                var isReferenced = await materialRepo.Get(x => x.DefaultWarehouseId == id && x.Status != 0).AnyAsync();
                if (isReferenced)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a kho nÃ y vÃ¬ cÃ³ nguyÃªn váº­t liá»‡u tham chiáº¿u tá»›i!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("Warehouse", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a kho thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Warehouse Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<Warehouse>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kho!");
                }

                var parent = item.ParentId.HasValue ? await repo.Get(x => x.Id == item.ParentId.Value).FirstOrDefaultAsync() : null;
                var branchRepo = _unit.GetRepository<WarehouseBranch>();
                var branches = await branchRepo.Get(x => x.WarehouseId == item.Id).Select(x => x.BranchId).ToListAsync();

                var model = new WarehouseDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    ParentId = item.ParentId,
                    ParentName = parent?.Name,
                    Description = item.Description,
                    Status = item.Status,
                    BranchIds = branches
                };

                return new ResponseObject<WarehouseDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(WarehouseFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<Warehouse>();
                var branchRepo = _unit.GetRepository<WarehouseBranch>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.ParentId.HasValue) query = query.Where(x => x.ParentId == model.ParentId.Value);

                if (model.BranchId.HasValue)
                {
                    var warehouseIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value).Select(x => x.WarehouseId).ToListAsync();
                    query = query.Where(x => warehouseIdsInBranch.Contains(x.Id));
                }

                if (model.IsInactive.HasValue)
                {
                    int targetStatus = model.IsInactive.Value ? 2 : 1;
                    query = query.Where(x => x.Status == targetStatus);
                }

                model.Size ??= 1000;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var items = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var datas = new List<WarehouseDetailModel>();
                foreach (var x in items)
                {
                    var parent = x.ParentId.HasValue ? await repo.Get(p => p.Id == x.ParentId.Value).FirstOrDefaultAsync() : null;
                    datas.Add(new WarehouseDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        ParentId = x.ParentId,
                        ParentName = parent?.Name,
                        Description = x.Description,
                        Status = x.Status
                    });
                }

                var response = new Pagination<WarehouseDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<WarehouseDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




