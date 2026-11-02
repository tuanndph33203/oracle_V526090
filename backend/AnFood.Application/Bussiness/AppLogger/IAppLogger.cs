namespace AnFood.Application.Bussiness.AppLogger
{
    public interface IAppLogger
    {
        Task ErrorAsync(string message, Exception ex, object? data = null);
        Task InfoAsync(string message, object? data = null);
    }
}
