using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ProductBussiness
{
    public interface IProductHandler
    {
        public Task<Response> Create(CreateProductModel model);
        public Task<Response> GetByFilter(ProductFilterModel model);
        public Task<Response> Update(UpdateProductModel model);
        public Task<Response> Delete(Guid Id);
        public Task<byte[]> ExportExcelByIdsAsync(List<Guid> ids, CancellationToken ct);
    }
}
