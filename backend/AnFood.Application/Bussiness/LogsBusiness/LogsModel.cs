using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.LogsBusiness
{
    public class LogsModel
    {
    }
    public class SystemAuditLogModel
    {
        public string? ServiceName { get; set; } = "ANPOS";
        public string? Environment { get; set; } = "prod";  // dev / uat / prod
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActionName { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public object? Details { get; set; }
    }
    public class CreateAppLogRequest
    {
        public string? ServiceName { get; set; } = "ANPOS";
        public string? Environment { get; set; } = "prod";  // dev / uat / prod
        public string Level { get; set; } = "Info";   // Info / Warn / Error / Debug
        public string? LogType { get; set; }
        public string? RequestId { get; set; }
        public string Message { get; set; } = "Logs System";
        public string? Exception { get; set; }
        public object? Data { get; set; }                  // s? serialize th�nh JSON
    }

}
