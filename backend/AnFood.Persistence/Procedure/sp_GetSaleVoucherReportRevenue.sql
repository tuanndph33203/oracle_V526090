CREATE   PROCEDURE [dbo].[sp_GetSaleVoucherReportRevenue]
    @Branchs        NVARCHAR(MAX)   = NULL,
    @Customers      NVARCHAR(MAX)   = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(200)   = NULL,
    @Page           INT             = 0,
    @Size           INT             = 20,
    @TotalCount     INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Filtered vouchers with joined data
    SELECT
        cus.CustomerCode,
        cus.CustomerName,
        fv.Code             AS VoucherCode,
        fv.VoucherTypeCode,
        CASE
            WHEN fv.[Percent] <> 0 THEN CAST(fv.[Percent] AS NVARCHAR(50))
            ELSE svt.Name
        END                 AS VoucherTypeName,
        fv.[Percent],
        fv.Money,
        CASE
            WHEN fv.QuantityUse > 0 THEN N'Đã sử dụng'
            ELSE N'Chưa sử dụng'
        END                 AS Status,
        fv.ListProductsCode AS ServiceCodes,
        fv.CreatedOnDate
    INTO #VoucherData
    FROM SaleVoucher fv
    LEFT JOIN SaleVoucherType svt ON svt.Code = fv.VoucherTypeCode
    LEFT JOIN Customer cus        ON cus.CustomerCode = fv.ListCustomersCode
    WHERE fv.Active = 1
      AND (@Branchs IS NULL OR fv.BranchCode IN (SELECT TRIM(value) FROM STRING_SPLIT(@Branchs, ',')))
      AND (@Customers IS NULL OR fv.ListCustomersCode LIKE '%' + @Customers + '%')
      AND (
            @TextSearch IS NULL
            OR fv.Code LIKE '%' + @TextSearch + '%'
            OR fv.ListCustomersCode LIKE '%' + @TextSearch + '%'
          );

    -- 2. Earliest usage per voucher
    SELECT
        svd.VoucherCode,
        sv.DocumentDate,
        svd.VoucherAmount AS MoneyExit
    INTO #UsageRaw
    FROM SaleServiceDetail svd
    INNER JOIN SaleService sv ON sv.STTRec = svd.STTRec
    WHERE svd.VoucherCode IN (SELECT VoucherCode FROM #VoucherData);

    SELECT
        VoucherCode,
        DocumentDate,
        MoneyExit
    INTO #Usage
    FROM (
        SELECT
            VoucherCode,
            DocumentDate,
            MoneyExit,
            ROW_NUMBER() OVER (PARTITION BY VoucherCode ORDER BY DocumentDate) AS rn
        FROM #UsageRaw
    ) t
    WHERE t.rn = 1;

    -- 3. Calculate amounts
    SELECT
        vd.CustomerCode,
        vd.CustomerName,
        vd.VoucherCode,
        vd.VoucherTypeCode,
        vd.VoucherTypeName,
        vd.ServiceCodes,
        vd.Status,
        ud.DocumentDate AS UsedDate,

        -- OpeningAmount
        CASE
            WHEN vd.[Percent] <> 0 THEN 0
            ELSE
                CASE
                    WHEN CAST(vd.CreatedOnDate AS DATE) < @FromDate THEN ISNULL(vd.Money, 0)
                    ELSE 0
                END
        END AS OpeningAmount,

        -- InAmount
        CASE
            WHEN vd.[Percent] <> 0 THEN
                CASE
                    WHEN ud.VoucherCode IS NOT NULL
                         AND CAST(ud.DocumentDate AS DATE) >= @FromDate AND CAST(ud.DocumentDate AS DATE) <= @ToDate
                        THEN ISNULL(ud.MoneyExit, 0)
                    ELSE 0
                END
            ELSE
                CASE
                    WHEN CAST(vd.CreatedOnDate AS DATE) >= @FromDate AND CAST(vd.CreatedOnDate AS DATE) <= @ToDate
                        THEN ISNULL(vd.Money, 0)
                    ELSE 0
                END
        END AS InAmount,

        -- OutAmount
        CASE
            WHEN ud.VoucherCode IS NOT NULL
                 AND CAST(ud.DocumentDate AS DATE) >= @FromDate AND CAST(ud.DocumentDate AS DATE) <= @ToDate
                THEN ISNULL(ud.MoneyExit, 0)
            ELSE 0
        END AS OutAmount

    INTO #Calculated
    FROM #VoucherData vd
    LEFT JOIN #Usage ud ON ud.VoucherCode = vd.VoucherCode;

    -- 4. Total count
    SELECT @TotalCount = COUNT(*) FROM #Calculated;

    -- 5. Paginated result
    SELECT
        CustomerCode,
        CustomerName,
        VoucherCode     AS Code,
        VoucherTypeCode,
        VoucherTypeName,
        ServiceCodes,
        Status,
        OpeningAmount,
        InAmount,
        OutAmount,
        OpeningAmount + InAmount - OutAmount AS ClosingAmount,
        UsedDate
    FROM #Calculated
    ORDER BY VoucherCode, UsedDate DESC
    OFFSET @Page * @Size ROWS
    FETCH NEXT @Size ROWS ONLY;

    -- 6. Cleanup
    DROP TABLE IF EXISTS #VoucherData;
    DROP TABLE IF EXISTS #UsageRaw;
    DROP TABLE IF EXISTS #Usage;
    DROP TABLE IF EXISTS #Calculated;
END
