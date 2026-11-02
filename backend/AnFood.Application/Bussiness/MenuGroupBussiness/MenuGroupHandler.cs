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

namespace AnFood.Application.Bussiness.MenuGroupBussiness
{
    public class MenuGroupHandler : IMenuGroupHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public MenuGroupHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(MenuGroupCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<MenuGroup>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã nhóm thực đơn đã tồn tại!");
                }

                var item = new MenuGroup
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    Type = model.Type,
                    Priority = model.Priority,
                    PreparationAreaId = model.PreparationAreaId,
                    Description = model.Description,
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("MenuGroup", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o nhÃ³m thá»±c Ä‘Æ¡n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create MenuGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(MenuGroupUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<MenuGroup>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m thá»±c Ä‘Æ¡n!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã nhóm thực đơn đã tồn tại!");
                }

                var old = new MenuGroup
                {
                    Code = item.Code,
                    Name = item.Name,
                    Type = item.Type,
                    Priority = item.Priority,
                    PreparationAreaId = item.PreparationAreaId,
                    Description = item.Description,
                    Status = item.Status
                };
                item.Code = model.Code.Trim();
                item.Name = model.Name.Trim();
                item.Type = model.Type;
                item.Priority = model.Priority;
                item.PreparationAreaId = model.PreparationAreaId;
                item.Description = model.Description;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("MenuGroup", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update MenuGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<MenuGroup>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m thá»±c Ä‘Æ¡n!");
                }

                // Check references
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var activeItemIds = menuItemRepo.Get(x => x.Status != 0).Select(x => x.Id);
                var isReferenced = await menuItemRepo.Get(x => x.MenuGroupId == id && x.Status != 0).AnyAsync()
                    || await _unit.GetRepository<MenuItemGroup>().Get(x => x.MenuGroupId == id && activeItemIds.Contains(x.MenuItemId)).AnyAsync();
                if (isReferenced)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a nhÃ³m thá»±c Ä‘Æ¡n nÃ y vÃ¬ Ä‘Ã£ cÃ³ mÃ³n Äƒn tham chiáº¿u tá»›i. Vui lÃ²ng chuyá»ƒn sang tráº¡ng thÃ¡i Ngá»«ng sá»­ dá»¥ng!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("MenuGroup", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a nhÃ³m thá»±c Ä‘Æ¡n thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete MenuGroup Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<MenuGroup>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m thá»±c Ä‘Æ¡n!");
                }

                string? prepAreaName = null;
                if (item.PreparationAreaId.HasValue)
                {
                    var prepArea = await _unit.GetRepository<PreparationArea>()
                        .Get(x => x.Id == item.PreparationAreaId.Value)
                        .FirstOrDefaultAsync();
                    prepAreaName = prepArea?.Name;
                }

                var model = new MenuGroupModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    Type = item.Type,
                    Priority = item.Priority,
                    PreparationAreaId = item.PreparationAreaId,
                    PreparationAreaName = prepAreaName,
                    Description = item.Description,
                    Status = item.Status
                };

                return new ResponseObject<MenuGroupModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(MenuGroupFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<MenuGroup>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.Type.HasValue)
                {
                    query = query.Where(x => x.Type == model.Type.Value);
                }

                if (model.PreparationAreaId.HasValue)
                {
                    query = query.Where(x => x.PreparationAreaId == model.PreparationAreaId.Value);
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

                var prepAreaRepo = _unit.GetRepository<PreparationArea>();
                var prepAreas = await prepAreaRepo.Get(x => x.Status != 0).ToListAsync();
                var prepAreaDict = prepAreas.ToDictionary(x => x.Id, x => x.Name);

                var datas = await query
                    .OrderBy(x => x.Priority.HasValue ? 0 : 1).ThenBy(x => x.Priority).ThenBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .Select(x => new MenuGroupModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        Type = x.Type,
                        Priority = x.Priority,
                        PreparationAreaId = x.PreparationAreaId,
                        Description = x.Description,
                        Status = x.Status
                    })
                    .ToListAsync();

                foreach (var item in datas)
                {
                    if (item.PreparationAreaId.HasValue && prepAreaDict.TryGetValue(item.PreparationAreaId.Value, out var pName))
                    {
                        item.PreparationAreaName = pName;
                    }
                }

                var response = new Pagination<MenuGroupModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<MenuGroupModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



