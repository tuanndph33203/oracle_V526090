# AnPos Backend - Copilot Instructions

This document provides essential context for working effectively in the AnPos POS (Point of Sale) backend codebase.

## Project Overview

AnPos is a .NET 8.0 POS system for spa/salon businesses with two main APIs:
- **MA.Core.API** (AnPos.API) - Main business API
- **Anpos.Mobile.API** - Mobile client API

## Architecture

### Layered Architecture

The solution follows a clean layered architecture:

```
MA.Core.API (AnPos.API)        ← ASP.NET Core Web API layer
    ↓
Anpos.Business                 ← Business logic & handlers
    ↓
Anpos.Data                     ← Data access & EF Core DbContext
    ↓
Anpos.Domain                   ← Domain entities & DTOs
```

**Supporting libraries:**
- `src/BuildingBlocks/Utils/` - Shared utilities (ApiUtils, CommonUtils, DataUtils)
- `src/BuildingBlocks/EventBus/` - Event-driven architecture components

### Key Conventions

#### Naming Patterns

- **Handlers**: Business logic classes end with `Handler` and implement `I{Name}Handler` interfaces
  - Example: `IReportExportHandler` / `ReportExportHandler`
  - Registered as scoped services in `ProjectServiceCollectionExtensions.cs`

- **Spelling variation**: The codebase uses both "Business" and "Bussiness" (with double 's')
  - Folder: `Anpos.Business/Bussiness/`
  - Namespace: `Anpos.Business.Bussiness.*`
  - **Important**: When creating new files in this layer, follow the existing "Bussiness" spelling to maintain consistency

- **Controllers**: Organized by feature area
  - Base path: `MA.Core.API/Controllers/`
  - Subdirectories: `Sale/`, `PaymentCard/`, `NoName/`, `Reports/`, `PromotionsCtl/`, `Accounting/`
  - Namespace pattern: `AnPos.API.Controllers` or `AnPos.API.Controllers.{Area}`

#### Data Access Patterns

- **Stored Procedures**: The application uses SQL Server stored procedures for complex queries
  - Convention: `sp_{Feature}_{Operation}` (e.g., `sp_SaleDetailRevenue_Export`, `sp_SaleDetailRevenue2_Export`)
  - Called via ADO.NET SqlCommand with `CommandType.StoredProcedure`
  - Timeout set to 300 seconds for long-running export operations

- **EF Core DbContext**: `DataContext` in `Anpos.Data.Infrastructure`
  - Registered in `CustomServiceCollectionExtensions.cs`
  - Connection string from `appsettings.json`

#### Export File Patterns

When implementing file export features (Excel, TXT, CSV):

- Support multiple formats via `FileType` parameter in request model (e.g., "xlsx", "xls", "txt")
- Use `ClosedXML.Excel` for Excel generation
- Text exports use UTF-8 encoding with semicolon (`;`) delimiters
- Sanitize text output: replace `\r`, `\n`, `;` to prevent format corruption
- File naming: `{BaseName}_{yyyyMMddHHmmss}.{ext}`
- Headers and column mappings defined as static arrays in handlers
- Example: `SaleDetailRevenueHeaders` + `SaleDetailRevenueColumnNames`

#### Background Jobs

- Background services in `MA.Core.API/BackgroundServices/`
- Registered as hosted services in `Program.cs`:
  ```csharp
  builder.Services.AddHostedService<JobCustomerLevelDaily>();
  builder.Services.AddHostedService<CustomerPointSyncService>();
  builder.Services.AddHostedService<ExportJobWorker>();
  ```
- Export jobs use `ExportJob` entity with status tracking: "Pending" → "Done"/"Failed"

## Build & Run

### Build

```bash
# Build entire solution
dotnet build AnPos.API.sln

# Build specific project
dotnet build MA.Core.API\AnPos.API.csproj
```

### Run

```bash
# Run main API (development mode)
dotnet run --project MA.Core.API\AnPos.API.csproj

# The API will be available at https://localhost:9500 (check appsettings.json for port)
```

### Docker

```bash
# Build Docker image
docker build -t anpos-api:latest -f Dockerfile .

# Run container
docker run -p 8080:8080 -p 8081:8081 anpos-api:latest
```

## Testing

The solution has minimal test coverage. Existing tests:
- `src/BuildingBlocks/EventBus/EventBus.Tests/` - Event bus unit tests

To run tests:
```bash
dotnet test
```

## Dependency Injection

All business handlers are registered in `MA.Core.API/Extensions/ProjectServiceCollectionExtensions.cs` using the scoped lifetime. When adding new handlers:

1. Create interface and implementation in `Anpos.Business/Bussiness/{Feature}/`
2. Add registration in `ProjectServiceCollectionExtensions.AddProjectServices()`:
   ```csharp
   .AddScoped<IYourHandler, YourHandler>()
   ```
3. Inject into controllers via constructor

## Database

- **Database**: SQL Server
- **ORM**: Entity Framework Core 8.0
- **Migrations**: Located in `Anpos.Data/Migrations/`
- **Connection String**: Configured in `appsettings.json`

### Running Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project Anpos.Data --startup-project MA.Core.API

# Update database
dotnet ef database update --project Anpos.Data --startup-project MA.Core.API
```

## Logging

- **Framework**: Serilog
- **Configuration**: `appsettings.json` → `Serilog` section
- **Log files**: `Logs/app/log-{Date}.txt` (rolling daily, 14 day retention)
- **Custom logging**: Use `IAppLogger` or `IAuditLogger` services

## Common Features

### SignalR Hubs

- Location: `MA.Core.API/Hubs/`
- Example: Payment notifications hub at `/paymentHub`
- JWT authentication via query string (`access_token` parameter)

### File Storage

- **MinIO** integration for object storage
- Handler: `IMinioHandler` in `Anpos.Business.Bussiness.MinIoProcess`

### Caching

- Redis caching configured via `AddCacheSetup()` extension
- Package: `Microsoft.Extensions.Caching.StackExchangeRedis`

## API Conventions

- **Routing**: Attribute routing with area-based organization
  - Example: `[Route("api/report/export")]`
- **JSON**: PascalCase property names (configured in `Program.cs`)
- **CORS**: Allows all origins in development ("AllowFrontend" policy)
- **Authentication**: JWT Bearer tokens (JwtBearerDefaults.AuthenticationScheme)

## Vietnamese Language

This is a Vietnamese business application. When adding features:
- UI labels and error messages should be in Vietnamese
- Database column headers in exports use Vietnamese (e.g., "Ngày chứng từ", "Số chứng từ")
- Code comments and variable names use English
