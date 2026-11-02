using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.WardBussiness
{
    public class WardHandler : IWardHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public WardHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit=unit;
        }
        public async Task<Response> Create(CreateWardModel model)
        {
            try
            {
                var wardsRepo = _unit.GetRepository<Wards>();
                var checkExitCode = await wardsRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                if (checkExitCode != null)
                {
                    return new Response(Code.Conflict, "Code Exit");
                }
                Wards item = new Wards()
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name,
                    IsDelete = false,
                    CityId = model.CityId
                };
                wardsRepo.Add(item);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save Wards Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Wards Error", ex, model);
                Log.Error("Wards Create Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id)
        {
            try
            {
                var WardsRepo = _unit.GetRepository<Wards>();
                var checkExit = await WardsRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }
                WardsRepo.Delete(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Delete Wards Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Wards Error", ex, Id);
                Log.Error("Wards Delete Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(WardFilterModel model)
        {
            try
            {
                var wardsRepo = _unit.GetRepository<Wards>();
                var query = wardsRepo.GetAll().AsNoTracking();
                if (model.CityId.HasValue)
                {
                    query = query.Where(x => x.CityId == model.CityId.Value);
                }
                if (model.IsGetDeleted.HasValue)
                {
                    query = query.Where(x => x.IsDelete == model.IsGetDeleted.Value);
                }
                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }
                if (!model.IsAll.HasValue || model.IsAll.Value == false)
                {
                    if (!model.Size.HasValue)
                    {
                        model.Size = 20;
                    }
                    model.Page = model.Page - 1;
                    int excludedRows = ((int)model.Page) * ((int)model.Size);
                    if (excludedRows <= 0)
                    {
                        excludedRows = 0;
                    }
                    query = query.Skip(excludedRows).Take((int)model.Size);
                }
                var totalCount = query.Count();
                query = query.OrderByDescending(x => x.Code);
                
                var datas = await (
                    from d in query.AsNoTracking()
                    join c in _unit.GetRepository<Cities>().GetAll().AsNoTracking()
                        on d.CityId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    select new WardDetailModel
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Code = d.Code,
                        IsDelete = d.IsDelete,
                        City = c == null ? null : new CityModel
                        {
                            Id = c.Id,
                            Code = c.Code,
                            Name = c.Name,
                            IsDeleted = c.IsDeleted
                        }
                    }
                ).ToListAsync();

                var response = new Pagination<WardDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page.HasValue ? model.Page.Value : 0,
                    Size = model.Size.HasValue ? model.Size.Value : 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };
                return new ResponsePagination<WardDetailModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Wards Error", ex, model);
                Log.Error("Wards GetByFilter Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid Id)
        {
            try
            {
                var wardsRepo = _unit.GetRepository<Wards>();
                var checkExit = await wardsRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                var city = await _unit.GetRepository<Cities>().Get(x => x.Id == checkExit.CityId).Select(x => new CityModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    IsDeleted = x.IsDeleted
                }).AsNoTracking().FirstOrDefaultAsync();

                var res = new WardDetailModel()
                {
                    Id = checkExit.Id,
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    IsDelete = checkExit.IsDelete,
                    City = city
                };
                return new ResponseObject<WardDetailModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Wards Error", ex, Id);
                Log.Error("Wards GetById Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(UpdateWardModel model)
        {
            try
            {
                var wardsRepo = _unit.GetRepository<Wards>();
                var checkExit = await wardsRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                checkExit.Code = model.Code;
                checkExit.Name = model.Name;
                checkExit.IsDelete = model.IsDelete;
                checkExit.CityId = model.CityId;
                wardsRepo.Update(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Update DB Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Wards Error", ex, model);
                Log.Error("Wards Update Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
