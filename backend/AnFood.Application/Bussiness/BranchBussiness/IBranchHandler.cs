using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.BranchBussiness
{
    public interface IBranchHandler
    {
        Task<Response> Create(BranchCreateModel model);
        Task<Response> GetByFilter(BranchFilterModel model);
        Task<Response> Update(BranchUpdateModel model);
        Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName);
        Task<Response> GetById(Guid id);
    }
}
