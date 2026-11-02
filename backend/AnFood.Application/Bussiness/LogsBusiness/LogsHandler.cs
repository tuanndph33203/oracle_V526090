using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnFood.Domain.Datatables.LogsSystem;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

namespace AnFood.Application.Bussiness.LogsBusiness
{
    public class LogsHandler : ILogsHandler
    {
        private readonly IUnitOfWork _unit;
        public LogsHandler(IUnitOfWork config)
        {
            _unit = config;
        }

        public async Task<Response> CreateAppLog(CreateAppLogRequest model)
        {
            try
            {
                var repo = _unit.GetRepository<SystemAppLog>();

                var entity = new SystemAppLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    ServiceName = model.ServiceName ?? "ANPOS",
                    Environment = model.Environment ?? "prod",
                    Level = model.Level,
                    LogType = model.LogType,
                    RequestId = model.RequestId,
                    Message = model.Message,
                    Exception = model.Exception,
                    DataJson = model.Data != null
                        ? JsonSerializer.Serialize(model.Data)
                        : null
                };

                repo.Add(entity);

                await _unit.SaveAsync();

                return new Response(Code.Success, "Log created");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }

        public async Task<Response> CreateAuditLog(SystemAuditLogModel model)
        {
            try
            {
                var entity = new SystemAuditLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    ServiceName = model.ServiceName ?? "ANPOS",
                    Environment = model.Environment ?? "prod",
                    UserId = model.UserId,
                    UserName = model.UserName,
                    ActionName = model.ActionName,
                    EntityName = model.EntityName,
                    EntityId = model.EntityId,
                    IpAddress = model.IpAddress,
                    UserAgent = model.UserAgent,
                    DetailsJson = model.Details != null
                        ? JsonSerializer.Serialize(model.Details)
                        : null
                };
                _unit.GetRepository<SystemAuditLog>().Add(entity);

                await _unit.SaveAsync();

                return new Response(Code.Success, "Log created");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }
    }
}
