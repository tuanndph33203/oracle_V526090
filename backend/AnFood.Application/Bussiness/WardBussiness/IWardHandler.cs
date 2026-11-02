using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.WardBussiness
{
    public interface IWardHandler
    {
        public Task<Response> Create(CreateWardModel model);
        public Task<Response> GetByFilter(WardFilterModel model);
        public Task<Response> Update(UpdateWardModel model);
        public Task<Response> Delete(Guid Id);
        public Task<Response> GetById(Guid Id);
    }
}
