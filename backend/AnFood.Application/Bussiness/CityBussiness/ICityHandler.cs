using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.CityBussiness
{
    public interface ICityHandler
    {
        public Task<Response> Create (CityCreateModel model);
        public Task<Response> GetByFilter(CityFilterModel model);
        public Task<Response> Update (CityUpdateModel model);
        public Task<Response> Delete(Guid Id);
        public Task<Response> GetById (Guid Id);
    }
}
