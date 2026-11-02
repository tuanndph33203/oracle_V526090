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

namespace AnFood.Application.Bussiness.MaterialBussiness
{
    public class MaterialHandler : IMaterialHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public MaterialHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(MaterialCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    if (model.MinimumStockQuantity < 0)
                    {
                        return new Response(Code.BadRequest, "Tá»“n tá»‘i thiá»ƒu khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    if (model.ShelfLife.HasValue && model.ShelfLife.Value < 0)
                    {
                        return new Response(Code.BadRequest, "Háº¡n sá»­ dá»¥ng khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    var repo = _unit.GetRepository<Material>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ nguyÃªn váº­t liá»‡u Ä‘Ã£ tá»“n táº¡i!");
                    }

                    var item = new Material
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Name = model.Name.Trim(),
                        PropertyType = model.PropertyType,
                        MaterialGroupId = model.MaterialGroupId,
                        UnitId = model.UnitId,
                        DefaultWarehouseId = model.DefaultWarehouseId,
                        ShelfLife = model.ShelfLife,
                        ShelfLifeUnit = model.ShelfLifeUnit,
                        MinimumStockQuantity = model.MinimumStockQuantity,
                        Note = model.Note,
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);

                    var branchRepo = _unit.GetRepository<MaterialBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new MaterialBranch
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("Material", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Táº¡o nguyÃªn váº­t liá»‡u thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create Material Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(MaterialUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    if (model.MinimumStockQuantity < 0)
                    {
                        return new Response(Code.BadRequest, "Tá»“n tá»‘i thiá»ƒu khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    if (model.ShelfLife.HasValue && model.ShelfLife.Value < 0)
                    {
                        return new Response(Code.BadRequest, "Háº¡n sá»­ dá»¥ng khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    var repo = _unit.GetRepository<Material>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nguyÃªn váº­t liá»‡u!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ nguyÃªn váº­t liá»‡u bá»‹ trÃ¹ng!");
                    }

                    var old = new Material
                    {
                        Code = item.Code,
                        Name = item.Name,
                        PropertyType = item.PropertyType,
                        MaterialGroupId = item.MaterialGroupId,
                        UnitId = item.UnitId,
                        DefaultWarehouseId = item.DefaultWarehouseId,
                        ShelfLife = item.ShelfLife,
                        ShelfLifeUnit = item.ShelfLifeUnit,
                        MinimumStockQuantity = item.MinimumStockQuantity,
                        Note = item.Note,
                        Status = item.Status
                    };
                    item.Name = model.Name.Trim();
                    item.PropertyType = model.PropertyType;
                    item.MaterialGroupId = model.MaterialGroupId;
                    item.UnitId = model.UnitId;
                    item.DefaultWarehouseId = model.DefaultWarehouseId;
                    item.ShelfLife = model.ShelfLife;
                    item.ShelfLifeUnit = model.ShelfLifeUnit;
                    item.MinimumStockQuantity = model.MinimumStockQuantity;
                    item.Note = model.Note;
                    item.Status = model.Status;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);

                    var branchRepo = _unit.GetRepository<MaterialBranch>();
                    var oldBranches = await branchRepo.Get(x => x.MaterialId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new MaterialBranch
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("Material", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cáº­p nháº­t thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update Material Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<Material>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nguyÃªn váº­t liá»‡u!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("Material", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a nguyÃªn váº­t liá»‡u thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Material Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<Material>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nguyÃªn váº­t liá»‡u!");
                }

                var mgRepo = _unit.GetRepository<MaterialGroup>();
                var uRepo = _unit.GetRepository<Unit>();
                var wRepo = _unit.GetRepository<Warehouse>();
                var branchRepo = _unit.GetRepository<MaterialBranch>();

                var mg = item.MaterialGroupId.HasValue ? await mgRepo.Get(x => x.Id == item.MaterialGroupId.Value).FirstOrDefaultAsync() : null;
                var unit = await uRepo.Get(x => x.Id == item.UnitId).FirstOrDefaultAsync();
                var wh = item.DefaultWarehouseId.HasValue ? await wRepo.Get(x => x.Id == item.DefaultWarehouseId.Value).FirstOrDefaultAsync() : null;

                var branches = await branchRepo.Get(x => x.MaterialId == item.Id).Select(x => x.BranchId).ToListAsync();

                var model = new MaterialDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    PropertyType = item.PropertyType,
                    MaterialGroupId = item.MaterialGroupId,
                    MaterialGroupName = mg?.Name,
                    UnitId = item.UnitId,
                    UnitName = unit?.Name,
                    DefaultWarehouseId = item.DefaultWarehouseId,
                    DefaultWarehouseName = wh?.Name,
                    ShelfLife = item.ShelfLife,
                    ShelfLifeUnit = item.ShelfLifeUnit,
                    MinimumStockQuantity = item.MinimumStockQuantity,
                    Note = item.Note,
                    Status = item.Status,
                    BranchIds = branches
                };

                return new ResponseObject<MaterialDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(MaterialFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<Material>();
                var branchRepo = _unit.GetRepository<MaterialBranch>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.PropertyType.HasValue) query = query.Where(x => x.PropertyType == model.PropertyType.Value);
                if (model.MaterialGroupId.HasValue) query = query.Where(x => x.MaterialGroupId == model.MaterialGroupId.Value);
                if (model.UnitId.HasValue) query = query.Where(x => x.UnitId == model.UnitId.Value);
                if (model.DefaultWarehouseId.HasValue) query = query.Where(x => x.DefaultWarehouseId == model.DefaultWarehouseId.Value);

                if (model.BranchId.HasValue)
                {
                    var materialIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value).Select(x => x.MaterialId).ToListAsync();
                    query = query.Where(x => materialIdsInBranch.Contains(x.Id));
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

                var mgRepo = _unit.GetRepository<MaterialGroup>();
                var uRepo = _unit.GetRepository<Unit>();
                var wRepo = _unit.GetRepository<Warehouse>();

                var datas = new List<MaterialDetailModel>();
                foreach (var x in items)
                {
                    var mg = x.MaterialGroupId.HasValue ? await mgRepo.Get(g => g.Id == x.MaterialGroupId.Value).FirstOrDefaultAsync() : null;
                    var unit = await uRepo.Get(u => u.Id == x.UnitId).FirstOrDefaultAsync();
                    var wh = x.DefaultWarehouseId.HasValue ? await wRepo.Get(w => w.Id == x.DefaultWarehouseId.Value).FirstOrDefaultAsync() : null;

                    datas.Add(new MaterialDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        PropertyType = x.PropertyType,
                        MaterialGroupId = x.MaterialGroupId,
                        MaterialGroupName = mg?.Name,
                        UnitId = x.UnitId,
                        UnitName = unit?.Name,
                        DefaultWarehouseId = x.DefaultWarehouseId,
                        DefaultWarehouseName = wh?.Name,
                        ShelfLife = x.ShelfLife,
                        ShelfLifeUnit = x.ShelfLifeUnit,
                        MinimumStockQuantity = x.MinimumStockQuantity,
                        Note = x.Note,
                        Status = x.Status
                    });
                }

                var response = new Pagination<MaterialDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<MaterialDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




