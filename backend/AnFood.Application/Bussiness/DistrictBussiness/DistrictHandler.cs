using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.DistrictBussiness
{
    public class DistrictHandler : IDistrictHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public DistrictHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit=unit;
        }
        public async Task<Response> Create(DistrictCreateModel model)
        {
            try
            {
                var districtRepo = _unit.GetRepository<Districts>();
                var checkExitCode = await districtRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                if (checkExitCode != null)
                {
                    return new Response(Code.Conflict, "Code Exit");
                }
                Districts item = new Districts()
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name,
                    IsDelete = false,
                    CityId = model.CityId
                };
                districtRepo.Add(item);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save Districts Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Districts Error", ex, model);
                Log.Error("Districts Create Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id)
        {
            try
            {
                var districtRepo = _unit.GetRepository<Districts>();
                var checkExit = await districtRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }
                districtRepo.Delete(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Delete Districts Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Districts Error", ex, Id);
                Log.Error("City Districts Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(DistrictFilterModel model)
        {
            try
            {
                var districtRepo = _unit.GetRepository<Districts>();
                var query = districtRepo.GetAll().AsNoTracking();
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
                    select new DistrictDetailModel
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

                var response = new Pagination<DistrictDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page.HasValue ? model.Page.Value : 0,
                    Size = model.Size.HasValue ? model.Size.Value : 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };
                return new ResponsePagination<DistrictDetailModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Districts Error", ex, model);
                Log.Error("Districts GetByFilter Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid Id)
        {
            try
            {
                var districtRepo = _unit.GetRepository<Districts>();
                var checkExit = await districtRepo.Get(x => x.Id == Id).AsNoTracking().FirstOrDefaultAsync();
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

                var res = new DistrictDetailModel()
                {
                    Id = checkExit.Id,
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    IsDelete = checkExit.IsDelete,
                    City = city
                };
                return new ResponseObject<DistrictDetailModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Districts Error", ex, Id);
                Log.Error("Districts GetById Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(DistrictUpdateModel model)
        {
            try
            {
                var districtRepo = _unit.GetRepository<Districts>();
                var checkExit = await districtRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                checkExit.Code = model.Code;
                checkExit.Name = model.Name;
                checkExit.IsDelete = model.IsDelete;
                checkExit.CityId = model.CityId;
                districtRepo.Update(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Update DB Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Districts Error", ex, model);
                Log.Error("Districts Update Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
