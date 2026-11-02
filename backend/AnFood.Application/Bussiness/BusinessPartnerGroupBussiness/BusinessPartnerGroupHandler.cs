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

namespace AnFood.Application.Bussiness.BusinessPartnerGroupBussiness
{
    public class BusinessPartnerGroupHandler : IBusinessPartnerGroupHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public BusinessPartnerGroupHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(BusinessPartnerGroupCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.ServerError, "Code and Name are required.");
                }

                if (model.GroupTypes <= 0 || model.GroupTypes > 3)
                {
                    return new Response(Code.ServerError, "Invalid GroupTypes.");
                }

                var repo = _unit.GetRepository<BusinessPartnerGroup>();
                var existing = await repo.Get(x => x.Code.ToLower() == model.Code.ToLower().Trim() && x.Status != 0).FirstOrDefaultAsync();
                if (existing != null)
                {
                    return new Response(Code.Conflict, "MÃ£ nhÃ³m khÃ¡ch hÃ ng/nhÃ  cung cáº¥p Ä‘Ã£ tá»“n táº¡i.");
                }

                var item = new BusinessPartnerGroup
                {
                    Id = Guid.NewGuid(),
                    GroupTypes = model.GroupTypes,
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    Description = model.Description,
                    Status = 1,
                    CreatedOnDate = DateTime.Now,
                    CreatedByUserCode = model.CreatedByUserCode,
                    CreatedByUserName = model.CreatedByUserName
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("BusinessPartnerGroup", item.Id.ToString(), item, model.CreatedByUserCode, model.CreatedByUserName);
                return new Response(Code.Success, "ThÃªm má»›i thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create BusinessPartnerGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(BusinessPartnerGroupUpdateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.ServerError, "Code and Name are required.");
                }

                if (model.GroupTypes <= 0 || model.GroupTypes > 3)
                {
                    return new Response(Code.ServerError, "Invalid GroupTypes.");
                }

                var repo = _unit.GetRepository<BusinessPartnerGroup>();
                var item = await repo.Get(x => x.Id == model.Id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m khÃ¡ch hÃ ng/nhÃ  cung cáº¥p.");
                }

                                var requestedCode = model.Code.Trim();
                if (!string.Equals(item.Code, requestedCode, StringComparison.OrdinalIgnoreCase))
                {
                    return new Response(Code.BadRequest, "Ma nhom doi tac khong duoc phep chinh sua sau khi tao.");
                }

                // Check group compatibility update
                if (item.GroupTypes != model.GroupTypes)
                {
                    var partnerRepo = _unit.GetRepository<BusinessPartner>();
                    var linkedPartners = await partnerRepo.Get(x => x.PartnerGroupId == model.Id && x.Status != 0).ToListAsync();
                    foreach (var partner in linkedPartners)
                    {
                        // Check if partner roles are compatible with the new group types
                        // partner types: 1 = Customer, 2 = Supplier, 3 = Both
                        // group types: 1 = CustomerGroup, 2 = SupplierGroup, 3 = Both
                        bool isCompatible = false;
                        if (model.GroupTypes == 3) isCompatible = true;
                        else if (model.GroupTypes == 1 && (partner.PartnerTypes == 1 || partner.PartnerTypes == 3)) isCompatible = true;
                        else if (model.GroupTypes == 2 && (partner.PartnerTypes == 2 || partner.PartnerTypes == 3)) isCompatible = true;

                        if (!isCompatible)
                        {
                            return new Response(Code.Conflict, $"KhÃ´ng thá»ƒ Ä‘á»•i loáº¡i nhÃ³m vÃ¬ cÃ³ Ä‘á»‘i tÃ¡c '{partner.Name}' ({partner.Code}) khÃ´ng tÆ°Æ¡ng thÃ­ch vá»›i loáº¡i nhÃ³m má»›i.");
                        }
                    }
                }

                var old = new BusinessPartnerGroup
                {
                    GroupTypes = item.GroupTypes,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    Status = item.Status
                };

                item.GroupTypes = model.GroupTypes;
                item.Name = model.Name.Trim();
                item.Description = model.Description;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.Now;
                item.UpdatedByUserCode = model.UpdatedByUserCode;

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("BusinessPartnerGroup", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update BusinessPartnerGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartnerGroup>();
                var item = await repo.Get(x => x.Id == id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y nhÃ³m khÃ¡ch hÃ ng/nhÃ  cung cáº¥p.");
                }

                // Check references
                var partnerRepo = _unit.GetRepository<BusinessPartner>();
                var hasPartner = await partnerRepo.Get(x => x.PartnerGroupId == id && x.Status != 0).AnyAsync();
                if (hasPartner)
                {
                    return new Response(Code.Conflict, "KhÃ´ng thá»ƒ xÃ³a nhÃ³m nÃ y vÃ¬ Ä‘ang Ä‘Æ°á»£c sá»­ dá»¥ng bá»Ÿi khÃ¡ch hÃ ng/nhÃ  cung cáº¥p.");
                }

                var old = new BusinessPartnerGroup
                {
                    GroupTypes = item.GroupTypes,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    Status = item.Status
                };

                item.Status = 0; // Soft delete
                item.UpdatedOnDate = DateTime.Now;
                item.UpdatedByUserCode = userCode;

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("BusinessPartnerGroup", item.Id.ToString(), old, userCode, userName);
                return new Response(Code.Success, "XÃ³a thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete BusinessPartnerGroup Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartnerGroup>();
                var item = await repo.Get(x => x.Id == id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y.");
                }

                var res = new BusinessPartnerGroupModel
                {
                    Id = item.Id,
                    GroupTypes = item.GroupTypes,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    Status = item.Status
                };

                return new ResponseObject<BusinessPartnerGroupModel>(res, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(BusinessPartnerGroupFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartnerGroup>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.GroupTypes.HasValue)
                {
                    // Match flags (if 3 matches both, otherwise single role matching)
                    if (model.GroupTypes.Value == 3)
                    {
                        query = query.Where(x => x.GroupTypes == 3);
                    }
                    else
                    {
                        query = query.Where(x => x.GroupTypes == model.GroupTypes.Value || x.GroupTypes == 3);
                    }
                }

                if (model.Status.HasValue)
                {
                    query = query.Where(x => x.Status == model.Status.Value);
                }

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var datas = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .Select(x => new BusinessPartnerGroupModel
                    {
                        Id = x.Id,
                        GroupTypes = x.GroupTypes,
                        Code = x.Code,
                        Name = x.Name,
                        Description = x.Description,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<BusinessPartnerGroupModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = (model.Page ?? 0) + 1,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<BusinessPartnerGroupModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter BusinessPartnerGroup Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




