using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.CityBussiness
{
    public class CityHandler : ICityHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public CityHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit=unit;
        }
        public async Task<Response> Create(CityCreateModel model)
        {
            try
            {
                var cityRepo = _unit.GetRepository<Cities>();
                var checkExitCode = await cityRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                if (checkExitCode != null)
                {
                    return new Response(Code.Conflict, "Code Exit");
                }
                Cities item = new Cities()
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name,
                    IsDeleted = false
                };
                cityRepo.Add(item);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save City Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch(Exception ex)
            {
                await _logsapp.ErrorAsync("Create City Error", ex, model);
                Log.Error("City Create Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id)
        {
            try
            {
                var cityRepo = _unit.GetRepository<Cities>();
                var checkExit = await cityRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }
                cityRepo.Delete(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Delete City Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete City Error", ex, Id);
                Log.Error("City Delete Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(CityFilterModel model)
        {
            try
            {
                var cityRepo = _unit.GetRepository<Cities>();
                var query = cityRepo.GetAll();
                if (model.IsGetDeleted.HasValue)
                {
                    query = query.Where(x => x.IsDeleted == model.IsGetDeleted.Value);
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
                var datas = await query.Select(x => new CityModel()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    IsDeleted = x.IsDeleted
                }).ToListAsync();

                var response = new Pagination<CityModel>();
                response.Content = datas;
                response.NumberOfElements = datas.Count();
                response.Page = model.Page.HasValue ? model.Page.Value : 0;
                response.Size = model.Size.HasValue ? model.Size.Value : 0;
                response.TotalElements = totalCount;
                response.TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1));
                return new ResponsePagination<CityModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter City Error", ex, model);
                Log.Error("City GetByFilter Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public  async Task<Response> GetById(Guid Id)
        {
            try
            {
                var cityRepo = _unit.GetRepository<Cities>();
                var checkExit = await cityRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                var res = new CityModel()
                {
                    Id = checkExit.Id,
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    IsDeleted = checkExit.IsDeleted
                };
                return new ResponseObject<CityModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById City Error", ex, Id);
                Log.Error("City GetById Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(CityUpdateModel model)
        {
            try
            {
                var cityRepo = _unit.GetRepository<Cities>();
                var checkExit = await cityRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                checkExit.Code = model.Code;
                checkExit.Name = model.Name;
                checkExit.IsDeleted = model.IsDeleted.Value;
                cityRepo.Update(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Update DB Error");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update City Error", ex, model);
                Log.Error("City Update Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
