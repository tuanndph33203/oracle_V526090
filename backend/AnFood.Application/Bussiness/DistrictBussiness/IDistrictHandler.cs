using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.DistrictBussiness
{
    public interface IDistrictHandler
    {
        public Task<Response> Create(DistrictCreateModel model);
        public Task<Response> GetByFilter(DistrictFilterModel model);
        public Task<Response> Update(DistrictUpdateModel model);
        public Task<Response> Delete(Guid Id);
        public Task<Response> GetById(Guid Id);
    }
}
