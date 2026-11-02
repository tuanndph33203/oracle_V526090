using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.UnitsBussiness
{
    public interface IUnitHandler
    {
        public Task<Response> Create(CreateUnitModel model);
        public Task<Response> GetByFilter(UnitFilterModel model);
        public Task<Response> Update(UpdateUnitModel model);
        public Task<Response> Delete(Guid Id);
        public Task<Response> GetById(Guid Id);
    }
}
