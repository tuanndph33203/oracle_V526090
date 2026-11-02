using AnFood.Application.Bussiness.AppLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.ProductGroupBussiness
{
    public class ProductGroupHandler : IProductGroupHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public ProductGroupHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit=unit;
        }
        public async Task<Response> Create(CreateProductGroupModel model)
        {
            try
            {
                
                    var productGroupRepo = _unit.GetRepository<ProductGroup>();
                    var checkExitCode = await productGroupRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                    if (checkExitCode != null)
                    {
                        return new Response(Code.Conflict, "Code Exit");
                    }
                    ProductGroup item = new ProductGroup()
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code,
                        Name = model.Name,
                        Status = 1,
                        Type = model.Type,
                        CreatedOnDate = DateTime.Now,
                        CreatedByUserName = model.CreatedByUserName,
                        CreatedByUserCode = model.CreatedByUserId
                    };
                    productGroupRepo.Add(item);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Save ProductGroup Error");
                    }
                    return new Response(Code.Success, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create ProductGroup Error", ex, model);
                Log.Error("ProductGroup Create Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id)
        {
            try
            {
               
                    var productGroupRepo = _unit.GetRepository<ProductGroup>();
                    var checkExit = await productGroupRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }
                    productGroupRepo.Delete(checkExit);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Delete ProductGroup Error");
                    }
                    return new Response(Code.Success, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete ProductGroup Error", ex, Id);
                Log.Error("ProductGroup Delete Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(ProductGroupFilterModel model)
        {
            try
            {
                
                    var productGroupRepo = _unit.GetRepository<ProductGroup>();
                    var query = productGroupRepo.GetAll();

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
                    var datas = await query.Select(x => new ProductGroupModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        Status = x.Status,
                        Type = x.Type
                    }).ToListAsync();

                    var response = new Pagination<ProductGroupModel>();
                    response.Content = datas;
                    response.NumberOfElements = datas.Count();
                    response.Page = model.Page.HasValue ? model.Page.Value : 0;
                    response.Size = model.Size.HasValue ? model.Size.Value : 0;
                    response.TotalElements = totalCount;
                    response.TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1));
                    return new ResponsePagination<ProductGroupModel>(response);
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter ProductGroup Error", ex, model);
                Log.Error("ProductGroup GetByFilter Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid Id)
        {
            try
            {
                
                    var productGroupRepo = _unit.GetRepository<ProductGroup>();
                    var checkExit = await productGroupRepo.Get(x => x.Id == Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }

                    var res = new ProductGroup()
                    {
                        Id = checkExit.Id,
                        Code = checkExit.Code,
                        Name = checkExit.Name,
                        Status = checkExit.Status,
                        Type = checkExit.Type
                    };
                    return new ResponseObject<ProductGroup>(res, "Success");
                
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById ProductGroup Error", ex, Id);
                Log.Error("ProductGroup GetById Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(UpdateProductGroupModel model)
        {
            try
            {
               
                    var productGroupRepo = _unit.GetRepository<ProductGroup>();
                    var checkExit = await productGroupRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (checkExit == null)
                    {
                        return new Response(Code.NotFound, "Id Not Found");
                    }

                    checkExit.Code = model.Code;
                    checkExit.Name = model.Name;
                    checkExit.Status = model.Status;
                    checkExit.Type = model.Type;

                    productGroupRepo.Update(checkExit);
                    if (await _unit.SaveAsync() < 0)
                    {
                        return new Response(Code.ServerError, "Update DB Error");
                    }
                    return new Response(Code.Success, "Success");
               
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update ProductGroup Error", ex, model);
                Log.Error("ProductGroup Update Catch: " + DateTime.Now.ToString() + ": " + ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}
