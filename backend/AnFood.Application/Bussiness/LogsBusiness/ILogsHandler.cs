using AnFood.Application.Bussiness.LogsBusiness;
using AnFood.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.LogsBusiness
{
    public interface ILogsHandler
    {
        public Task<Response> CreateAppLog(CreateAppLogRequest model);
        public Task<Response> CreateAuditLog(SystemAuditLogModel model);
    }

}
