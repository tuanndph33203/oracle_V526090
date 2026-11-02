# API Export Reports - Test Guide

## 🎯 Endpoint Chính

```
POST /api/report/export
```

## 📝 Test Cases

### 1. Export Sale Detail Revenue (Excel)

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "ReportType": "SaleDetailRevenue",
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31",
    "FileType": "xlsx",
    "CustomerCode": "KH001",
    "BranchCode": "CN01",
    "PromotionCode": "KM01",
    "DiscountCode": "CK01",
    "AutoFitColumns": true
  }'
```

**Expected Response:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "reportType": "SaleDetailRevenue",
  "status": "Queued"
}
```

### 2. Export Sale Detail Revenue 2 (TXT)

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "ReportType": "SaleDetailRevenue2",
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31",
    "FileType": "txt",
    "CustomerCode": "KH001"
  }'
```

### 3. Export Sale Debt Revenue (Excel)

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "ReportType": "SaleDebtRevenue",
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31",
    "FileType": "xlsx",
    "CustomerCode": "KH001",
    "TextSearch": "Nguyễn"
  }'
```

### 4. Export Sale Debt Revenue (TXT)

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "ReportType": "SaleDebtRevenue",
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31",
    "FileType": "txt"
  }'
```

### 5. Test Validation - Missing ReportType

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "error": "ReportType is required. Valid values: SaleDetailRevenue, SaleDetailRevenue2, SaleDebtRevenue"
}
```

### 6. Test Validation - Invalid ReportType

```bash
curl -X POST "https://localhost:9500/api/report/export" \
  -H "Content-Type: application/json" \
  -d '{
    "ReportType": "InvalidType",
    "FromDate": "2024-01-01",
    "ToDate": "2024-12-31"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "error": "Invalid ReportType. Valid values: SaleDetailRevenue, SaleDetailRevenue2, SaleDebtRevenue"
}
```

---

## 🔍 Check Job Status

```bash
curl -X GET "https://localhost:9500/api/report/export/status/{jobId}"
```

**Response - Pending:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "fileUrl": null,
  "errorMessage": null
}
```

**Response - Processing:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Processing",
  "fileUrl": null,
  "errorMessage": null
}
```

**Response - Done:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Done",
  "fileUrl": "/api/report/sale-detail-revenue/export-sale-detail-revenue/download/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errorMessage": null
}
```

**Response - Failed:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Failed",
  "fileUrl": null,
  "errorMessage": "sp_SaleDebtRevenue_Count: Invalid parameter"
}
```

---

## 📥 Download File

```bash
curl -X GET "https://localhost:9500/api/report/export/download/{jobId}" \
  --output report.xlsx
```

---

## 📊 Report Types & Columns

### SaleDetailRevenue (87 columns)
- Ngày chứng từ, Số chứng từ, Mã KH, Tên KH, Điện thoại
- Số seri thẻ noname, Số thẻ PK, Mã dịch vụ, Tên dịch vụ
- Số lượng, Giá bán, Tiền bán, Chiết khấu, Thuế
- Mã NV1-5, Tiền NV1-5, Chi nhánh, Phòng, Giờ vào/ra
- ...và 70+ cột khác

### SaleDetailRevenue2 (36 columns)
- Ngày Ct, Số Ct, Mã khách, Tên khách hàng, Di động
- Số CCCD, Tên công ty, Địa chỉ, Mã số thuế, Email
- Số seri thẻ noname, Số thẻ pk, Mã dịch vụ, Tên dịch vụ
- Số lượng, Giá bán, Tiền bán, Chiết khấu, % thuế, Tiền thuế
- TT bằng tiền, Tiền mặt, Cà thẻ, Chuyển khoản
- ...và các cột khác

### SaleDebtRevenue (16 columns)
- Stt, Mã KH, Tên KH, Điện Thoại, Địa chỉ
- Dư đầu, Ps nợ, Ps có, Dư nợ
- Số Seri PK, Mã DV, Tên DV
- Mã NV1, Tên NV1, Mã NV2, Tên NV2

---

## 🔧 Backend Components

### 1. Controller
- `ReportExportController.cs`
- Single endpoint: `POST /api/report/export`
- Validates ReportType
- Returns jobId

### 2. Models
- `ReportFilterModel.cs` - Base model for all reports
- `SaleDetailRevenueFilterModel.cs` - Inherited model
- `SaleDebtRevenueFilterModel.cs` - Inherited model

### 3. Handlers
- `ReportExportHandler` - SaleDetailRevenue & SaleDetailRevenue2
- `SaleDebtRevenueHandler` - SaleDebtRevenue

### 4. Background Worker
- `ExportJobWorker.cs` - Processes jobs in queue
- Polls every 5 seconds
- Handles 3 report types
- Supports xlsx, xls, txt formats

### 5. Database
- Table: `ExportJobs`
- Columns: Id, Type, Status, CreatedAt, UpdatedAt, RequestJson, FilePath, ErrorMessage
- Statuses: Pending → Processing → Done/Failed

---

## ⚙️ Configuration

### Dependency Injection (ProjectServiceCollectionExtensions.cs)
```csharp
.AddScoped<IReportExportHandler, ReportExportHandler>()
.AddScoped<ISaleDebtRevenueHandler, SaleDebtRevenueHandler>()
```

### Background Service (Program.cs)
```csharp
builder.Services.AddHostedService<ExportJobWorker>();
```

---

## 🐛 Troubleshooting

### Job stuck in "Pending"
- Check ExportJobWorker is running
- Check database connection
- Check logs in `Logs/app/`

### Job failed with error
- Check stored procedure exists: `sp_SaleDetailRevenue_Export`, `sp_SaleDetailRevenue2_Export`, `sp_SaleDebtRevenue_Count`
- Check SQL Server connection
- Check ErrorMessage in job status response

### File not found on download
- Check `ExportFiles/{ReportType}/` folder exists
- Check FilePath in database
- Check file permissions

---

## 📚 Related Files

```
MA.Core.API/
├── Controllers/Reports/
│   └── ReportExportController.cs          # Main API endpoint
├── BackgroundServices/
│   └── ExportJobWorker.cs                 # Background worker
└── Extensions/
    └── ProjectServiceCollectionExtensions.cs  # DI registration

Anpos.Business/Bussiness/Reports/
├── ReportFilterModel.cs                   # Base model
├── ReportExport/
│   ├── IReportExportHandler.cs
│   └── ReportExportHandler.cs
├── SaleDetailRevenue/
│   ├── ISaleDetailRevenueHandler.cs
│   ├── SaleDetailRevenueHandler.cs
│   └── SaleDetailRevenueFilterModel.cs
└── SaleDebtRevenue/
    ├── ISaleDebtRevenueHandler.cs
    ├── SaleDebtRevenueHandler.cs
    └── SaleDebtRevenueFilterModel.cs

Anpos.Domain/Datatables/
└── ExportJob.cs                           # Database entity
```
