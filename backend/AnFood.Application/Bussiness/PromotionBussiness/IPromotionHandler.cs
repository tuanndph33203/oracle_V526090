using System;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.PromotionBussiness
{
    public interface IPromotionHandler
    {
        Task<Response> Create(PromotionCreateModel model);
        Task<Response> Update(PromotionUpdateModel model);
        Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName);
        Task<Response> GetById(Guid id);
        Task<Response> GetByFilter(PromotionFilterModel model);
    }
}
