CREATE   PROCEDURE [dbo].[sp_SaleServiceShiftRevenue_Export]
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL,
    @BranchCode     NVARCHAR(500)   = NULL,
    @ServiceCode    NVARCHAR(500)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
	    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    DECLARE @FromDateTime DATETIME2(0) = CASE WHEN @FromDate IS NULL THEN NULL ELSE CAST(@FromDate AS DATETIME2(0)) END;
    DECLARE @ToDateExclusive DATETIME2(0) = CASE WHEN @ToDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2(0))) END;
    DECLARE @TextSearchNormalized NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@TextSearch)), '');
    DECLARE @ServiceCodeNormalized NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@ServiceCode)), '');
    DECLARE @BranchCodeNormalized NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@BranchCode)), '');

    DECLARE @BranchFilter TABLE (BranchCode NVARCHAR(500) PRIMARY KEY);
    DECLARE @ServiceCodeFilter TABLE (ServiceCode NVARCHAR(500) PRIMARY KEY);
    IF @BranchCodeNormalized IS NOT NULL
    BEGIN
        INSERT INTO @BranchFilter (BranchCode)
        SELECT DISTINCT LTRIM(RTRIM([value]))
        FROM STRING_SPLIT(@BranchCodeNormalized, ',')
        WHERE LTRIM(RTRIM([value])) <> '';
    END

    IF @ServiceCodeNormalized IS NOT NULL
    BEGIN
        INSERT INTO @ServiceCodeFilter (ServiceCode)
        SELECT DISTINCT LTRIM(RTRIM([value]))
        FROM STRING_SPLIT(@ServiceCodeNormalized, ',')
        WHERE LTRIM(RTRIM([value])) <> '';
    END

    SELECT DISTINCT s.STTRec
    INTO #FilteredSaleService
    FROM dbo.SaleService s
    WHERE
        (@FromDateTime IS NULL OR s.DocumentDate >= @FromDateTime)
        AND (@ToDateExclusive IS NULL OR s.DocumentDate < @ToDateExclusive)
        AND (
            @BranchCodeNormalized IS NULL
            OR EXISTS (SELECT 1 FROM @BranchFilter bf WHERE bf.BranchCode = s.BranchCode)
        );

    CREATE CLUSTERED INDEX IX_FilteredSaleService_STTRec ON #FilteredSaleService(STTRec);

    ;WITH BaseData AS
    (
        SELECT
            d.ServiceCode,
            ISNULL(d.Quantity, 0) AS Quantity,
            ISNULL(TRY_CONVERT(DECIMAL(18,2), d.TimeUsed), 0) AS TimeUsed,
            d.Gender,
            HourValue = CASE
                WHEN TRY_CONVERT(TIME(0), d.TimeIn) IS NOT NULL THEN DATEPART(HOUR, TRY_CONVERT(TIME(0), d.TimeIn))
                WHEN CHARINDEX(':', ISNULL(d.TimeIn, '')) > 0
                    THEN TRY_CONVERT(INT, LEFT(d.TimeIn, CHARINDEX(':', d.TimeIn) - 1))
                ELSE TRY_CONVERT(INT, d.TimeIn)
            END
        FROM dbo.SaleServiceDetail d
        INNER JOIN #FilteredSaleService s ON s.STTRec = d.STTRec
        WHERE
            (
                @ServiceCodeNormalized IS NULL
                OR EXISTS
                (
                    SELECT 1
                    FROM @ServiceCodeFilter sf
                    WHERE d.ServiceCode LIKE '%' + sf.ServiceCode + '%'
                )
            )
    ),
    GroupedData AS
    (
        SELECT
            ServiceCode,
            SUM(Quantity) AS TotalQuantity,
            SUM(CASE WHEN HourValue >= 0 AND HourValue < 9 THEN 1 ELSE 0 END) AS Shift1,
            SUM(CASE WHEN HourValue >= 9 AND HourValue < 11 THEN 1 ELSE 0 END) AS Shift2,
            SUM(CASE WHEN HourValue >= 11 AND HourValue < 14 THEN 1 ELSE 0 END) AS Shift3,
            SUM(CASE WHEN HourValue >= 14 AND HourValue < 17 THEN 1 ELSE 0 END) AS Shift4,
            SUM(CASE WHEN HourValue >= 17 AND HourValue < 19 THEN 1 ELSE 0 END) AS Shift5,
            SUM(CASE WHEN HourValue >= 19 AND HourValue < 22 THEN 1 ELSE 0 END) AS Shift6,
            SUM(CASE WHEN HourValue >= 22 AND HourValue < 24 THEN 1 ELSE 0 END) AS Shift7,
            SUM(TimeUsed) AS TotalTime,
            SUM(CASE WHEN Gender = 1 THEN 1 ELSE 0 END) AS Male,
            SUM(CASE WHEN Gender = 2 THEN 1 ELSE 0 END) AS Female,
            CAST(0 AS INT) AS Other
        FROM BaseData
        GROUP BY ServiceCode
    ),
    ResultData AS
    (
        SELECT
            g.ServiceCode,
            ISNULL(p.Name, '') AS ServiceName,
            g.TotalQuantity,
            g.Shift1,
            g.Shift2,
            g.Shift3,
            g.Shift4,
            g.Shift5,
            g.Shift6,
            g.Shift7,
            g.TotalTime,
            g.Male,
            g.Female,
            g.Other
        FROM GroupedData g
        LEFT JOIN dbo.Product p ON p.Code = g.ServiceCode
        WHERE
            @TextSearchNormalized IS NULL
            OR g.ServiceCode LIKE '%' + @TextSearchNormalized + '%'
            OR p.Name LIKE '%' + @TextSearchNormalized + '%'
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY ServiceCode) AS STT,
        ServiceCode,
        ServiceName,
        CAST(TotalQuantity AS INT) AS TotalQuantity,
        CAST(Shift1 AS INT) AS Shift1,
        CAST(Shift2 AS INT) AS Shift2,
        CAST(Shift3 AS INT) AS Shift3,
        CAST(Shift4 AS INT) AS Shift4,
        CAST(Shift5 AS INT) AS Shift5,
        CAST(Shift6 AS INT) AS Shift6,
        CAST(Shift7 AS INT) AS Shift7,
        CAST(TotalTime AS DECIMAL(18,2)) AS TotalTime,
        CAST(Male AS INT) AS Male,
        CAST(Female AS INT) AS Female,
        Other
    FROM ResultData
    ORDER BY ServiceCode
    OPTION (RECOMPILE);
                SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

END;
