CREATE OR ALTER PROCEDURE dbo.sp_GetCustomerPointTransactionReport
    @FromDate DATE,
    @ToDate DATE,
    @Customers NVARCHAR(MAX) = NULL,
    @TextSearch NVARCHAR(MAX) = NULL,
    @Page INT = 1,
    @Size INT = 20,
    @IsAll BIT = 0,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrimmedTextSearch NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@TextSearch)), N'');
    DECLARE @EffectivePage INT = CASE WHEN @IsAll = 1 THEN 1 WHEN ISNULL(@Page, 0) < 1 THEN 1 ELSE @Page END;
    DECLARE @RequestedSize INT = CASE WHEN ISNULL(@Size, 0) <= 0 THEN 20 ELSE @Size END;

    DECLARE @CustomerFilter TABLE (Value NVARCHAR(200) PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@Customers)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @CustomerFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Customers, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    ;WITH RelevantCustomers AS
    (
        SELECT DISTINCT t.CustomerCode
        FROM CustomerPointTransaction t
        WHERE t.DocumentDate IS NOT NULL
            AND CAST(t.DocumentDate AS DATE) <= @ToDate
            AND NULLIF(LTRIM(RTRIM(t.CustomerCode)), N'') IS NOT NULL
    ),
    FilteredCustomers AS
    (
        SELECT
            c.CustomerCode,
            c.CustomerName,
            c.CustomerCardNumber,
            c.CustomerCardSerial,
            c.PhoneNumber2
        FROM Customer c
        WHERE NULLIF(LTRIM(RTRIM(c.CustomerCode)), N'') IS NOT NULL
            AND ISNULL(c.IsDelete, 0) = 0
            AND EXISTS
            (
                SELECT 1
                FROM RelevantCustomers rc
                WHERE rc.CustomerCode = c.CustomerCode
            )
            AND
            (
                NOT EXISTS (SELECT 1 FROM @CustomerFilter)
                OR EXISTS
                (
                    SELECT 1
                    FROM @CustomerFilter cf
                    WHERE cf.Value = c.CustomerCode
                )
            )
            AND
            (
                @TrimmedTextSearch IS NULL
                OR c.CustomerCardSerial LIKE N'%' + @TrimmedTextSearch + N'%'
                OR c.CustomerCardNumber LIKE N'%' + @TrimmedTextSearch + N'%'
                OR c.PhoneNumber2 LIKE N'%' + @TrimmedTextSearch + N'%'
                OR c.CustomerCode LIKE N'%' + @TrimmedTextSearch + N'%'
            )
    )
    SELECT
        fc.CustomerCode,
        fc.CustomerName,
        fc.CustomerCardNumber,
        ROW_NUMBER() OVER
        (
            ORDER BY ISNULL(latestUsedDate.UsedDate, '1900-01-01') DESC, ISNULL(fc.CustomerCode, N'') ASC
        ) AS RowNum
    INTO #FilteredCustomers
    FROM FilteredCustomers fc
    OUTER APPLY
    (
        SELECT TOP (1)
            s.DocumentDate AS UsedDate
        FROM SaleService s
        WHERE s.CustomerCode = fc.CustomerCode
            AND s.DocumentDate IS NOT NULL
        ORDER BY s.DocumentDate DESC, s.DocumentNumber DESC
    ) latestUsedDate;

    SELECT @TotalCount = COUNT(1)
    FROM #FilteredCustomers;

    SELECT
        fc.CustomerCode,
        fc.CustomerName,
        fc.CustomerCardNumber,
        fc.RowNum
    INTO #PagedCustomers
    FROM #FilteredCustomers fc
    WHERE @IsAll = 1
        OR fc.RowNum BETWEEN ((@EffectivePage - 1) * @RequestedSize + 1) AND (@EffectivePage * @RequestedSize);

    SELECT
        pc.CustomerCode,
        pc.CustomerName,
        pc.CustomerCardNumber,
        ISNULL(opening.EndingQuantity, 0) AS OpeningQuantity,
        CAST(ISNULL(opening.EndingQuantity, 0) * 1000 AS DECIMAL(18, 2)) AS OpeningAmount,
        ISNULL(agg.QuantityIn, 0) AS QuantityIn,
        ISNULL(agg.QuantityOut, 0) AS QuantityOut,
        ISNULL(agg.AmountIn, 0) AS AmountIn,
        ISNULL(agg.AmountOut, 0) AS AmountOut,
        ISNULL(opening.EndingQuantity, 0) + ISNULL(agg.QuantityIn, 0) - ISNULL(agg.QuantityOut, 0) AS EndingQuantity,
        CAST((ISNULL(opening.EndingQuantity, 0) * 1000) + ISNULL(agg.AmountIn, 0) - ISNULL(agg.AmountOut, 0) AS DECIMAL(18, 2)) AS EndingBalance,
        latestUsedDate.UsedDate
    FROM #PagedCustomers pc
    OUTER APPLY
    (
        SELECT TOP (1)
            t.EndingQuantity
        FROM CustomerPointTransaction t
        WHERE t.CustomerCode = pc.CustomerCode
            AND t.DocumentDate IS NOT NULL
            AND CAST(t.DocumentDate AS DATE) < @FromDate
        ORDER BY t.DocumentDate DESC, t.[Index] DESC
    ) opening
    OUTER APPLY
    (
        SELECT
            SUM(t.QuantityIn) AS QuantityIn,
            SUM(t.QuantityOut) AS QuantityOut,
            SUM(t.AmountIn) AS AmountIn,
            SUM(t.AmountOut) AS AmountOut
        FROM CustomerPointTransaction t
        WHERE t.CustomerCode = pc.CustomerCode
            AND t.DocumentDate IS NOT NULL
            AND CAST(t.DocumentDate AS DATE) >= @FromDate
            AND CAST(t.DocumentDate AS DATE) <= @ToDate
    ) agg
    OUTER APPLY
    (
        SELECT TOP (1)
            s.DocumentDate AS UsedDate
        FROM SaleService s
        WHERE s.CustomerCode = pc.CustomerCode
            AND s.DocumentDate IS NOT NULL
        ORDER BY s.DocumentDate DESC, s.DocumentNumber DESC
    ) latestUsedDate
    ORDER BY pc.RowNum;

    DROP TABLE #FilteredCustomers;
    DROP TABLE #PagedCustomers;
END;
