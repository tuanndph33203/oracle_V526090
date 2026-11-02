using AnFood.Application.Bussiness.AppLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.UnitsBussiness
{
    public class UnitHandler : IUnitHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public UnitHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit=unit;
        }
        public async Task<Response> Create(CreateUnitModel model)
        {
            try
            {
              
                    var unitRepo = _unit.GetRepository<Unit>();
                    var checkExitCode = await unitRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                    if (checkExitCode != null)
                    {
                        return new Response(Code.Conflict, "Code Exit");
                    }
                    Unit item = new Unit()
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code,
                        Name = model.Name,
                        Status = 1,
                        CreatedOnDate = DateTime.Now,
                        CreatedByUserName = model.CreatedByUserName,
                        CreatedByUserCode = model.CreatedByUserId
                    };
                    unitRepo.Add(item);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Save Unit Error");
                    }
                    return new Response(Code.Success, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Unit Error", ex, model);
                Log.Error("Unit Create Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id)
        {
            try
            {
                
                    var unitRepo = _unit.GetRepository<Unit>();
                    var checkExit = await unitRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }
                    unitRepo.Delete(checkExit);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Delete Unit Error");
                    }
                    return new Response(Code.Success, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Unit Error", ex, Id);
                Log.Error("Unit Delete Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(UnitFilterModel model)
        {
            try
            {
                
                    var unitRepo = _unit.GetRepository<Unit>();
                    var query = unitRepo.GetAll();
                    
                    if (!string.IsNullOrEmpty(model.TextSearch))
                    {
                        var textLower = model.TextSearch.ToLower().Trim();
                        query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                    }
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
                    var totalCount = query.Count();
                    query = query.OrderByDescending(x => x.CreatedOnDate);
                    query = query.Skip(excludedRows).Take((int)model.Size);
                    var datas = await query.Select(x => new UnitModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        Status = x.Status
                    }).ToListAsync();

                    var response = new Pagination<UnitModel>();
                    response.Content = datas;
                    response.NumberOfElements = datas.Count();
                    response.Page = model.Page.HasValue ? model.Page.Value : 0;
                    response.Size = model.Size.HasValue ? model.Size.Value : 0;
                    response.TotalElements = totalCount;
                    response.TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1));
                    return new ResponsePagination<UnitModel>(response);
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Unit Error", ex, model);
                Log.Error("Unit GetByFilter Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid Id)
        {
            try
            {
                
                    var unitRepo = _unit.GetRepository<Unit>();
                    var checkExit = await unitRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }

                    var res = new UnitModel()
                    {

                        Id = checkExit.Id,
                        Name = checkExit.Name,
                        Code = checkExit.Code,
                        Status = checkExit.Status
                    };
                    return new ResponseObject<UnitModel>(res, "Success");
               
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Unit Error", ex, Id);
                Log.Error("Unit GetById Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(UpdateUnitModel model)
        {
            try
            {
               
                    var unitRepo = _unit.GetRepository<Unit>();
                    var checkExit = await unitRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }

                    checkExit.Code = model.Code;
                    checkExit.Name = model.Name;
                    checkExit.Status = model.Status;
                    unitRepo.Update(checkExit);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Update DB Error");
                    }
                    return new Response(Code.Success, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Unit Error", ex, model);
                Log.Error("Unit Update Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
