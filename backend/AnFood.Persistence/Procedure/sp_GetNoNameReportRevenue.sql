CREATE PROCEDURE dbo.sp_GetNoNameReportRevenue
    @FromDate DATE,
    @ToDate DATE,
    @Branchs NVARCHAR(MAX) = NULL,
    @TextSearch NVARCHAR(MAX) = NULL,
    @Page INT = 1,
    @Size INT = 20,
    @IsAll BIT = 0,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrimmedTextSearch NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@TextSearch)), N'');
    DECLARE @EffectivePage INT = CASE WHEN @IsAll = 1 OR ISNULL(@Page, 0) < 1 THEN 1 ELSE @Page END;
    DECLARE @RequestedSize INT = CASE WHEN ISNULL(@Size, 0) <= 0 THEN 20 ELSE @Size END;

    DECLARE @BranchFilter TABLE (Value NVARCHAR(200) PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@Branchs)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @BranchFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Branchs, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    ;WITH FirstUsage AS
    (
        SELECT
            svd.NoNameCard,
            sv.DocumentDate,
            svd.NoNameAmount,
            ROW_NUMBER() OVER
            (
                PARTITION BY svd.NoNameCard
                ORDER BY sv.DocumentDate ASC, sv.STTRec ASC
            ) AS RowNum
        FROM SaleServiceDetail svd
        INNER JOIN SaleService sv ON svd.STTRec = sv.STTRec
        WHERE svd.NoNameCard IS NOT NULL
    ),
    FirstOrder AS
    (
        SELECT
            nnd.CardNumberExternal,
            nno.DocumentDate,
            ROW_NUMBER() OVER
            (
                PARTITION BY nnd.CardNumberExternal
                ORDER BY nno.DocumentDate ASC, nno.STTRec ASC
            ) AS RowNum
        FROM NoNameOrderDetail nnd
        INNER JOIN NoNameOrders nno ON nnd.SttRec = nno.SttRec
        WHERE nnd.CardNumberExternal IS NOT NULL
    ),
    Calculated AS
    (
        SELECT
            nc.CustomerList AS Customer,
            nc.BranchCode AS Branch,
            nc.CardNumberExternal,
            nc.CardSerialExternal,
            nc.ItemList AS Service,
            nc.CreatedOnDate,
            CAST(
                CASE
                    WHEN CAST(fo.DocumentDate AS DATE) < @FromDate THEN
                        CASE
                            WHEN CAST(fu.DocumentDate AS DATE) >= @FromDate AND CAST(fu.DocumentDate AS DATE) <= @ToDate
                                THEN ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                            WHEN CAST(fu.DocumentDate AS DATE) > @ToDate
                                THEN ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                            WHEN fu.DocumentDate IS NOT NULL
                                THEN ISNULL(nc.BalanceAmount, 0)
                            ELSE ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                        END
                    ELSE 0
                END
            AS DECIMAL(18, 2)) AS OpeningAmount,
            CAST(
                CASE
                    WHEN CAST(fo.DocumentDate AS DATE) >= @FromDate AND CAST(fo.DocumentDate AS DATE) <= @ToDate
                        THEN ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                    ELSE 0
                END
            AS DECIMAL(18, 2)) AS InAmount,
            CAST(
                CASE
                    WHEN CAST(fo.DocumentDate AS DATE) < @FromDate
                         AND CAST(fu.DocumentDate AS DATE) >= @FromDate
                         AND CAST(fu.DocumentDate AS DATE) <= @ToDate
                        THEN ISNULL(fu.NoNameAmount, 0)
                    WHEN CAST(fo.DocumentDate AS DATE) >= @FromDate
                         AND CAST(fo.DocumentDate AS DATE) <= @ToDate
                         AND CAST(fu.DocumentDate AS DATE) >= @FromDate
                         AND CAST(fu.DocumentDate AS DATE) <= @ToDate
                        THEN ISNULL(fu.NoNameAmount, 0)
                    ELSE 0
                END
            AS DECIMAL(18, 2)) AS OutAmount,
            CAST(
                CASE
                    WHEN CAST(fo.DocumentDate AS DATE) < @FromDate THEN
                        CASE
                            WHEN CAST(fu.DocumentDate AS DATE) >= @FromDate AND CAST(fu.DocumentDate AS DATE) <= @ToDate
                                THEN (ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)) - ISNULL(fu.NoNameAmount, 0)
                            WHEN CAST(fu.DocumentDate AS DATE) > @ToDate
                                THEN ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                            WHEN fu.DocumentDate IS NOT NULL
                                THEN ISNULL(nc.BalanceAmount, 0)
                            ELSE ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                        END
                    WHEN CAST(fo.DocumentDate AS DATE) >= @FromDate AND CAST(fo.DocumentDate AS DATE) <= @ToDate THEN
                        CASE
                            WHEN CAST(fu.DocumentDate AS DATE) >= @FromDate AND CAST(fu.DocumentDate AS DATE) <= @ToDate
                                THEN (ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)) - ISNULL(fu.NoNameAmount, 0)
                            ELSE ISNULL(nc.SaleAmount, 0) * (1 + ISNULL(nc.TaxRate, 0) / 100.0)
                        END
                    ELSE 0
                END
            AS DECIMAL(18, 2)) AS ClosingAmount,
            ROW_NUMBER() OVER
            (
                ORDER BY nc.CreatedOnDate DESC, ISNULL(nc.CardNumberExternal, N'') DESC
            ) AS RowNum
        FROM NoNameCard nc
        LEFT JOIN FirstUsage fu
            ON fu.NoNameCard = nc.CardNumberExternal
           AND fu.RowNum = 1
        LEFT JOIN FirstOrder fo
            ON fo.CardNumberExternal = nc.CardNumberExternal
           AND fo.RowNum = 1
        WHERE
            (
                NOT EXISTS (SELECT 1 FROM @BranchFilter)
                OR EXISTS
                (
                    SELECT 1
                    FROM @BranchFilter bf
                    WHERE bf.Value = nc.BranchCode
                )
            )
            AND
            (
                @TrimmedTextSearch IS NULL
                OR nc.CardNumberExternal LIKE N'%' + @TrimmedTextSearch + N'%'
                OR nc.CardSerialExternal LIKE N'%' + @TrimmedTextSearch + N'%'
                OR nc.CustomerList LIKE N'%' + @TrimmedTextSearch + N'%'
            )
    )
    SELECT
        Customer,
        Branch,
        CardNumberExternal,
        CardSerialExternal,
        Service,
        OpeningAmount,
        InAmount,
        OutAmount,
        ClosingAmount,
        RowNum
    INTO #Calculated
    FROM Calculated;

    SELECT @TotalCount = COUNT(1)
    FROM #Calculated;

    IF @IsAll = 1
    BEGIN
        SELECT
            Customer,
            Branch,
            CardNumberExternal,
            CardSerialExternal,
            Service,
            OpeningAmount,
            InAmount,
            OutAmount,
            ClosingAmount
        FROM #Calculated
        ORDER BY RowNum;
    END
    ELSE
    BEGIN
        SELECT
            Customer,
            Branch,
            CardNumberExternal,
            CardSerialExternal,
            Service,
            OpeningAmount,
            InAmount,
            OutAmount,
            ClosingAmount
        FROM #Calculated
        WHERE RowNum BETWEEN ((@EffectivePage - 1) * @RequestedSize + 1) AND (@EffectivePage * @RequestedSize)
        ORDER BY RowNum;
    END

    DROP TABLE IF EXISTS #Calculated;
END;
