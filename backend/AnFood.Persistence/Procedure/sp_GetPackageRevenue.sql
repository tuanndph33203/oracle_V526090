CREATE   PROCEDURE dbo.sp_GetPackageRevenue
    @FromDate DATE,
    @ToDate DATE,
    @Branchs NVARCHAR(MAX) = NULL,
    @Customers NVARCHAR(MAX) = NULL,
    @TextSearch NVARCHAR(MAX) = NULL,
    @Page INT = 1,
    @Size INT = 20,
    @IsAll BIT = 0,
    @Type INT = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrimmedTextSearch NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@TextSearch)), N'');
    DECLARE @EffectivePage INT = CASE WHEN @IsAll = 1 THEN 1 WHEN ISNULL(@Page, 0) < 1 THEN 1 ELSE @Page END;
    DECLARE @RequestedSize INT = CASE WHEN ISNULL(@Size, 0) <= 0 THEN 60 ELSE @Size END;

    DECLARE @BranchFilter TABLE (Value NVARCHAR(200) PRIMARY KEY);
    DECLARE @CustomerFilter TABLE (Value NVARCHAR(200) PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@Branchs)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @BranchFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Branchs, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    IF NULLIF(LTRIM(RTRIM(@Customers)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @CustomerFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Customers, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    CREATE TABLE #FilteredCards
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        CustomerCode NVARCHAR(200) NULL,
        SeriNumberCard NVARCHAR(200) NULL,
        ServiceCode NVARCHAR(200) NULL,
        BranchCode NVARCHAR(200) NULL,
        AreaUsed INT NULL,
        Status NVARCHAR(200) NULL,
        UsedDate DATETIME NULL,
        PreOutQuantity DECIMAL(18, 2) NULL,
        PreClosingAmount DECIMAL(18, 2) NULL,
        RowNum INT NULL
    );

    CREATE TABLE #PagedCards
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        CustomerCode NVARCHAR(200) NULL,
        SeriNumberCard NVARCHAR(200) NULL,
        ServiceCode NVARCHAR(200) NULL,
        BranchCode NVARCHAR(200) NULL,
        AreaUsed INT NULL,
        Status NVARCHAR(200) NULL,
        UsedDate DATETIME NULL
    );

    IF @IsAll = 1
    BEGIN
        ;WITH ExcludedSerials AS
        (
            SELECT DISTINCT d.SeriCardNumber
            FROM PaymentCardUpdateDetail d
            INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
            WHERE u.Status = '0'
                AND d.SeriCardNumber IS NOT NULL
        ),
        RelevantSerials AS
        (
            SELECT DISTINCT t.CardSerialNumber
            FROM PaymentCardTransaction t
            WHERE t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
                AND t.CardSerialNumber IS NOT NULL
        ),
        FilteredCards AS
        (
            SELECT
                c.Id,
                c.CustomerCode,
                c.SeriNumberCard,
                c.ServiceCode,
                c.BranchCode,
                c.AreaUsed,
                c.Status,
                c.UsedDate
            FROM PaymentCard c
            WHERE c.SeriNumberCard IS NOT NULL
                AND ISNULL(TRY_CONVERT(INT, c.Status), 0) <> 1
                AND NULLIF(c.CustomerCode, '') IS NOT NULL
                AND EXISTS
                (
                    SELECT 1
                    FROM RelevantSerials rs
                    WHERE rs.CardSerialNumber = c.SeriNumberCard
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM ExcludedSerials es
                    WHERE es.SeriCardNumber = c.SeriNumberCard
                )
                AND
                (
                    NOT EXISTS (SELECT 1 FROM @BranchFilter)
                    OR EXISTS
                    (
                        SELECT 1
                        FROM @BranchFilter bf
                        WHERE bf.Value = c.BranchCode
                    )
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
                    OR c.SeriNumberCard LIKE N'%' + @TrimmedTextSearch + N'%'
                    OR c.CustomerCode LIKE N'%' + @TrimmedTextSearch + N'%'
                    OR c.ServiceCode LIKE N'%' + @TrimmedTextSearch + N'%'
                )
                AND
                (
                    ISNULL(@Type, 0) NOT IN (4, 5, 6, 7, 8, 9, 10)
                    OR (@Type = 4 AND c.CardType = 3 AND ISNULL(TRY_CONVERT(INT, c.Status), 0) NOT IN (0, 1))
                    OR (@Type = 5 AND c.CardType = 2)
                    OR (@Type = 6 AND c.CardType = 1)
                    OR (@Type = 7 AND EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardUpdateDetail d
                        INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
                        WHERE d.SeriCardNumber = c.SeriNumberCard
                            AND u.Status = '1'
                            AND CAST(u.DocumentDate AS DATE) >= @FromDate
                            AND CAST(u.DocumentDate AS DATE) <= @ToDate
                    ))
                    OR (@Type = 8 AND NOT EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardTransaction t
                        WHERE t.CardSerialNumber = c.SeriNumberCard
                            AND t.DocumentDate IS NOT NULL
                            AND CAST(t.DocumentDate AS DATE) >= DATEADD(MONTH, -12, @ToDate)
                    ))
                    OR (@Type = 9 AND EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardTransaction t
                        WHERE t.CardSerialNumber = c.SeriNumberCard
                            AND t.DocumentDate IS NOT NULL
                            AND CAST(t.DocumentDate AS DATE) > DATEADD(MONTH, -12, @ToDate)
                    ))
                    OR (@Type = 10 AND c.UsedDate IS NOT NULL AND CAST(c.UsedDate AS DATE) <> '1900-01-01' AND CAST(c.UsedDate AS DATE) < CAST(GETDATE() AS DATE))
                )
        )
        INSERT INTO #FilteredCards
        (
            Id,
            CustomerCode,
            SeriNumberCard,
            ServiceCode,
            BranchCode,
            AreaUsed,
            Status,
            UsedDate,
            PreOutQuantity,
            PreClosingAmount,
            RowNum
        )
            SELECT
            fc.Id,
            fc.CustomerCode,
            fc.SeriNumberCard,
            fc.ServiceCode,
            fc.BranchCode,
            fc.AreaUsed,
            CASE
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 5 THEN N'Trả lại Hủy'
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 4 THEN N'Khóa thẻ'
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 6 THEN N'Không sử dụng'
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) IN (2, 3)
                         AND fc.UsedDate IS NOT NULL
                         AND CAST(fc.UsedDate AS DATE) < CAST(GETDATE() AS DATE) THEN N'Hết hạn sử dụng'
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 3
                         AND (fc.UsedDate IS NULL OR CAST(fc.UsedDate AS DATE) >= CAST(GETDATE() AS DATE)) THEN N'Đang sử dụng'
                    WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 2 THEN N'Xuất bán'
                    ELSE CAST(fc.Status AS NVARCHAR(200))
            END AS Status,
            fc.UsedDate,
            ISNULL(preAgg.OutQuantity, 0) AS PreOutQuantity,
            ISNULL(preClosing.EndingBalance, 0) AS PreClosingAmount,
            NULL AS RowNum
        FROM FilteredCards fc
        OUTER APPLY
        (
            SELECT
                SUM(
                    CASE
                        WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.QuantityOut
                        WHEN ISNULL(@Type, 0) = 1 AND ISNULL(t.AmountOut, 0) > 0 THEN t.QuantityOut
                        WHEN ISNULL(@Type, 0) = 2 THEN t.QuantityIn
                        ELSE CAST(0 AS DECIMAL(18, 2))
                    END
                ) AS OutQuantity
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) >= @FromDate
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
        ) preAgg
        OUTER APPLY
        (
            SELECT TOP (1) t.EndingBalance
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND t.DocumentDate =
                (
                    SELECT MAX(t1.DocumentDate)
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = fc.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) <= @ToDate
                )
            ORDER BY t.[Index] DESC
        ) preClosing;
    END
    ELSE
    BEGIN
        ;WITH ExcludedSerials AS
        (
            SELECT DISTINCT d.SeriCardNumber
            FROM PaymentCardUpdateDetail d
            INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
            WHERE u.Status = '0'
                AND d.SeriCardNumber IS NOT NULL
        ),
        RelevantSerials AS
        (
            SELECT DISTINCT t.CardSerialNumber
            FROM PaymentCardTransaction t
            WHERE t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
                AND t.CardSerialNumber IS NOT NULL
        ),
        FilteredCards AS
        (
            SELECT
                c.Id,
                c.CustomerCode,
                c.SeriNumberCard,
                c.ServiceCode,
                c.BranchCode,
                c.AreaUsed,
                c.Status,
                c.UsedDate
            FROM PaymentCard c
            WHERE c.SeriNumberCard IS NOT NULL
                AND ISNULL(TRY_CONVERT(INT, c.Status), 0) <> 1
                AND NULLIF(c.CustomerCode, '') IS NOT NULL
                AND EXISTS
                (
                    SELECT 1
                    FROM RelevantSerials rs
                    WHERE rs.CardSerialNumber = c.SeriNumberCard
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM ExcludedSerials es
                    WHERE es.SeriCardNumber = c.SeriNumberCard
                )
                AND
                (
                    NOT EXISTS (SELECT 1 FROM @BranchFilter)
                    OR EXISTS
                    (
                        SELECT 1
                        FROM @BranchFilter bf
                        WHERE bf.Value = c.BranchCode
                    )
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
                    OR c.SeriNumberCard LIKE N'%' + @TrimmedTextSearch + N'%'
                    OR c.CustomerCode LIKE N'%' + @TrimmedTextSearch + N'%'
                    OR c.ServiceCode LIKE N'%' + @TrimmedTextSearch + N'%'
                )
                AND
                (
                    ISNULL(@Type, 0) NOT IN (4, 5, 6, 7, 8, 9, 10)
                    OR (@Type = 4 AND c.CardType = 3 AND ISNULL(TRY_CONVERT(INT, c.Status), 0) NOT IN (0, 1))
                    OR (@Type = 5 AND c.CardType = 2)
                    OR (@Type = 6 AND c.CardType = 1)
                    OR (@Type = 7 AND EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardUpdateDetail d
                        INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
                        WHERE d.SeriCardNumber = c.SeriNumberCard
                            AND u.Status = '1'
                            AND CAST(u.DocumentDate AS DATE) >= @FromDate
                            AND CAST(u.DocumentDate AS DATE) <= @ToDate
                    ))
                    OR (@Type = 8 AND NOT EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardTransaction t
                        WHERE t.CardSerialNumber = c.SeriNumberCard
                            AND t.DocumentDate IS NOT NULL
                            AND CAST(t.DocumentDate AS DATE) >= DATEADD(MONTH, -12, @ToDate)
                    ))
                    OR (@Type = 9 AND EXISTS
                    (
                        SELECT 1
                        FROM PaymentCardTransaction t
                        WHERE t.CardSerialNumber = c.SeriNumberCard
                            AND t.DocumentDate IS NOT NULL
                            AND CAST(t.DocumentDate AS DATE) > DATEADD(MONTH, -12, @ToDate)
                    ))
                    OR (@Type = 10 AND c.UsedDate IS NOT NULL AND CAST(c.UsedDate AS DATE) <> '1900-01-01' AND CAST(c.UsedDate AS DATE) < CAST(GETDATE() AS DATE))
                )
        )
        INSERT INTO #FilteredCards
        (
            Id,
            CustomerCode,
            SeriNumberCard,
            ServiceCode,
            BranchCode,
            AreaUsed,
            Status,
            UsedDate,
            PreOutQuantity,
            PreClosingAmount,
            RowNum
        )
        SELECT
            fc.Id,
            fc.CustomerCode,
            fc.SeriNumberCard,
            fc.ServiceCode,
            fc.BranchCode,
            fc.AreaUsed,
            CASE
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 5 THEN N'Trả lại Hủy'
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 4 THEN N'Khóa thẻ'
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 6 THEN N'Không sử dụng'
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) IN (2, 3)
                     AND fc.UsedDate IS NOT NULL
                     AND CAST(fc.UsedDate AS DATE) < CAST(GETDATE() AS DATE) THEN N'Hết hạn sử dụng'
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 3
                     AND (fc.UsedDate IS NULL OR CAST(fc.UsedDate AS DATE) >= CAST(GETDATE() AS DATE)) THEN N'Đang sử dụng'
                WHEN ISNULL(TRY_CONVERT(INT, fc.Status), -1) = 2 THEN N'Xuất bán'
                ELSE CAST(fc.Status AS NVARCHAR(200))
            END AS Status,
            fc.UsedDate,
            ISNULL(preAgg.OutQuantity, 0) AS PreOutQuantity,
            ISNULL(preClosing.EndingBalance, 0) AS PreClosingAmount,
            ROW_NUMBER() OVER
            (
                ORDER BY ISNULL(preAgg.OutQuantity, 0) DESC, ISNULL(fc.SeriNumberCard, N'') DESC, ISNULL(fc.CustomerCode, N'') ASC
            ) AS RowNum
        FROM FilteredCards fc
        OUTER APPLY
        (
            SELECT
                SUM(
                    CASE
                        WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.QuantityOut
                        WHEN ISNULL(@Type, 0) = 1 AND ISNULL(t.AmountOut, 0) > 0 THEN t.QuantityOut
                        WHEN ISNULL(@Type, 0) = 2 THEN t.QuantityIn
                        ELSE CAST(0 AS DECIMAL(18, 2))
                    END
                ) AS OutQuantity
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) >= @FromDate
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
        ) preAgg
        OUTER APPLY
        (
            SELECT TOP (1) t.EndingBalance
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND t.DocumentDate =
                (
                    SELECT MAX(t1.DocumentDate)
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = fc.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) <= @ToDate
                )
            ORDER BY t.[Index] DESC
        ) preClosing;
    END

    IF ISNULL(@Type, 0) = 1
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) >= @FromDate
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
                AND ISNULL(t.AmountIn, 0) > 0
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 2
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) >= @FromDate
                AND CAST(t.DocumentDate AS DATE) <= @ToDate
                AND ISNULL(t.AmountOut, 0) > 0
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 3
    BEGIN
        DELETE FROM #FilteredCards WHERE ISNULL(PreClosingAmount, 0) <> 0;

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 4
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCard c
            WHERE c.Id = fc.Id
                AND c.CardType = 3
                AND ISNULL(TRY_CONVERT(INT, c.Status), 0) NOT IN (0, 1)
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 5
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCard c
            WHERE c.Id = fc.Id
                AND c.CardType = 2
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 6
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCard c
            WHERE c.Id = fc.Id
                AND c.CardType = 1
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 7
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCardUpdateDetail d
            INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
            WHERE d.SeriCardNumber = fc.SeriNumberCard
                AND u.Status = '1'
                AND CAST(u.DocumentDate AS DATE) >= @FromDate
                AND CAST(u.DocumentDate AS DATE) <= @ToDate
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 8
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE EXISTS
        (
            SELECT 1
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) >= DATEADD(MONTH, -12, @ToDate)
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 9
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCardTransaction t
            WHERE t.CardSerialNumber = fc.SeriNumberCard
                AND t.DocumentDate IS NOT NULL
                AND CAST(t.DocumentDate AS DATE) > DATEADD(MONTH, -12, @ToDate)
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    IF ISNULL(@Type, 0) = 10
    BEGIN
        DELETE fc
        FROM #FilteredCards fc
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM PaymentCard c
            WHERE c.Id = fc.Id
                AND c.UsedDate IS NOT NULL
                AND CAST(c.UsedDate AS DATE) <> '1900-01-01'
                AND CAST(c.UsedDate AS DATE) < CAST(GETDATE() AS DATE)
        );

        IF @IsAll = 0
        BEGIN
            ;WITH Renumbered AS
            (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY RowNum) AS NewRowNum
                FROM #FilteredCards
            )
            UPDATE fc
            SET fc.RowNum = r.NewRowNum
            FROM #FilteredCards fc
            INNER JOIN Renumbered r ON fc.Id = r.Id;
        END
    END

    SELECT @TotalCount = COUNT(1)
    FROM #FilteredCards;

    IF @IsAll = 1
    BEGIN
        INSERT INTO #PagedCards
        (
            Id,
            CustomerCode,
            SeriNumberCard,
            ServiceCode,
            BranchCode,
            AreaUsed,
            Status,
            UsedDate
        )
        SELECT
            fc.Id,
            fc.CustomerCode,
            fc.SeriNumberCard,
            fc.ServiceCode,
            fc.BranchCode,
            fc.AreaUsed,
            fc.Status,
            fc.UsedDate
        FROM #FilteredCards fc;
    END
    ELSE
    BEGIN
        INSERT INTO #PagedCards
        (
            Id,
            CustomerCode,
            SeriNumberCard,
            ServiceCode,
            BranchCode,
            AreaUsed,
            Status,
            UsedDate
        )
        SELECT
            fc.Id,
            fc.CustomerCode,
            fc.SeriNumberCard,
            fc.ServiceCode,
            fc.BranchCode,
            fc.AreaUsed,
            fc.Status,
            fc.UsedDate
        FROM #FilteredCards fc
        WHERE fc.RowNum BETWEEN ((@EffectivePage - 1) * @RequestedSize + 1) AND (@EffectivePage * @RequestedSize);
    END

    SELECT
        pc.CustomerCode,
        latest.CustomerName,
        pc.SeriNumberCard,
        pc.ServiceCode,
        p.Name AS ServiceName,
        pc.BranchCode AS UnitCode,

        CASE 
            WHEN ISNULL(pc.AreaUsed, 0) = 0 THEN CAST(0 AS DECIMAL(18, 2))
            ELSE ISNULL(opening.EndingQuantity, 0)
        END AS OpeningQuantity,

        ISNULL(opening.EndingBalance, 0) AS OpeningAmount,

        CASE 
            WHEN ISNULL(pc.AreaUsed, 0) = 0 THEN CAST(0 AS DECIMAL(18, 2))
            ELSE ISNULL(agg.InQuantity, 0)
        END AS InQuantity,

        ISNULL(agg.InAmount, 0) AS InAmount,

        ISNULL(agg.OutQuantity, 0) AS OutQuantity,

        ISNULL(agg.OutAmount, 0) AS OutAmount,

        CASE 
            WHEN ISNULL(pc.AreaUsed, 0) = 0 THEN CAST(0 AS DECIMAL(18, 2))
            ELSE ISNULL(closing.EndingQuantity, 0)
        END AS ClosingQuantity,

        ISNULL(closing.EndingBalance, 0) AS ClosingAmount,

        pc.Status,
        pc.UsedDate,
        latestDiscountBill.DiscountEvent,
        latestPromotionBill.PromotionEvent
    FROM #PagedCards pc
    OUTER APPLY
    (
        SELECT
            SUM(
                CASE
                    WHEN ISNULL(@Type, 0) = 2 THEN t.QuantityIn
                    WHEN ISNULL(@Type, 0) = 1 AND ISNULL(t.AmountIn, 0) > 0 THEN t.QuantityIn
                    WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.QuantityIn
                    ELSE CAST(0 AS DECIMAL(18, 2))
                END
            ) AS InQuantity,
            SUM(
                CASE
                    WHEN ISNULL(@Type, 0) = 2 THEN t.AmountIn
                    WHEN ISNULL(@Type, 0) = 1 AND ISNULL(t.AmountIn, 0) > 0 THEN t.AmountIn
                    WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.AmountIn
                    ELSE CAST(0 AS DECIMAL(18, 2))
                END
            ) AS InAmount,
            SUM(
                CASE
                    WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.QuantityOut
                    WHEN ISNULL(@Type, 0) IN (1, 2) AND ISNULL(t.AmountOut, 0) > 0 THEN t.QuantityOut
                    ELSE CAST(0 AS DECIMAL(18, 2))
                END
            ) AS OutQuantity,
            SUM(
                CASE
                    WHEN ISNULL(@Type, 0) NOT IN (1, 2) THEN t.AmountOut
                    WHEN ISNULL(@Type, 0) IN (1, 2) AND ISNULL(t.AmountOut, 0) > 0 THEN t.AmountOut
                    ELSE CAST(0 AS DECIMAL(18, 2))
                END
            ) AS OutAmount
        FROM PaymentCardTransaction t
        WHERE t.CardSerialNumber = pc.SeriNumberCard
            AND t.DocumentDate IS NOT NULL
            AND CAST(t.DocumentDate AS DATE) >= @FromDate
            AND CAST(t.DocumentDate AS DATE) <= @ToDate
    ) agg
    OUTER APPLY
    (
        SELECT TOP (1)
            t.EndingQuantity,
            t.EndingBalance
        FROM PaymentCardTransaction t
        WHERE t.CardSerialNumber = pc.SeriNumberCard
            AND t.DocumentDate IS NOT NULL
            AND t.DocumentDate =
            (
                SELECT MAX(t1.DocumentDate)
                FROM PaymentCardTransaction t1
                WHERE t1.CardSerialNumber = pc.SeriNumberCard
                    AND t1.DocumentDate IS NOT NULL
                    AND CAST(t1.DocumentDate AS DATE) < @FromDate
            )
        ORDER BY t.[Index] DESC
    ) opening
    OUTER APPLY
    (
        SELECT TOP (1)
            t.EndingQuantity,
            t.EndingBalance
        FROM PaymentCardTransaction t
        WHERE t.CardSerialNumber = pc.SeriNumberCard
            AND t.DocumentDate IS NOT NULL
            AND t.DocumentDate =
            (
                SELECT MAX(t1.DocumentDate)
                FROM PaymentCardTransaction t1
                WHERE t1.CardSerialNumber = pc.SeriNumberCard
                    AND t1.DocumentDate IS NOT NULL
                    AND CAST(t1.DocumentDate AS DATE) <= @ToDate
            )
        ORDER BY t.[Index] DESC
    ) closing
    OUTER APPLY
    (
        SELECT TOP (1)
            t.CustomerName
        FROM PaymentCardTransaction t
        WHERE t.CardSerialNumber = pc.SeriNumberCard
            AND t.DocumentDate IS NOT NULL
            AND CAST(t.DocumentDate AS DATE) <= @ToDate
        ORDER BY t.DocumentDate DESC, t.DocumentNumber DESC
    ) latest
    OUTER APPLY
    (
        SELECT TOP (1)
            d.EventDiscountCode AS DiscountEvent
        FROM SaleBillDetail d
        INNER JOIN SaleBill b ON d.STTRec = b.STTRec
        WHERE d.SeriCardNumber = pc.SeriNumberCard
            AND b.DocumentDate IS NOT NULL
            AND CAST(b.DocumentDate AS DATE) <= @ToDate
            AND NULLIF(LTRIM(RTRIM(d.EventDiscountCode)), N'') IS NOT NULL
        ORDER BY b.DocumentDate DESC
    ) latestDiscountBill
    OUTER APPLY
    (
        SELECT TOP (1)
            d.EventCode AS PromotionEvent
        FROM SaleBillDetail d
        INNER JOIN SaleBill b ON d.STTRec = b.STTRec
        WHERE d.SeriCardNumber = pc.SeriNumberCard
            AND b.DocumentDate IS NOT NULL
            AND CAST(b.DocumentDate AS DATE) <= @ToDate
            AND NULLIF(LTRIM(RTRIM(d.EventCode)), N'') IS NOT NULL
        ORDER BY b.DocumentDate DESC
    ) latestPromotionBill
    LEFT JOIN Product p ON p.Code = pc.ServiceCode
    ORDER BY OutQuantity DESC;

    DROP TABLE #FilteredCards;
    DROP TABLE #PagedCards;
END;
