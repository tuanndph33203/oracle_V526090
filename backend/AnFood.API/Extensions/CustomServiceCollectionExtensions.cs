using AnFood.Infrastructure;
using AnFood.Infrastructure.CacheService;
using AnFood.API.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.IO.Compression;
using System.Linq;
using AnFood.Persistence.Context;

namespace Business
{
    public static class CustomServiceCollectionExtensions
    {
        
        public static IServiceCollection AddCustomMVC(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
                options.EnableEndpointRouting = false;
            });
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                     builder => builder
                    .SetIsOriginAllowed(_ => true) // Cho phép mọi origin
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

            return services;
        }

        public static IServiceCollection AddCacheSetup(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConfiguration = new RedisConfiguration();
            configuration.GetSection("RedisConfiguration").Bind(redisConfiguration);
       
            services.AddSingleton(redisConfiguration);

            if (!redisConfiguration.Enabled)
            {
                return services;
            }
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration.ConnectionString));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfiguration.ConnectionString;
                options.InstanceName = redisConfiguration.InstanceName;
            });
            services.AddScoped<IRedisCacheService, RedisCacheService>();
            return services;
        }

        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var databaseType = configuration["ConnectionString:DbType"];
            if (string.IsNullOrWhiteSpace(databaseType))
            {
                databaseType = "SqlServerDatabase";
            }

            var connectionString = GetPrimaryConnectionString(configuration, databaseType);
            var connectionStringHangfire = configuration["ConnectionStringHangFire"];

            services.AddDbContext<DataContext>(options =>
            {
                if (databaseType == "PostgreSQLDatabase")
                    options.UseNpgsql(connectionString);
                else
                    options.UseSqlServer(connectionString);
            });

            services.AddDbContext<ReadOnlyDataContext>(options =>
            {
                var useReadOnlyRaw = configuration["ConnectionString:UseReadOnly"]
                                     ?? configuration["UseReadOnlyDatabase"]
                                     ?? configuration["UseReadOnlyDb"];

                var customReadOnlyConnStr = GetReadOnlyConnectionString(configuration, databaseType);
                bool useReadOnly = !string.IsNullOrWhiteSpace(useReadOnlyRaw) 
                    ? ParseBool(useReadOnlyRaw, false) 
                    : !string.IsNullOrWhiteSpace(customReadOnlyConnStr);

                string finalConnStr;
                if (useReadOnly)
                {
                    var baseConnStr = !string.IsNullOrWhiteSpace(customReadOnlyConnStr)
                        ? customReadOnlyConnStr
                        : GetPrimaryConnectionString(configuration, databaseType);

                    finalConnStr = databaseType == "PostgreSQLDatabase"
                        ? baseConnStr
                        : EnsureApplicationIntentReadOnly(baseConnStr);
                }
                else
                {
                    finalConnStr = GetPrimaryConnectionString(configuration, databaseType);
                }

                if (databaseType == "PostgreSQLDatabase")
                    options.UseNpgsql(finalConnStr);
                else
                    options.UseSqlServer(finalConnStr);

                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            return services;
        }

        private static string? GetReadOnlyConnectionString(IConfiguration configuration, string databaseType)
        {
            var connStr = configuration["ConnectionStringReadOnly"]
                          ?? configuration["ConnectionString:ReadOnly"]
                          ?? configuration["ConnectionString:ReadOnlyDatabase"]
                          ?? configuration["ConnectionString:SqlServerDatabaseReadOnly"]
                          ?? configuration["ConnectionString:PostgreSQLDatabaseReadOnly"]
                          ?? configuration.GetConnectionString("ConnectionStringReadOnly")
                          ?? configuration.GetConnectionString("ReadOnlyConnection")
                          ?? configuration.GetConnectionString("ReadOnlyDatabase");

            return string.IsNullOrWhiteSpace(connStr) ? null : connStr;
        }

        private static string GetPrimaryConnectionString(IConfiguration configuration, string databaseType)
        {
            var connStr = !string.IsNullOrWhiteSpace(databaseType)
                ? configuration["ConnectionString:" + databaseType]
                : null;

            if (string.IsNullOrWhiteSpace(connStr))
            {
                connStr = configuration["ConnectionString:SqlServerDatabase"]
                          ?? configuration["ConnectionString:PostgreSQLDatabase"]
                          ?? configuration.GetConnectionString("DefaultConnection")
                          ?? configuration["ConnectionString"];
            }

            return connStr ?? string.Empty;
        }

        private static bool ParseBool(string? value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var val = value.Trim().ToLowerInvariant();
            if (val == "true" || val == "1" || val == "yes" || val == "on")
                return true;

            if (val == "false" || val == "0" || val == "no" || val == "off")
                return false;

            return defaultValue;
        }

        private static string EnsureApplicationIntentReadOnly(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return string.Empty;

            if (!connectionString.Contains("ApplicationIntent", System.StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("Application Intent", System.StringComparison.OrdinalIgnoreCase))
            {
                return connectionString.TrimEnd(';') + ";Application Intent=ReadOnly;";
            }
            return connectionString;
        }

        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Please refer to the errors property for additional details."
                    };

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json", "application/problem+xml" }
                    };
                };
            });

            return services;
        }

       
        public static IServiceCollection AddCustomRouting(this IServiceCollection services) =>
            services.AddRouting(
                options =>
                {
                    // All generated URL's should be lower-case.
                    options.LowercaseUrls = true;
                });

        public static IServiceCollection AddCustomHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStringHangfire = configuration["ConnectionStringHangFire"];
            if (string.IsNullOrWhiteSpace(connectionStringHangfire))
            {
                var databaseType = configuration["ConnectionString:DbType"];
                connectionStringHangfire = configuration["ConnectionString:" + databaseType];
            }

            if (!string.IsNullOrWhiteSpace(connectionStringHangfire))
            {
                services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionStringHangfire, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

                services.AddHangfireServer();
            }

            return services;
        }
    }
}
