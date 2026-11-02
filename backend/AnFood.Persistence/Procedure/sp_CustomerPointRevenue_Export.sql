CREATE OR ALTER PROCEDURE [dbo].[sp_CustomerPointRevenue_Export]
    @CustomerCode   NVARCHAR(50)    = NULL,
    @BranchCode     NVARCHAR(500)   = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EffectiveFrom DATE = ISNULL(@FromDate, '19000101');
    DECLARE @EffectiveTo DATE = ISNULL(@ToDate, '99991231');

    DECLARE @NearestExpiry DATE;
    SELECT @NearestExpiry = MIN(CAST(cp.[Date] AS DATE))
    FROM CustomerPoints cp
    WHERE cp.[Date] IS NOT NULL
      AND CAST(cp.[Date] AS DATE) >= CAST(GETDATE() AS DATE);

    IF @NearestExpiry IS NULL
    BEGIN
        SELECT @NearestExpiry = MAX(CAST(cp.[Date] AS DATE))
        FROM CustomerPoints cp
        WHERE cp.[Date] IS NOT NULL;
    END

    ;WITH EligibleCustomers AS
    (
        SELECT
            LTRIM(RTRIM(c.CustomerCode)) AS CustomerCode,
            c.CustomerName,
            c.BranchCode
        FROM Customer c
        WHERE NULLIF(LTRIM(RTRIM(c.CustomerCode)), '') IS NOT NULL
          AND ISNULL(c.IsDelete, 0) = 0
          AND
          (
                @BranchCode IS NULL OR @BranchCode = ''
                OR c.BranchCode IN
                (
                    SELECT LTRIM(RTRIM([value]))
                    FROM STRING_SPLIT(@BranchCode, ',')
                )
          )
          AND (@CustomerCode IS NULL OR @CustomerCode = '' OR c.CustomerCode LIKE '%' + @CustomerCode + '%')
          AND
          (
                @TextSearch IS NULL OR @TextSearch = ''
                OR c.CustomerCode LIKE '%' + @TextSearch + '%'
                OR c.CustomerName LIKE '%' + @TextSearch + '%'
          )
    ),
    LatestPointByCustomerDate AS
    (
        SELECT
            t.CustomerCode,
            t.EndingPoint
        FROM
        (
            SELECT
                LTRIM(RTRIM(cp.CustomerCode)) AS CustomerCode,
                CAST(cp.[Date] AS DATE) AS PointDate,
                CAST(ISNULL(cp.EndingPoint, 0) AS DECIMAL(18,2)) AS EndingPoint,
                ROW_NUMBER() OVER
                (
                    PARTITION BY LTRIM(RTRIM(cp.CustomerCode)), CAST(cp.[Date] AS DATE)
                    ORDER BY cp.CreateOnDate DESC, cp.Id DESC
                ) AS Rn
            FROM CustomerPoints cp
            WHERE NULLIF(LTRIM(RTRIM(cp.CustomerCode)), '') IS NOT NULL
              AND cp.[Date] IS NOT NULL
        ) t
        WHERE t.Rn = 1
    ),
    TotalPointByCustomer AS
    (
        SELECT
            lp.CustomerCode,
            SUM(lp.EndingPoint) AS TotalPoint
        FROM LatestPointByCustomerDate lp
        GROUP BY lp.CustomerCode
    ),
    OpeningByCustomer AS
    (
        SELECT
            t.CustomerCode,
            CAST(ISNULL(t.EndingQuantity, 0) AS DECIMAL(18,2)) AS OpeningBalance
        FROM
        (
            SELECT
                LTRIM(RTRIM(cpt.CustomerCode)) AS CustomerCode,
                cpt.EndingQuantity,
                ROW_NUMBER() OVER
                (
                    PARTITION BY LTRIM(RTRIM(cpt.CustomerCode))
                    ORDER BY ISNULL(cpt.[Index], 0) DESC, cpt.DocumentDate DESC
                ) AS Rn
            FROM CustomerPointTransaction cpt
            WHERE NULLIF(LTRIM(RTRIM(cpt.CustomerCode)), '') IS NOT NULL
              AND cpt.DocumentDate IS NOT NULL
              AND CAST(cpt.DocumentDate AS DATE) < @EffectiveFrom
        ) t
        WHERE t.Rn = 1
    ),
    MovementByCustomer AS
    (
        SELECT
            LTRIM(RTRIM(cpt.CustomerCode)) AS CustomerCode,
            SUM(CAST(ISNULL(cpt.QuantityIn, 0) AS DECIMAL(18,2))) AS AccumulatedPoint,
            SUM(CAST(ISNULL(cpt.QuantityOut, 0) AS DECIMAL(18,2))) AS UsedPoint
        FROM CustomerPointTransaction cpt
        WHERE NULLIF(LTRIM(RTRIM(cpt.CustomerCode)), '') IS NOT NULL
          AND cpt.DocumentDate IS NOT NULL
          AND CAST(cpt.DocumentDate AS DATE) >= @EffectiveFrom
          AND CAST(cpt.DocumentDate AS DATE) <= @EffectiveTo
        GROUP BY LTRIM(RTRIM(cpt.CustomerCode))
    ),
    ClosingInRangeByCustomer AS
    (
        SELECT
            t.CustomerCode,
            CAST(ISNULL(t.EndingQuantity, 0) AS DECIMAL(18,2)) AS ClosingBalance
        FROM
        (
            SELECT
                LTRIM(RTRIM(cpt.CustomerCode)) AS CustomerCode,
                cpt.EndingQuantity,
                ROW_NUMBER() OVER
                (
                    PARTITION BY LTRIM(RTRIM(cpt.CustomerCode))
                    ORDER BY ISNULL(cpt.[Index], 0) DESC, cpt.DocumentDate DESC
                ) AS Rn
            FROM CustomerPointTransaction cpt
            WHERE NULLIF(LTRIM(RTRIM(cpt.CustomerCode)), '') IS NOT NULL
              AND cpt.DocumentDate IS NOT NULL
              AND CAST(cpt.DocumentDate AS DATE) >= @EffectiveFrom
              AND CAST(cpt.DocumentDate AS DATE) <= @EffectiveTo
        ) t
        WHERE t.Rn = 1
    ),
    LatestByCustomer AS
    (
        SELECT
            t.CustomerCode,
            CAST(ISNULL(t.EndingQuantity, 0) AS DECIMAL(18,2)) AS LatestEndingQuantity
        FROM
        (
            SELECT
                LTRIM(RTRIM(cpt.CustomerCode)) AS CustomerCode,
                cpt.EndingQuantity,
                ROW_NUMBER() OVER
                (
                    PARTITION BY LTRIM(RTRIM(cpt.CustomerCode))
                    ORDER BY ISNULL(cpt.[Index], 0) DESC, cpt.DocumentDate DESC
                ) AS Rn
            FROM CustomerPointTransaction cpt
            WHERE NULLIF(LTRIM(RTRIM(cpt.CustomerCode)), '') IS NOT NULL
        ) t
        WHERE t.Rn = 1
    ),
    FinalData AS
    (
        SELECT
            ec.CustomerName,
            ec.CustomerCode,
            ISNULL(tp.TotalPoint, 0) AS TotalPoint,
            ISNULL(ob.OpeningBalance, 0) AS OpeningBalance,
            ISNULL(mv.AccumulatedPoint, 0) AS AccumulatedPoint,
            ISNULL(mv.UsedPoint, 0) AS UsedPoint,
            ISNULL(cr.ClosingBalance, ISNULL(lb.LatestEndingQuantity, ISNULL(ob.OpeningBalance, 0))) AS ClosingBalance,
            ISNULL(mv.UsedPoint, 0) * 10 AS RedeemValue,
            @NearestExpiry AS ExpiredDate
        FROM EligibleCustomers ec
        LEFT JOIN TotalPointByCustomer tp ON tp.CustomerCode = ec.CustomerCode
        LEFT JOIN OpeningByCustomer ob ON ob.CustomerCode = ec.CustomerCode
        LEFT JOIN MovementByCustomer mv ON mv.CustomerCode = ec.CustomerCode
        LEFT JOIN ClosingInRangeByCustomer cr ON cr.CustomerCode = ec.CustomerCode
        LEFT JOIN LatestByCustomer lb ON lb.CustomerCode = ec.CustomerCode
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY fd.TotalPoint DESC) AS Stt,
        fd.CustomerName,
        fd.CustomerCode,
        fd.TotalPoint,
        fd.OpeningBalance,
        fd.AccumulatedPoint,
        fd.UsedPoint,
        fd.ClosingBalance,
        fd.RedeemValue,
        fd.ExpiredDate
    FROM FinalData fd
    ORDER BY fd.TotalPoint DESC;
END;
