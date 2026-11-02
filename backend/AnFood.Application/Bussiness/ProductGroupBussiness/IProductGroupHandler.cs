using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ProductGroupBussiness
{
    public interface IProductGroupHandler
    {
        public Task<Response> Create(CreateProductGroupModel model);
        public Task<Response> GetByFilter(ProductGroupFilterModel model);
        public Task<Response> Update(UpdateProductGroupModel model);
        public Task<Response> Delete(Guid Id);
        public Task<Response> GetById(Guid Id);
    }
}
