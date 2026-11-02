
using AnFood.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.SystemSettingBusiness
{
    public interface ISystemSettingHandler
    {
        public Task<Response> Create(CreateSystemSettingCommand model, CancellationToken ct);
        public Task<Response> Update(UpdateSystemSettingCommand model, CancellationToken ct);
        public Task<Response> UpdateMultiple(UpdateMultipleSystemSettingCommand model, CancellationToken ct);
        public Task<Response> Delete(Guid Id, CancellationToken ct);
        public Task<Response> GetByFilter(CancellationToken ct);
    }
}
