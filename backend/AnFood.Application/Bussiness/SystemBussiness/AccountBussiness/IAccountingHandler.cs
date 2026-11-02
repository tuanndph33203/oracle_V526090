using AnFood.Application.Contant;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.SystemBussiness.AccountBussiness
{
    public interface IAccountingHandler
    {
        public Task<Response> GetAll(FilterModel model,CancellationToken ct);
        public Task<Response> Update(UpdateAccountModel model, CancellationToken ct);
        public Task<Response> UpdateMutiple(List<UpdateAccountModel> models, CancellationToken ct);
        public Task<Response> Create(CreateAccountModel model, CancellationToken ct);
        public Task<Response> Delete(string Id, CancellationToken ct);
        public Task<Response> GetById(string Id, CancellationToken ct);

    }
}
