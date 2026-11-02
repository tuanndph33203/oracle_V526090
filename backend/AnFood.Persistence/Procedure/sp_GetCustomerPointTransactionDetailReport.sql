CREATE OR ALTER PROCEDURE dbo.sp_GetCustomerPointTransactionDetailReport
    @FromDate DATE,
    @ToDate DATE,
    @Customers NVARCHAR(MAX) = NULL,
    @TextSearch NVARCHAR(MAX) = NULL,
    @Page INT = 1,
    @Size INT = 20,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrimmedTextSearch NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@TextSearch)), N'');
    DECLARE @EffectivePage INT = CASE WHEN ISNULL(@Page, 0) < 1 THEN 1 ELSE @Page END;
    DECLARE @RequestedSize INT = CASE WHEN ISNULL(@Size, 0) <= 0 THEN 20 ELSE @Size END;

    DECLARE @CustomerFilter TABLE (Value NVARCHAR(200) PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@Customers)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @CustomerFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Customers, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    SELECT
        t.[Index],
        t.BranchCode,
        t.DocumentType,
        t.DocumentDate,
        t.DocumentNumber,
        t.CustomerCode,
        t.CustomerName,
        t.CardSerialNumber AS CustomerNumberCard,
        t.QuantityIn,
        t.QuantityOut,
        t.AmountIn,
        t.AmountOut,
        t.EndingQuantity,
        t.EndingBalance,
        ROW_NUMBER() OVER
        (
            ORDER BY ISNULL(t.[Index], 0) ASC
        ) AS RowNum
    INTO #FilteredTransactions
    FROM CustomerPointTransaction t
    WHERE t.DocumentDate IS NOT NULL
        AND CAST(t.DocumentDate AS DATE) >= @FromDate
        AND CAST(t.DocumentDate AS DATE) <= @ToDate
        AND
        (
            NOT EXISTS (SELECT 1 FROM @CustomerFilter)
            OR EXISTS
            (
                SELECT 1
                FROM @CustomerFilter cf
                WHERE cf.Value = t.CustomerCode
            )
        )
        AND
        (
            @TrimmedTextSearch IS NULL
            OR t.DocumentNumber LIKE N'%' + @TrimmedTextSearch + N'%'
            OR t.CustomerCode LIKE N'%' + @TrimmedTextSearch + N'%'
            OR t.CustomerName LIKE N'%' + @TrimmedTextSearch + N'%'
            OR t.CardSerialNumber LIKE N'%' + @TrimmedTextSearch + N'%'
            OR t.Description LIKE N'%' + @TrimmedTextSearch + N'%'
            OR t.DocumentType LIKE N'%' + @TrimmedTextSearch + N'%'
        );

    SELECT @TotalCount = COUNT(1)
    FROM #FilteredTransactions;

    SELECT
        ft.[Index],
        ft.BranchCode,
        ft.DocumentType,
        ft.DocumentDate,
        ft.DocumentNumber,
        ft.CustomerCode,
        ft.CustomerName,
        ft.CustomerNumberCard,
        ft.QuantityIn,
        ft.QuantityOut,
        ft.AmountIn,
        ft.AmountOut,
        ft.EndingQuantity,
        ft.EndingBalance
    FROM #FilteredTransactions ft
    WHERE ft.RowNum BETWEEN ((@EffectivePage - 1) * @RequestedSize + 1) AND (@EffectivePage * @RequestedSize)
    ORDER BY ISNULL(ft.[Index], 0) ASC, ft.RowNum ASC;

    DROP TABLE #FilteredTransactions;
END;
