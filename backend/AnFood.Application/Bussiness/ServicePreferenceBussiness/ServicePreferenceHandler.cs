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

namespace AnFood.Application.Bussiness.ServicePreferenceBussiness
{
    public class ServicePreferenceHandler : IServicePreferenceHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public ServicePreferenceHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(ServicePreferenceCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                if (model.DefaultExtraCharge < 0)
                {
                    return new Response(Code.BadRequest, "PhÃ­ phá»¥ thu máº·c Ä‘á»‹nh khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                }

                var repo = _unit.GetRepository<ServicePreference>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã sở thích phục vụ đã tồn tại!");
                }

                var item = new ServicePreference
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    DefaultExtraCharge = model.DefaultExtraCharge,
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("ServicePreference", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o sá»Ÿ thÃ­ch phá»¥c vá»¥ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create ServicePreference Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(ServicePreferenceUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                if (model.DefaultExtraCharge < 0)
                {
                    return new Response(Code.BadRequest, "PhÃ­ phá»¥ thu máº·c Ä‘á»‹nh khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                }

                var repo = _unit.GetRepository<ServicePreference>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sá»Ÿ thÃ­ch phá»¥c vá»¥!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã sở thích phục vụ đã tồn tại!");
                }

                var old = new ServicePreference
                {
                    Code = item.Code,
                    Name = item.Name,
                    DefaultExtraCharge = item.DefaultExtraCharge,
                    Status = item.Status
                };
                item.Name = model.Name.Trim();
                item.DefaultExtraCharge = model.DefaultExtraCharge;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("ServicePreference", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t sá»Ÿ thÃ­ch phá»¥c vá»¥ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update ServicePreference Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<ServicePreference>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sá»Ÿ thÃ­ch phá»¥c vá»¥!");
                }

                // Check references in MenuItemServicePreference
                var mipRepo = _unit.GetRepository<MenuItemServicePreference>();
                var isReferenced = await mipRepo.Get(x => x.ServicePreferenceId == id).AnyAsync();
                if (isReferenced)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a sá»Ÿ thÃ­ch phá»¥c vá»¥ nÃ y vÃ¬ Ä‘ang cÃ³ mÃ³n Äƒn tham chiáº¿u tá»›i. Vui lÃ²ng chuyá»ƒn sang tráº¡ng thÃ¡i Ngá»«ng sá»­ dá»¥ng!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("ServicePreference", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a sá»Ÿ thÃ­ch phá»¥c vá»¥ thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete ServicePreference Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<ServicePreference>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y sá»Ÿ thÃ­ch phá»¥c vá»¥!");
                }

                var model = new ServicePreferenceModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    DefaultExtraCharge = item.DefaultExtraCharge,
                    Status = item.Status
                };

                return new ResponseObject<ServicePreferenceModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(ServicePreferenceFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<ServicePreference>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
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

                var datas = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .Select(x => new ServicePreferenceModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        DefaultExtraCharge = x.DefaultExtraCharge,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<ServicePreferenceModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<ServicePreferenceModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



