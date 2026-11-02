using System;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.MenuItemBussiness;
using AnFood.Persistence;
using AnFood.Domain.Datatables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnFood.API.BackgroundServices
{
    public class DailyMenuItemResetWorker : BackgroundService
    {
        private const string SettingCode = "DAILY_MENU_ITEM_RESET_DATE";
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyMenuItemResetWorker> _logger;

        public DailyMenuItemResetWorker(IServiceProvider serviceProvider, ILogger<DailyMenuItemResetWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private static DateTime GetVietnamNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch
                {
                    return DateTime.UtcNow.AddHours(7);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily MenuItem Reset Background Worker started.");

            // Brief initial delay to ensure DB and services are ready
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            string? lastResetDateStr = null;
            try
            {
                lastResetDateStr = await GetLastResetDateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load initial reset date from database.");
            }

            var startupVnNow = GetVietnamNow();
            var startupTodayStr = startupVnNow.ToString("yyyy-MM-dd");

            if (string.IsNullOrEmpty(lastResetDateStr))
            {
                // First run / setting uninitialized: record today's date so we DO NOT wipe stock in the middle of the day
                _logger.LogInformation("Daily MenuItem Reset: Initializing setting to today ({Date}) to prevent mid-day reset.", startupTodayStr);
                await SaveLastResetDateAsync(startupTodayStr);
                lastResetDateStr = startupTodayStr;
            }
            else if (lastResetDateStr != startupTodayStr)
            {
                // Server was stopped overnight and started on a new day: run reset for the new day
                _logger.LogInformation("Daily MenuItem Reset: New day detected on startup (Last: {Last}, Today: {Today}). Executing reset...", lastResetDateStr, startupTodayStr);
                await ExecuteResetAsync();
                lastResetDateStr = startupTodayStr;
                await SaveLastResetDateAsync(startupTodayStr);
            }
            else
            {
                _logger.LogInformation("Daily MenuItem Reset: Today ({Today}) already reset. Waiting for next 00:00 midnight (Vietnam Time).", startupTodayStr);
            }

            // Periodic check every 10 seconds for exact 00:00 transition
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowVn = GetVietnamNow();
                    var todayStr = nowVn.ToString("yyyy-MM-dd");

                    if (todayStr != lastResetDateStr)
                    {
                        _logger.LogInformation("Daily MenuItem Reset: Midnight reached! Vietnam time: {Time}. Resetting stock and availability...", nowVn.ToString("yyyy-MM-dd HH:mm:ss"));
                        await ExecuteResetAsync();
                        lastResetDateStr = todayStr;
                        await SaveLastResetDateAsync(todayStr);
                        _logger.LogInformation("Daily MenuItem Reset: Completed successfully for date {Date}.", todayStr);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Daily MenuItem Reset worker loop.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Daily MenuItem Reset Background Worker stopped.");
        }

        private async Task ExecuteResetAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var menuItemHandler = scope.ServiceProvider.GetRequiredService<IMenuItemHandler>();
            await menuItemHandler.ResetAllAvailability();
        }

        private async Task<string?> GetLastResetDateAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var settingRepo = unit.GetRepository<SystemSetting>();
            var setting = await settingRepo.Get(x => x.Code == SettingCode).FirstOrDefaultAsync();
            return setting?.Value;
        }

        private async Task SaveLastResetDateAsync(string dateStr)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var settingRepo = unit.GetRepository<SystemSetting>();
                var setting = await settingRepo.Get(x => x.Code == SettingCode).FirstOrDefaultAsync();
                if (setting == null)
                {
                    setting = new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = SettingCode,
                        Name = "Ngày reset định mức món ăn gần nhất",
                        Value = dateStr,
                        DataType = "string",
                        Module = "catalog",
                        Description = "Lưu ngày (yyyy-MM-dd) đã thực hiện tự động reset định mức và hiển thị món ăn lúc 00:00",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    settingRepo.Add(setting);
                }
                else
                {
                    setting.Value = dateStr;
                    setting.UpdatedAt = DateTime.UtcNow;
                    settingRepo.Update(setting);
                }
                await unit.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save {Code} to SystemSetting.", SettingCode);
            }
        }
    }
}
