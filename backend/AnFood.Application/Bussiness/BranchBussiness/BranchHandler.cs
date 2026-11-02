using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.BranchBussiness
{
    public class BranchHandler : IBranchHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        private readonly IAuditLogger _logsaudit;

        public BranchHandler(IAppLogger logsapp, IUnitOfWork unit, IAuditLogger logsaudit)
        {
            _logsapp = logsapp;
            _unit = unit;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(BranchCreateModel model)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var checkExitCode = await branchRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                if (checkExitCode != null)
                {
                    return new Response(Code.Conflict, "Code Exit");
                }

                var item = new Branchs
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name,
                    CityCode = model.CityCode,
                    DistrictCode = model.DistrictCode,
                    WardCode = model.WardCode,
                    Address = model.Address,
                    MapUrl = model.MapUrl,
                    Phone = model.Phone,
                    Notes = model.Notes,
                    Email = model.Email,
                    Status = 1,
                    CreatedOnDate = DateTime.Now,
                    CreatedByUserCode = model.CreatedByUserId,
                    CreatedByUserName = model.CreatedByUserName
                };

                branchRepo.Add(item);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save Branchs Error");
                }

                await _logsaudit.LogCreateAsync("Branchs", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Branchs Error", ex, model);
                Log.Error("Branchs Create Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var checkExit = await branchRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                branchRepo.Delete(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Delete Branchs Error");
                }

                await _logsaudit.LogDeleteAsync("Branchs", checkExit.Id.ToString(), checkExit, deletedByUserId, deletedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Branchs Error", ex, id);
                Log.Error("Branchs Delete Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(BranchFilterModel model)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var query = branchRepo.GetAll();

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                model.Size ??= 1000;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var datas = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .Select(x => new BranchDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        CityCode = x.CityCode,
                        DistrictCode = x.DistrictCode,
                        WardCode = x.WardCode,
                        Address = x.Address,
                        MapUrl = x.MapUrl,
                        Phone = x.Phone,
                        Notes = x.Notes,
                        Email = x.Email,
                        Status = x.Status
                    })
                    .ToListAsync();

                var response = new Pagination<BranchDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<BranchDetailModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Branchs Error", ex, model);
                Log.Error("Branchs GetByFilter Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var checkExit = await branchRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                var res = new BranchDetailModel
                {
                    Id = checkExit.Id,
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    CityCode = checkExit.CityCode,
                    DistrictCode = checkExit.DistrictCode,
                    WardCode = checkExit.WardCode,
                    Address = checkExit.Address,
                    MapUrl = checkExit.MapUrl,
                    Phone = checkExit.Phone,
                    Notes = checkExit.Notes,
                    Email = checkExit.Email,
                    Status = checkExit.Status
                };

                return new ResponseObject<BranchDetailModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Branchs Error", ex, id);
                Log.Error("Branchs GetById Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(BranchUpdateModel model)
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var checkExit = await branchRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                var old = new Branchs
                {
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    CityCode = checkExit.CityCode,
                    DistrictCode = checkExit.DistrictCode,
                    WardCode = checkExit.WardCode,
                    Status = checkExit.Status,
                    Address = checkExit.Address,
                    MapUrl = checkExit.MapUrl,
                    Phone = checkExit.Phone,
                    Notes = checkExit.Notes,
                    Email = checkExit.Email
                };

                checkExit.Code = model.Code;
                checkExit.Name = model.Name;
                checkExit.CityCode = model.CityCode;
                checkExit.DistrictCode = model.DistrictCode;
                checkExit.WardCode = model.WardCode;
                checkExit.Status = model.Status;
                checkExit.Address = model.Address;
                checkExit.MapUrl = model.MapUrl;
                checkExit.Phone = model.Phone;
                checkExit.Notes = model.Notes;
                checkExit.Email = model.Email;

                branchRepo.Update(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Update DB Error");
                }

                await _logsaudit.LogUpdateAsync("Branchs", checkExit.Id.ToString(), old, checkExit, model.UpdatedByUserId, model.UpdatedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Branchs Error", ex, model);
                Log.Error("Branchs Update Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
