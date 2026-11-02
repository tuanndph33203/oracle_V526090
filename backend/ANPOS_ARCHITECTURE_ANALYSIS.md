# Tài liệu phân tích kiến trúc dự án AnPos.API

## 1. Tổng quan dự án

- **Tên dự án:** `AnPos.API.sln`
- **Mục đích hệ thống:** Cung cấp backend API cho hệ thống quản lý bán hàng/dịch vụ, thẻ thanh toán nội bộ, tích điểm khách hàng, báo cáo doanh thu và tích hợp thanh toán QR.
- **Domain nghiệp vụ chính:** POS, Membership/Card, Customer Loyalty (điểm/vip), Payment, Reporting.
- **Các runtime API chính:**
  - `MA.Core.API` (API nghiệp vụ lõi)
  - `Anpos.Mobile.API` (API cho client mobile)

## 2. Công nghệ sử dụng

- **Nền tảng:** ASP.NET Core (`net8.0`)
- **ORM:** Entity Framework Core 8 (`Microsoft.EntityFrameworkCore` + `SqlServer`/`Npgsql`)
- **Database:** SQL Server (đang cấu hình `ConnectionString:DbType = SqlServerDatabase`), có hỗ trợ PostgreSQL qua switch cấu hình.
- **Thư viện chính:**
  - `Serilog` (logging)
  - `Swashbuckle.AspNetCore` (Swagger)
  - `StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis` (cache)
  - `Minio` (object storage)
  - `ClosedXML` (xuất Excel, tại Business layer)
  - `EFCore.BulkExtensions` (bulk thao tác dữ liệu trong generic repository)
- **Authentication:**
  - JWT Bearer trong `MA.Core.API` (validate issuer/audience/signing key)
  - Có xử lý token qua query string cho SignalR hub `/paymentHub`
  - Quản lý account/login thực tế tích hợp với dịch vụ SSO ngoài (`AuthentService:Url`)

## 3. Kiến trúc hệ thống

- **Mô hình kiến trúc:** Layered Architecture (API → Business → Data → Domain), dùng thêm Building Blocks dùng chung.
- **Tổ chức solution theo lớp:**
  - `MA.Core.API` / `Anpos.Mobile.API`: presentation layer (controller, middleware, host, background services, SignalR hub).
  - `Anpos.Business`: business logic (các `*Handler` theo module nghiệp vụ).
  - `Anpos.Data`: EF `DataContext`, migrations, persistence.
  - `Anpos.Domain`: entity/model domain (`Datatables`, `Infrastructure`).
  - `src/BuildingBlocks`: tiện ích chung (`ApiUtils`, `CommonUtils`, `DataUtils`, `EventBus`).

### Giải thích theo layer

- **Controller layer**
  - Nhận request, bind model, set metadata từ claims (`CreatedByUserId`, `CreatedByUserName`), gọi handler tương ứng.
  - Logic nghiệp vụ nặng chủ yếu nằm ở Business, controller thiên về orchestration.
- **Service/Business layer**
  - Được triển khai theo pattern `I...Handler` / `...Handler`.
  - Chứa validation nghiệp vụ, quy tắc domain, gọi external service (Payment Gateway, SSO), xử lý transaction/report.
- **Repository layer**
  - Dùng `IUnitOfWork` + generic `IRepository<T>` trong `DataUtils`.
  - Các handler thao tác DB qua `_unit.GetRepository<TEntity>()`.
  - Save tập trung qua `_unit.SaveAsync()`.
- **Domain/Entity layer**
  - Entity nằm trong `Anpos.Domain.Datatables.*`.
  - `DataContext` map `DbSet<>` rất nhiều bảng domain (sale, customer, card, report, payment...).

### Dependency Injection

- DI được cấu hình qua extension:
  - `AddProjectServices()` đăng ký hàng loạt handler theo `AddScoped`.
  - `AddCustomDbContext()` đăng ký `DataContext`.
  - `AddCacheSetup()` đăng ký Redis cache service.
  - `AddHttpContextAccessor()` phục vụ truy xuất token/claims trong business handler.
- `IPaymentTransactionHandler` được đăng ký theo decorator:
  - `PaymentTransactionHandler` (logic chính)
  - `PaymentTransactionHandlerRealtime` (push SignalR realtime sau khi update trạng thái).

### Middleware / HTTP pipeline

- **`MA.Core.API`**
  - `UseCors("AllowFrontend")`
  - `UseSwagger()` + `UseSwaggerUI()`
  - `UseCors("CorsPolicy")`
  - `UseAuthentication()`
  - `UseAuthorization()`
  - `MapControllers()`
  - `MapHub<PaymentHub>("/paymentHub")`
- **`Anpos.Mobile.API`**
  - `UseCors("AllowFrontend")`
  - `UseSwagger()` + `UseSwaggerUI()`
  - `UseAuthorization()`
  - `MapControllers()`

## 4. Cấu trúc thư mục

```text
/MA.Core.API
  /Controllers
  /Extensions
  /BackgroundServices
  /Hubs
  /Handlers
  Program.cs

/Anpos.Mobile.API
  /Controllers
  /Extensions
  Program.cs

/Anpos.Business
  /Bussiness/** (handlers theo module)
  /App/** (logic phục vụ mobile/client)
  /Helper

/Anpos.Data
  /Infrastructure (DataContext)
  /Migrations

/Anpos.Domain
  /Datatables/** (entity domain)
  /Infrastructure/** (base model)

/src/BuildingBlocks
  /Utils/ApiUtils
  /Utils/CommonUtils
  /Utils/DataUtils (UnitOfWork/Repository)
  /EventBus
```

- **Vai trò chính:**
  - `/Controllers`: expose REST endpoint.
  - `/Bussiness` hoặc `/App`: xử lý nghiệp vụ/use-case.
  - `/Infrastructure`, `/Migrations`: persistence + schema evolution.
  - `/Datatables`: domain entities.
  - `/Hubs`: realtime channel (SignalR).
  - `/BackgroundServices`: job chạy nền theo lịch hoặc queue DB.

## 5. Luồng xử lý chính (Main Flow)

### 5.1 Luồng request chuẩn API → DB

1. Client gọi endpoint controller.
2. Controller bind request model, lấy thông tin người dùng từ JWT claims (nếu có).
3. Controller gọi business handler (`I...Handler`).
4. Handler validate nghiệp vụ + gọi `IUnitOfWork.GetRepository<T>()`.
5. Repository thao tác entity qua EF Core.
6. Handler gọi `_unit.SaveAsync()` để commit.
7. Controller trả `Response`/`ResponseObject`/`ResponsePagination`.

### 5.2 Luồng Payment Transaction + Realtime (QR)

1. Client gọi `POST /api/PaymentTransactions/create-transaction`.
2. `PaymentTransactionHandler`:
   - Validate input (`TransactionId`, `BranchCode`, `AmountDue`).
   - Kiểm tra setting `sale-open-qr`.
   - Gọi Payment Gateway tạo QR (`PaymentGateway:BaseUrl`, `QrInvoicesEndpoint`, `XKey`).
   - Lưu bản ghi `PaymentHistories` (status ban đầu = 1).
3. Payment Gateway callback `POST /api/PaymentTransactions/webhook` (có `X-API-Key`).
4. Handler cập nhật `PaymentHistories` (`AmountPaid`, `Status = 2`, `Type = 1`).
5. Decorator `PaymentTransactionHandlerRealtime` đọc before/after status; nếu status đổi thì push SignalR event `TransactionUpdated` đến group `transaction:{transactionId}`.
6. Client nhận realtime qua hub `/paymentHub`.

### 5.3 Luồng SaleBill (điển hình nghiệp vụ bán)

1. Client gọi `POST /api/SaleBill`.
2. Controller enrich thông tin user từ claims.
3. `SaleBillHandler.Create` thực hiện:
   - Validate theo `SystemSetting` (membership card/customer card).
   - Đọc customer, card trống/card đã dùng, voucher khả dụng.
   - Tính document number, money term, validate card type/seri.
   - Tạo dữ liệu `SaleBill`, `SaleBillDetail`, `SaleBillPayment` và liên quan.
4. Save qua UnitOfWork.
5. Trả response thành công/thất bại theo rule business.

## 6. Authentication & Authorization

- **Cách login hoạt động:**
  - Không tìm thấy endpoint login trực tiếp trong source `MA.Core.API` hoặc `Anpos.Mobile.API`.
  - Quản lý account tích hợp qua dịch vụ ngoài `AuthentService` (SSO), ví dụ gọi `authentication/register`, `account/getall`, `account/getby-id`,...
- **Cách verify token:**
  - JWT Bearer configured tại `MA.Core.API/Program.cs`.
  - Validate `Issuer`, `Audience`, `Lifetime`, `IssuerSigningKey`.
- **SignalR token:**
  - Với hub `/paymentHub`, token được đọc từ query string `access_token`.
- **Refresh token:**
  - Không tìm thấy trong source.
- **Role/permission:**
  - Có helper đọc claim role (`ClaimTypes.Role`) và các claim custom (`userCode`, `applicationId`).
  - Nhiều controller có `[Authorize]` nhưng đang comment ở nhiều file (`// [Authorize]` hoặc `//[Authorize]`).
  - Endpoint webhook payment có `[AllowAnonymous]` + xác thực bằng header `X-API-Key`.

## 7. Database & Model

### Các bảng chính (theo `DataContext.DbSet`)

- **Danh mục hệ thống:** `Cities`, `Districts`, `Wards`, `Branchs`, `RoomGroups`, `Room`, `Service`, `Unit`, `Vat`,...
- **Khách hàng & loyalty:** `Customer`, `CustomerGroups`, `CustomerPoints`, `CustomerLevel`, `CustomerLevelPoint`, `VipType`, `VipPeriods`,...
- **Thẻ & thanh toán:** `PaymentCard`, `PaymentCardUpdate`, `PaymentCardPeriods`, `PaymentCardHistories`, `PaymentCardTransaction`, `PaymentCardPolicies`, `PaymentHistories`, `MoneyTerm`,...
- **Bán hàng:** `SaleBill`, `SaleBillDetail`, `SaleBillPayment`, `SaleService`, `SaleServiceDetail`, `SaleReturn`, `SaleGiftOrder`, `SaleVoucher`, `SaleDiscount`,...
- **Báo cáo/khác:** `ExportJobs`, `SystemAppLog`, `SystemAuditLog`, `Commissions`, `Warehouses`,...

### Quan hệ dữ liệu

- `DataContext.OnModelCreating` ép toàn bộ foreign key về `DeleteBehavior.Restrict`.
- Quan hệ chi tiết từng bảng không được cấu hình fluent rõ ràng tại một file mapping riêng trong source hiện đọc (phần lớn dựa theo convention/attribute entity).
- Có migration snapshot trong `Anpos.Data/Migrations` để phản ánh schema.

### ORM mapping

- EF Core code-first với `DbSet<>`.
- Override `SaveChanges/SaveChangesAsync` để normalize trường `DocumentDate` về `.Date`.
- Hỗ trợ chạy trên SQL Server và PostgreSQL tùy `ConnectionString:DbType`.

## 8. API chính

> Danh sách dưới đây là các API nổi bật theo nghiệp vụ chính và mức độ ảnh hưởng hệ thống.

### 8.1 Payment/Realtime

- `POST /api/PaymentTransactions/create-transaction`
  - **Chức năng:** Tạo giao dịch thanh toán QR và lưu `PaymentHistories`.
  - **Input:** `CreatePaymentTransactionModel`
  - **Output:** `ResponseObject<CreatePaymentTransactionResult>` (trả `QrImageUrl`, `TransactionId`, `AmountDue`)
- `POST /api/PaymentTransactions/webhook`
  - **Chức năng:** Nhận callback trạng thái thanh toán từ gateway.
  - **Input:** `PaymentGatewayWebhookModel` + header `X-API-Key`
  - **Output:** `Response`
- `GET /api/PaymentTransactions/status`
  - **Chức năng:** Lấy trạng thái theo `transactionId`.
  - **Input:** query `transactionId`
  - **Output:** `ResponseObject<PaymentHistories>`
- `PUT /api/PaymentTransactions/manual-approve`
  - **Chức năng:** Duyệt thủ công giao dịch.
  - **Input:** query `transactionId`
  - **Output:** `Response`

### 8.2 Sale

- `POST /api/SaleBill`
  - **Chức năng:** Tạo hóa đơn bán.
  - **Input:** `SaleBillCreateModel`
  - **Output:** `Response`
- `PUT /api/SaleBill`
  - **Chức năng:** Cập nhật hóa đơn.
  - **Input:** `SaleBillUpdateModel`
  - **Output:** `Response`
- `POST /api/SaleBill/getby-filter`
  - **Chức năng:** Lọc danh sách hóa đơn.
  - **Input:** `SaleBillFilterModel`
  - **Output:** `Response` (dữ liệu phân trang)
- `GET /api/SaleBill/getby-id`
  - **Chức năng:** Lấy chi tiết hóa đơn.
  - **Input:** query `Id`
  - **Output:** `Response`
- `POST /api/sales/sales-services/create`
  - **Chức năng:** Tạo giao dịch bán dịch vụ.
  - **Input:** `CreateSaleServicesModel`
  - **Output:** `Response`

### 8.3 Card/Loyalty

- `POST /api/PaymentCard/getby-filter`
  - **Chức năng:** Lọc thẻ thanh toán.
  - **Input:** filter model
  - **Output:** `Response`
- `POST /api/PaymentCard/get-empty-card`
  - **Chức năng:** Lấy danh sách thẻ trống để cấp phát.
  - **Input:** filter model
  - **Output:** `Response`
- `POST /api/PaymentCardTransaction/getby-filter`
  - **Chức năng:** Truy vấn giao dịch thẻ.
  - **Input:** filter model
  - **Output:** `Response`

### 8.4 Report

- `api/report/sale-detail-revenue`
- `api/report/sale-revenue`
- `api/report/sale-debt-revenue`
- `api/report/customer-point-revenue`
- `api/report/sale-service-shift-revenue`
- `api/report/service-turn-employee`
- `api/report/report-partners-vip2`
- `api/report/branch-service-revenue`
- `api/report/export`

**Mục đích:** Cung cấp báo cáo doanh thu/chỉ số vận hành, hỗ trợ xuất file đồng bộ và bất đồng bộ qua bảng `ExportJobs`.

### 8.5 Mobile API

- `GET /api/client/payment-card-histories`
- `GET /api/client/payment-card-histories/customer-point-transactions`
- `GET /api/client/payment-cards`
- `GET /api/client/customers/{code}`
- `GET /api/client/customers/phone/{phone}`
- `GET /api/client/customers/{usercode}/vouchers`

## 9. Các điểm đặc biệt

- **Caching (Redis) 📌**
  - Có `AddCacheSetup()` đọc `RedisConfiguration`.
  - Khi `Enabled=true`, đăng ký `IConnectionMultiplexer`, distributed cache, `IRedisCacheService`.
- **SignalR / Realtime 🚀**
  - Hub `PaymentHub` có `[Authorize]`, group theo `transaction:{transactionId}`.
  - Push event `TransactionUpdated` sau webhook/manual approve thành công.
- **Background jobs**
  - `JobCustomerLevelDaily`: chạy hằng ngày 23:30.
  - `CustomerPointSyncService`: chạy sync điểm hằng năm (mốc 01/04).
  - `ExportJobWorker`: poll `ExportJobs` pending, tạo file báo cáo.
- **Logging**
  - Dùng Serilog (console + rolling file app/error).
  - Có module app log/audit log handler trong business.
- **Object Storage**
  - Tích hợp MinIO qua `MinioClient` + `IMinioStorage`.

## 10. Cách chạy dự án

### 10.1 Setup môi trường

- Cài .NET SDK 8.
- Chuẩn bị SQL Server (hoặc PostgreSQL nếu đổi `DbType`).
- Chuẩn bị Redis (nếu bật cache).
- Chuẩn bị cấu hình external service:
  - SSO (`AuthentService`)
  - Payment Gateway
  - MinIO (nếu dùng upload/object storage)

### 10.2 Cấu hình

- File chính:
  - `MA.Core.API/appsettings.json`
  - `MA.Core.API/appsettings.Development.json`
  - `Anpos.Mobile.API/appsettings.json`
  - `Anpos.Mobile.API/appsettings.Development.json`
- Mục cấu hình quan trọng:
  - `ConnectionString`
  - `Jwt`
  - `Swagger`
  - `RedisConfiguration`
  - `PaymentGateway`
  - `AuthentService`
  - `Minio`

### 10.3 Command run

```bash
dotnet restore AnPos.API.sln
dotnet build AnPos.API.sln -c Debug
dotnet run --project MA.Core.API/AnPos.API.csproj
dotnet run --project Anpos.Mobile.API/Anpos.Mobile.API.csproj
```

## 11. Nhận xét & đề xuất cải thiện

### 11.1 Code structure

- ✅ Ưu điểm:
  - Phân lớp rõ ràng theo API/Business/Data/Domain.
  - DI nhất quán theo handler.
  - Có generic repository + unit of work.
- ⚠️ Đề xuất:
  - Chuẩn hóa naming (`Bussiness` → `Business`) để giảm technical debt.
  - Tách bớt handler quá lớn (ví dụ `SaleBillHandler` rất dài) thành các service/use-case nhỏ hơn.
  - Bổ sung mapping profile hoặc dedicated DTO mapper để giảm code map thủ công lặp lại.

### 11.2 Performance

- Dùng `HttpClient` trực tiếp trong handler; nên chuyển sang `IHttpClientFactory` để quản lý connection pool tốt hơn.
- Một số luồng có khả năng query nhiều lần trong vòng lặp; nên tối ưu batch query/preload.
- Cân nhắc thêm caching tầng đọc cho lookup/danh mục dùng nhiều.

### 11.3 Security

- Nhiều endpoint có `[Authorize]` đang comment; cần rà soát và bật lại đúng policy.
- Nên quản lý secret qua User Secrets/Environment variables/secret manager thay vì để trực tiếp trong `appsettings.json`.
- Nên chuẩn hóa cơ chế webhook signature (HMAC) thay vì chỉ API key tĩnh.
- `ClockSkew = TimeSpan.FromHours(8)` khá lớn; nên cân nhắc giảm để kiểm soát lifetime token chặt hơn.

### 11.4 Maintainability

- Bổ sung test tự động cho business handler quan trọng (SaleBill, PaymentTransaction, Account integration).
- Bổ sung tài liệu contract API (OpenAPI examples, error code dictionary).
- Bổ sung health checks cho DB/Redis/external gateway.
- Chuẩn hóa middleware pipeline để tránh lặp CORS/Swagger registration.

---

## Ghi chú tính đầy đủ dữ liệu

- **Không tìm thấy trong source:** endpoint login nội bộ, refresh token flow, cơ chế permission matrix chi tiết theo role.
- Phân tích trên dựa vào code hiện có trong các module API/Business/Data/Domain và cấu hình runtime.
