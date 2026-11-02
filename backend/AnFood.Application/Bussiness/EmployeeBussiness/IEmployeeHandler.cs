using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.EmployeeBussiness
{
    public interface IEmployeeHandler
    {
        Task<Response> Create(CreateEmployeeModel model);
        Task<Response> GetByFilter(EmployeeFilterModel model);
        Task<Response> Update(UpdateEmployeeModel model);
        Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName);
        Task<ResponseObject<EmployeeDetailModel>> GetById(Guid id);
        Task<Response> CreateCode(string branchCode, CancellationToken ct);
        Task<ResponseObject<EmployeeDetailModel>> GetByCode(string code, CancellationToken ct);
    }
}
