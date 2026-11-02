
using AnFood.Application.Bussiness.LogsBusiness;
using Microsoft.Extensions.Configuration;

namespace AnFood.Application.Bussiness.AuditLogger
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IConfiguration _config;
        private readonly ILogsHandler _logsHandler;

        public AuditLogger(IConfiguration config, ILogsHandler logsHandler)
        {
            _config = config;
            _logsHandler = logsHandler;
        }

        public Task LogCreateAsync(
            string entityName,
            string entityId,
            object? newData,
            string? userId,
            string? userName)
        {
            return SendAsync("Create", entityName, entityId, newData, userId, userName);
        }

        public Task LogUpdateAsync(
            string entityName,
            string entityId,
            object? oldData,
            object? newData,
            string? userId,
            string? userName)
        {
            return SendAsync(
                "Update",
                entityName,
                entityId,
                new AuditUpdateDetail
                {
                    OldData = oldData,
                    NewData = newData
                },
                userId,
                userName);
        }

        public Task LogDeleteAsync(
            string entityName,
            string entityId,
            object? oldData,
            string? userId,
            string? userName)
        {
            return SendAsync("Delete", entityName, entityId, oldData, userId, userName);
        }

        public Task LogOtherAsync(
            string actionName,
            string? entityName,
            string? entityId,
            object? details,
            string? userId,
            string? userName)
        {
            return SendAsync(actionName, entityName, entityId, details, userId, userName);
        }

        private async Task SendAsync(
            string actionName,
            string? entityName,
            string? entityId,
            object? details,
            string? userId,
            string? userName)
        {
            var model = new SystemAuditLogModel
            {
                ActionName = actionName,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                UserId = userId,
                UserName = userName
            };

            await _logsHandler.CreateAuditLog(model);
        }
    }

}
