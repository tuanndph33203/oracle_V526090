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

namespace AnFood.Application.Bussiness.ReasonBussiness
{
    public class ReasonHandler : IReasonHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public ReasonHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(ReasonCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<Reason>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Type == model.Type && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã lý do cho loại này đã tồn tại!");
                }

                var item = new Reason
                {
                    Id = Guid.NewGuid(),
                    Type = model.Type,
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("Reason", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o lÃ½ do thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Reason Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(ReasonUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                var repo = _unit.GetRepository<Reason>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y lÃ½ do!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.Type == model.Type && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã lý do cho loại này đã tồn tại!");
                }

                var old = new Reason
                {
                    Type = item.Type,
                    Code = item.Code,
                    Name = item.Name,
                    Status = item.Status
                };

                item.Type = model.Type;
                item.Name = model.Name.Trim();
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("Reason", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t lÃ½ do thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Reason Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<Reason>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y lÃ½ do!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("Reason", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a lÃ½ do thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Reason Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<Reason>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y lÃ½ do!");
                }

                var model = new ReasonModel
                {
                    Id = item.Id,
                    Type = item.Type,
                    Code = item.Code,
                    Name = item.Name,
                    Status = item.Status
                };

                return new ResponseObject<ReasonModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(ReasonFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<Reason>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.Type.HasValue) query = query.Where(x => x.Type == model.Type.Value);

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
                    .Select(x => new ReasonModel
                    {
                        Id = x.Id,
                        Type = x.Type,
                        Code = x.Code,
                        Name = x.Name,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<ReasonModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<ReasonModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



