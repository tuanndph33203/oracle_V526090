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

namespace AnFood.Application.Bussiness.TaxRateBussiness
{
    public class TaxRateHandler : ITaxRateHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public TaxRateHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(TaxRateCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    return new Response(Code.BadRequest, "MÃ£ thuáº¿ suáº¥t khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                if (model.Rate < 0 || model.Rate > 100)
                {
                    return new Response(Code.BadRequest, "Thuáº¿ suáº¥t pháº£i tá»« 0% Ä‘áº¿n 100%!");
                }

                var repo = _unit.GetRepository<TaxRate>();
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã thuế suất đã tồn tại!");
                }

                var item = new TaxRate
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Rate = model.Rate,
                    Description = model.Description,
                    Status = 1,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system"
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("TaxRate", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Táº¡o thuáº¿ suáº¥t thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create TaxRate Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(TaxRateUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    return new Response(Code.BadRequest, "MÃ£ thuáº¿ suáº¥t khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                }

                if (model.Rate < 0 || model.Rate > 100)
                {
                    return new Response(Code.BadRequest, "Thuáº¿ suáº¥t pháº£i tá»« 0% Ä‘áº¿n 100%!");
                }

                var repo = _unit.GetRepository<TaxRate>();
                var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y thuáº¿ suáº¥t!");
                }
                var codeLower = model.Code.Trim().ToLower();
                var exists = await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync();
                if (exists)
                {
                    return new Response(Code.Conflict, "Mã thuế suất đã tồn tại!");
                }

                var old = new TaxRate
                {
                    Code = item.Code,
                    Rate = item.Rate,
                    Description = item.Description,
                    Status = item.Status
                };
                item.Rate = model.Rate;
                item.Description = model.Description;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("TaxRate", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t thuáº¿ suáº¥t thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update TaxRate Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<TaxRate>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y thuáº¿ suáº¥t!");
                }

                // Check references in MenuItem
                var menuItemRepo = _unit.GetRepository<MenuItem>();
                var isReferenced = await menuItemRepo.Get(x => x.TaxRateId == id && x.Status != 0).AnyAsync();
                if (isReferenced)
                {
                    return new Response(Code.BadRequest, "KhÃ´ng thá»ƒ xÃ³a thuáº¿ suáº¥t nÃ y vÃ¬ Ä‘ang cÃ³ mÃ³n Äƒn tham chiáº¿u tá»›i. Vui lÃ²ng chuyá»ƒn sang tráº¡ng thÃ¡i Ngá»«ng sá»­ dá»¥ng!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("TaxRate", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a thuáº¿ suáº¥t thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete TaxRate Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<TaxRate>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y thuáº¿ suáº¥t!");
                }

                var model = new TaxRateModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Rate = item.Rate,
                    Description = item.Description,
                    Status = item.Status
                };

                return new ResponseObject<TaxRateModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(TaxRateFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<TaxRate>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Description.ToLower().Contains(textLower));
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
                    .Select(x => new TaxRateModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Rate = x.Rate,
                        Description = x.Description,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<TaxRateModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<TaxRateModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}



