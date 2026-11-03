using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnFood.API.Extensions
{
    public static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddAppHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            var hangfireCs = configuration["ConnectionStringHangFire"];
            if (string.IsNullOrWhiteSpace(hangfireCs))
            {
                var dbType = configuration["ConnectionString:DbType"];
                if (!string.IsNullOrWhiteSpace(dbType))
                    hangfireCs = configuration[$"ConnectionString:{dbType}"];
            }

            if (string.IsNullOrWhiteSpace(hangfireCs))
                throw new InvalidOperationException(
                    "Hangfire: thiết lập ConnectionStringHangFire hoặc ConnectionString:DbType + connection tương ứng.");

            services.AddHangfire(global => global
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(hangfireCs, new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                }));

            services.AddHangfireServer(options =>
            {
                options.Queues = new[] { "default", "misa-invoice" };
                options.WorkerCount = Math.Min(Math.Max(Environment.ProcessorCount, 2), 20);
            });

            return services;
        }
    }
}
