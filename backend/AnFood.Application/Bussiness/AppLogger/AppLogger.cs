
using AnFood.Application.Bussiness.LogsBusiness;
namespace AnFood.Application.Bussiness.AppLogger
{
    public class AppLogger : IAppLogger
    {
        private readonly ILogsHandler _logsService;

        public AppLogger(ILogsHandler logsService)
        {
            _logsService = logsService;
        }

        public Task ErrorAsync(string message, Exception ex, object? data = null)
        {
            return _logsService.CreateAppLog(new CreateAppLogRequest
            {
                Level = "Error",
                LogType = "System",
                Message = message + ": " + DateTime.Now.ToString(),
                Exception = ex?.ToString(),
                Data = data
            });
        }

        public Task InfoAsync(string message, object? data = null)
        {
            return _logsService.CreateAppLog(new CreateAppLogRequest
            {
                Level = "Information",
                LogType = "System",
                Message = message + ": " + DateTime.Now.ToString(),
                Data = data
            });
        }
    }
}
