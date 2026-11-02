namespace AnFood.Application.Bussiness.AuditLogger
{
    public interface IAuditLogger
    {
        Task LogCreateAsync(
            string entityName,
            string entityId,
            object? newData = null,
            string? userId = null,
            string? userName = null);

        Task LogUpdateAsync(
            string entityName,
            string entityId,
            object? oldData = null,
            object? newData = null,
            string? userId = null,
            string? userName = null);

        Task LogDeleteAsync(
            string entityName,
            string entityId,
            object? oldData = null,
            string? userId = null,
            string? userName = null);

        Task LogOtherAsync(
            string actionName,
            string? entityName = null,
            string? entityId = null,
            object? details = null,
            string? userId = null,
            string? userName = null);
    }

}
