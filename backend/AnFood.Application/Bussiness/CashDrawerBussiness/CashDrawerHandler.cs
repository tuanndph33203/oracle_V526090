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

namespace AnFood.Application.Bussiness.CashDrawerBussiness
{
    public class CashDrawerHandler : ICashDrawerHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public CashDrawerHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(CashDrawerCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<CashDrawer>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ kÃ©t Ä‘á»±ng tiá»n Ä‘Ã£ tá»“n táº¡i!");
                    }

                    var item = new CashDrawer
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Name = model.Name.Trim(),
                        Description = model.Description,
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);

                    var branchRepo = _unit.GetRepository<CashDrawerBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new CashDrawerBranch
                            {
                                Id = Guid.NewGuid(),
                                CashDrawerId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("CashDrawer", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Táº¡o kÃ©t Ä‘á»±ng tiá»n thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create CashDrawer Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(CashDrawerUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<CashDrawer>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kÃ©t Ä‘á»±ng tiá»n!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ kÃ©t Ä‘á»±ng tiá»n bá»‹ trÃ¹ng!");
                    }

                    var old = new CashDrawer
                    {
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Status = item.Status
                    };
                    item.Name = model.Name.Trim();
                    item.Description = model.Description;
                    item.Status = model.Status;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);

                    var branchRepo = _unit.GetRepository<CashDrawerBranch>();
                    var oldBranches = await branchRepo.Get(x => x.CashDrawerId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new CashDrawerBranch
                            {
                                Id = Guid.NewGuid(),
                                CashDrawerId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("CashDrawer", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cáº­p nháº­t kÃ©t Ä‘á»±ng tiá»n thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update CashDrawer Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<CashDrawer>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kÃ©t Ä‘á»±ng tiá»n!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("CashDrawer", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a kÃ©t Ä‘á»±ng tiá»n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete CashDrawer Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<CashDrawer>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y kÃ©t Ä‘á»±ng tiá»n!");
                }

                var branchRepo = _unit.GetRepository<CashDrawerBranch>();
                var branches = await branchRepo.Get(x => x.CashDrawerId == item.Id).Select(x => x.BranchId).ToListAsync();

                var model = new CashDrawerDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    Status = item.Status,
                    BranchIds = branches
                };

                return new ResponseObject<CashDrawerDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(CashDrawerFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<CashDrawer>();
                var branchRepo = _unit.GetRepository<CashDrawerBranch>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.BranchId.HasValue)
                {
                    var drawerIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value).Select(x => x.CashDrawerId).ToListAsync();
                    query = query.Where(x => drawerIdsInBranch.Contains(x.Id));
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

                var datas = items.Select(x => new CashDrawerDetailModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Status = x.Status
                }).ToList();

                var response = new Pagination<CashDrawerDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<CashDrawerDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




