using Business;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.IO;
using AnFood.API.Extensions;
using AnFood.API.JsonConverters;
using AnFood.API.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled"); 

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .SetIsOriginAllowed(_ => true) // Cho phép mọi origin
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins("https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
}); 

var configuration = builder.Configuration;
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

if (swaggerEnabled)
{
    builder.Services.AddSwaggerGen();
}

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Không cần nhập tiền tố Bearer."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCustomMVC(configuration);
builder.Services.AddCustomOptions(configuration);
builder.Services.AddProjectServices();

builder.Services.Configure<AnFood.Application.Bussiness.MinIoProcess.MinioOptions>(
    builder.Configuration.GetSection("Minio"));

builder.Services.AddSingleton<Minio.MinioClient>(sp =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnFood.Application.Bussiness.MinIoProcess.MinioOptions>>().Value;
    var client = new Minio.MinioClient()
        .WithEndpoint(opt.Endpoint)
        .WithCredentials(opt.AccessKey, opt.SecretKey);
    
    if (opt.UseSSL)
    {
        client.WithSSL();
    }
    
    return client.Build();
});

builder.Services.AddCustomRouting();
builder.Services.AddCors();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddCustomDbContext(configuration);
builder.Services.AddCacheSetup(configuration);
builder.Services.AddScoped<AnFood.API.Services.PrintAgentRegistry>();
builder.Services.AddScoped<AnFood.API.Services.KitchenPrintDispatcher>();

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddHostedService<AnFood.API.BackgroundServices.DailyMenuItemResetWorker>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // giữ nguyên PascalCase
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.Converters.Add(new CustomDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableCustomDateTimeJsonConverter());
    });

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
        ClockSkew = TimeSpan.FromHours(8)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].ToString();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrWhiteSpace(accessToken) && (path.StartsWithSegments("/paymentHub") || path.StartsWithSegments("/kitchenHub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AnFood.API.Hubs.PaymentHub>("/paymentHub");
app.MapHub<AnFood.API.Hubs.KitchenHub>("/kitchenHub");
app.MapHub<AnFood.API.Hubs.PrintAgentHub>("/printAgentHub");

app.Run();
