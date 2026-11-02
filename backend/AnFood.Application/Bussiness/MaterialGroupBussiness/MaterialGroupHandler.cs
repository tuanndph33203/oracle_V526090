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

namespace AnFood.Application.Bussiness.MaterialGroupBussiness
{
    public class MaterialGroupHandler : IMaterialGroupHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public MaterialGroupHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(MaterialGroupCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<MaterialGroup>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã nhóm nguyên vật liệu đã tồn tại!");
                }

                if (model.ParentId.HasValue)
                {
                    var parentExists = await repo.Get(x => x.Id == model.ParentId.Value && x.Status != 0).AnyAsync();
                    if (!parentExists)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m cha!");
                    }
                }

                var item = new MaterialGroup
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
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("MaterialGroup", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o nhÃ³m nguyÃªn váº­t liá»‡u thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create MaterialGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(MaterialGroupUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<MaterialGroup>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m nguyÃªn váº­t liá»‡u!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã nhóm nguyên vật liệu đã tồn tại!");
                }

                if (model.ParentId.HasValue)
                {
                    if (model.ParentId.Value == model.Id)
                    {
                        return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ chá»n nhÃ³m cha lÃ  chÃ­nh nÃ³!");
                    }

                    // Cycle check
                    var currentParentId = model.ParentId;
                    while (currentParentId.HasValue)
                    {
                        if (currentParentId.Value == model.Id)
                        {
                            return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ chá»n nhÃ³m cha gÃ¢y ra vÃ²ng láº·p vÃ´ háº¡n!");
                        }
                        var parentNode = await repo.Get(x => x.Id == currentParentId.Value).FirstOrDefaultAsync();
                        currentParentId = parentNode?.ParentId;
                    }
                }

                var old = new MaterialGroup
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
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("MaterialGroup", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update MaterialGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<MaterialGroup>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m nguyÃªn váº­t liá»‡u!");
                }

                // Check child groups
                var hasChildren = await repo.Get(x => x.ParentId == id && x.Status != 0).AnyAsync();
                if (hasChildren)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a nhÃ³m nÃ y vÃ¬ cÃ³ cÃ¡c nhÃ³m con Ä‘ang hoáº¡t Ä‘á»™ng!");
                }

                // Check referenced Materials
                var materialRepo = _unit.GetRepository<Material>();
                var isReferenced = await materialRepo.Get(x => x.MaterialGroupId == id && x.Status != 0).AnyAsync();
                if (isReferenced)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a nhÃ³m nÃ y vÃ¬ Ä‘Ã£ cÃ³ nguyÃªn váº­t liá»‡u tham chiáº¿u tá»›i!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("MaterialGroup", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a nhÃ³m nguyÃªn váº­t liá»‡u thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete MaterialGroup Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<MaterialGroup>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m nguyÃªn váº­t liá»‡u!");
                }

                var model = new MaterialGroupModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    ParentId = item.ParentId,
                    Description = item.Description,
                    Status = item.Status
                };

                return new ResponseObject<MaterialGroupModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(MaterialGroupFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<MaterialGroup>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.ParentId.HasValue)
                {
                    query = query.Where(x => x.ParentId == model.ParentId.Value);
                }

                if (model.IsInactive.HasValue)
                {
                    int targetStatus = model.IsInactive.Value ? 2 : 1;
                    query = query.Where(x => x.Status == targetStatus);
                }

                model.Size ??= 1000; // tree lists typically need all nodes
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var datas = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .Select(x => new MaterialGroupModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        ParentId = x.ParentId,
                        Description = x.Description,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<MaterialGroupModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<MaterialGroupModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



