# Commission Sales Report Spec

## Scope

Technical spec for the current `Commission Sales Report` implementation in this repository.

## Source mapping

- `Commission.Type1 = 1` -> `SaleService` + `SaleServiceDetail`
- `Commission.Type1 = 2` -> `SaleBill` + `SaleBillDetail`
- `Commission.Type1 = 3` -> `NoNameOrders` + `NoNameOrderDetail`

## Normalized output columns

- `DocumentDate`
- `DocumentNumber`
- `CustomerCode`
- `CustomerName`
- `Quantity`
- `Amount`
- `CommissionCode`
- `CommissionGroupName`
- `CommissionPercent`
- `CommissionAmount`
- `BranchCode`
- `BranchName`
- `Phone`
- `SalesEmployeeCode`
- `SalesEmployeeName`

## Amount rules

- Service: sum of `SaleServiceDetail.PayImmediately` for matched detail rows
- Package: `SaleBill.TotalMoneyPay` at document level
- NoName: `NoNameOrders.TotalPayment` at document level

Header-level amount fields must not be summed once per matched detail row. They are aggregated with header-safe logic during invoice grouping.

## Validation rules

- Date filter is applied at source query level
- Invoice year must match commission year when commission year is set
- Invoice transaction type must match commission transaction type when commission transaction type is set
- `DV_20` requires voucher data
- If commission requires salesman and invoice salesman is empty, the invoice is skipped
- Commission group start/end date must cover the invoice date
- Invoice branch must belong to the applied branch set when the branch set is configured
- The highest valid threshold is selected by `SalesMin DESC, CommissionPercentage DESC`

## Output row semantics

### Grand total row

- `RowType = -1`
- Label is shown in `CustomerName`
- `DocumentNumber` is empty
- Quantity, Amount, CommissionAmount are sums of employee groups

### Employee summary row

- `RowType = 0`
- `CustomerCode = SalesEmployeeCode`
- `CustomerName = SalesEmployeeName`
- `DocumentNumber` is empty
- Quantity, Amount, CommissionAmount are sums of detail rows for that employee

### Detail row

- `RowType = 1`
- One row per matched invoice after threshold selection

## Main files

- `Anpos.Business/Bussiness/Reports/CommissionSalesReport/sp_CommissionSalesReport.sql.txt`
- `Anpos.Business/Bussiness/Reports/CommissionSalesReport/sp_CommissionSalesReport_Count.sql.txt`
- `Anpos.Business/Bussiness/Reports/CommissionSalesReport/sp_CommissionSalesReport_Export.sql.txt`
- `Anpos.Business/Bussiness/Reports/CommissionSalesReport/CommissionSalesReportHandler.cs`
- `Anpos.Business/Bussiness/Reports/CommissionSalesReport/CommissionSalesReportModel.cs`
- `Anpos.Business/Bussiness/Reports/ReportExport/ReportExportHandler.cs`
