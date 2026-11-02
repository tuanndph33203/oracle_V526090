CREATE   PROCEDURE [dbo].[sp_SaleServiceShiftRevenue_Count]
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
            d.ServiceCode
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
            ServiceCode
        FROM BaseData
        GROUP BY ServiceCode
    ),
    ResultData AS
    (
        SELECT
            g.ServiceCode,
            ISNULL(p.Name, '') AS ServiceName
        FROM GroupedData g
        LEFT JOIN dbo.Product p ON p.Code = g.ServiceCode
        WHERE
            @TextSearchNormalized IS NULL
            OR g.ServiceCode LIKE '%' + @TextSearchNormalized + '%'
            OR p.Name LIKE '%' + @TextSearchNormalized + '%'
    )
    SELECT COUNT(1)
    FROM ResultData
    OPTION (RECOMPILE);
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

END;
