CREATE OR ALTER PROCEDURE [dbo].[sp_GetDailyRevenueReport]
    @BranchID NVARCHAR(MAX) = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @BranchID = '' OR @BranchID = 'null' OR @BranchID = 'undefined' OR @BranchID = 'all'
        SET @BranchID = NULL;

    WITH OrderPayments AS (
        SELECT OrderId, BranchCode,
            SUM(CASE WHEN PaymentType = 'Cash' THEN PaymentAmount ELSE 0 END) AS CashAmount,
            SUM(CASE WHEN PaymentType IN ('Transfer', 'QR', 'Bank', 'CreditCard', 'Card') THEN PaymentAmount ELSE 0 END) AS TransferAmount,
            SUM(CASE WHEN PaymentType LIKE '%Voucher%' OR PaymentType LIKE '%Khuyen mai%' OR PaymentType LIKE '%Khuyến mại%' OR PaymentType = 'Discount' THEN PaymentAmount ELSE 0 END) AS VoucherAmount
        FROM dbo.SalesOrderPayment
        GROUP BY OrderId, BranchCode
    ),
    DailyAggregates AS (
        SELECT
            CAST(o.OrderDate AS DATE) AS ReportDate,
            SUM(o.SubTotal) AS PreTaxAmount,
            SUM(o.VatAmount) AS TaxAmount,
            SUM(o.DiscountAmount) AS DiscountAmount,
            SUM(o.TotalAmount) AS TotalAmount,
            SUM(o.CustomerCount) AS CustomerCount,
            SUM(ISNULL(op.CashAmount, 0)) AS CashAmount,
            SUM(ISNULL(op.TransferAmount, 0)) AS TransferAmount,
            SUM(ISNULL(op.VoucherAmount, 0)) AS VoucherAmount
        FROM dbo.SalesOrder o
        LEFT JOIN OrderPayments op ON o.Id = op.OrderId AND o.BranchCode = op.BranchCode
        WHERE o.Status = 'Paid'
            AND (@BranchID IS NULL OR o.BranchCode = @BranchID OR o.BranchCode IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@BranchID, ',') WHERE LTRIM(RTRIM(value)) <> ''))
            AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
            AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE))
        GROUP BY CAST(o.OrderDate AS DATE)
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY ReportDate DESC) AS STT,
        ReportDate,
        PreTaxAmount,
        TaxAmount,
        DiscountAmount,
        TotalAmount,
        CashAmount,
        TransferAmount,
        VoucherAmount,
        ActualReceipt = CashAmount + TransferAmount,
        CustomerCount
    INTO #TempDailyReport
    FROM DailyAggregates;

    -- Result Set 1: Paginated daily aggregates
    SELECT * 
    FROM #TempDailyReport 
    ORDER BY ReportDate DESC 
    OFFSET (@PageNumber - 1) * @PageSize ROWS 
    FETCH NEXT @PageSize ROWS ONLY;

    -- Result Set 2: Global totals for footer
    SELECT 
        COUNT(1) AS TotalRecords,
        ISNULL(SUM(PreTaxAmount), 0) AS TotalPreTaxAmount,
        ISNULL(SUM(TaxAmount), 0) AS TotalTaxAmount,
        ISNULL(SUM(DiscountAmount), 0) AS TotalDiscountAmount,
        ISNULL(SUM(TotalAmount), 0) AS TotalTotalAmount,
        ISNULL(SUM(CashAmount), 0) AS TotalCashAmount,
        ISNULL(SUM(TransferAmount), 0) AS TotalTransferAmount,
        ISNULL(SUM(VoucherAmount), 0) AS TotalVoucherAmount,
        ISNULL(SUM(ActualReceipt), 0) AS TotalActualReceipt,
        ISNULL(SUM(CustomerCount), 0) AS TotalCustomerCount
    FROM #TempDailyReport;

    DROP TABLE #TempDailyReport;
END;
