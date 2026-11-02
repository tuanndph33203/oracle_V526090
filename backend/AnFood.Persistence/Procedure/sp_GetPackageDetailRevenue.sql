CREATE OR ALTER PROCEDURE [dbo].[sp_GetPackageDetailRevenue]
    @Branchs        NVARCHAR(MAX)   = NULL,
    @Customers      NVARCHAR(500)   = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL,
    @Page           INT             = 1,
    @Size           INT             = 20,
    @Type           INT             = NULL,
    @TotalCount     INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page <= 0   SET @Page = 1;
    IF @Size <= 0   SET @Size = 20;

    -- Split @Branchs into a temp table
    DECLARE @BranchTable TABLE (BranchCode NVARCHAR(50));
    IF @Branchs IS NOT NULL AND @Branchs <> ''
    BEGIN
        INSERT INTO @BranchTable (BranchCode)
        SELECT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@Branchs, ',');
    END

    ;WITH FilteredData AS
    (
        SELECT
            t.Id,
            t.[Index],
            t.BranchCode,
            t.DocumentType,
            t.DocumentDate,
            t.DocumentNumber,
            t.CustomerCode,
            t.CustomerName,
            t.ServiceCode,
            t.ServiceName,
            t.CardSerialNumber,
            t.[Description],
            t.QuantityIn,
            t.QuantityOut,
            t.AmountIn,
            t.AmountOut,
            t.EndingQuantity,
            t.EndingBalance
        FROM dbo.PaymentCardTransaction t
        INNER JOIN dbo.PaymentCard c ON c.SeriNumberCard = t.CardSerialNumber
        WHERE t.CardSerialNumber IS NOT NULL
          AND (@Branchs IS NULL OR @Branchs = '' OR t.BranchCode IN (SELECT BranchCode FROM @BranchTable))
          AND (@Customers IS NULL OR @Customers = '' OR t.CustomerCode = @Customers)
          AND (@FromDate IS NULL OR (t.DocumentDate IS NOT NULL AND CAST(t.DocumentDate AS DATE) >= @FromDate))
          AND (@ToDate   IS NULL OR (t.DocumentDate IS NOT NULL AND CAST(t.DocumentDate AS DATE) <= @ToDate))
          AND (
                ISNULL(@Type, 0) NOT IN (1, 2)
                OR (@Type = 1 AND t.AmountIn > 0)
                OR (@Type = 2 AND t.AmountOut > 0)
              )
          AND (
                ISNULL(@Type, 0) NOT IN (4, 5, 6, 7, 8, 9, 10)
                OR (@Type = 4 AND c.CardType = 3 AND ISNULL(c.Status, 0) NOT IN (0, 1))
                OR (@Type = 5 AND c.CardType = 2)
                OR (@Type = 6 AND c.CardType = 1)
                OR (@Type = 7 AND EXISTS
                (
                    SELECT 1
                    FROM PaymentCardUpdateDetail d
                    INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
                    WHERE d.SeriCardNumber = c.SeriNumberCard
                        AND u.Status = '1'
                ))
                OR (@Type = 8 AND NOT EXISTS
                (
                    SELECT 1
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = c.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) >= DATEADD(MONTH, -12, @ToDate)
                ))
                OR (@Type = 9 AND EXISTS
                (
                    SELECT 1
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = c.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) > DATEADD(MONTH, -12, @ToDate)
                ))
                OR (@Type = 10 AND c.UsedDate IS NOT NULL AND CAST(c.UsedDate AS DATE) <> '1900-01-01' AND CAST(c.UsedDate AS DATE) < CAST(GETDATE() AS DATE))
              )
          AND (
                @TextSearch IS NULL OR @TextSearch = ''
                OR t.DocumentNumber   LIKE '%' + @TextSearch + '%'
                OR t.CardSerialNumber LIKE '%' + @TextSearch + '%'
                OR t.CustomerCode     LIKE '%' + @TextSearch + '%'
                OR t.CustomerName     LIKE '%' + @TextSearch + '%'
                OR t.ServiceCode      LIKE '%' + @TextSearch + '%'
                OR t.[Description]    LIKE '%' + @TextSearch + '%'
              )
    )
    SELECT @TotalCount = COUNT(*) FROM FilteredData;

    ;WITH FilteredData AS
    (
        SELECT
            t.Id,
            t.[Index],
            t.BranchCode,
            t.DocumentType,
            t.DocumentDate,
            t.DocumentNumber,
            t.CustomerCode,
            t.CustomerName,
            t.ServiceCode,
            t.ServiceName,
            t.CardSerialNumber,
            t.[Description],
            t.QuantityIn,
            t.QuantityOut,
            t.AmountIn,
            t.AmountOut,
            t.EndingQuantity,
            t.EndingBalance
        FROM dbo.PaymentCardTransaction t
        INNER JOIN dbo.PaymentCard c ON c.SeriNumberCard = t.CardSerialNumber
        WHERE t.CardSerialNumber IS NOT NULL
          AND (@Branchs IS NULL OR @Branchs = '' OR t.BranchCode IN (SELECT BranchCode FROM @BranchTable))
          AND (@Customers IS NULL OR @Customers = '' OR t.CustomerCode = @Customers)
          AND (@FromDate IS NULL OR (t.DocumentDate IS NOT NULL AND CAST(t.DocumentDate AS DATE) >= @FromDate))
          AND (@ToDate   IS NULL OR (t.DocumentDate IS NOT NULL AND CAST(t.DocumentDate AS DATE) <= @ToDate))
          AND (
                ISNULL(@Type, 0) NOT IN (1, 2)
                OR (@Type = 1 AND t.AmountIn > 0)
                OR (@Type = 2 AND t.AmountOut > 0)
              )
          AND (
                ISNULL(@Type, 0) NOT IN (4, 5, 6, 7, 8, 9, 10)
                OR (@Type = 4 AND c.CardType = 3 AND ISNULL(c.Status, 0) NOT IN (0, 1))
                OR (@Type = 5 AND c.CardType = 2)
                OR (@Type = 6 AND c.CardType = 1)
                OR (@Type = 7 AND EXISTS
                (
                    SELECT 1
                    FROM PaymentCardUpdateDetail d
                    INNER JOIN PaymentCardUpdate u ON d.STTRec = u.STTRec
                    WHERE d.SeriCardNumber = c.SeriNumberCard
                        AND u.Status = '1'
                ))
                OR (@Type = 8 AND NOT EXISTS
                (
                    SELECT 1
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = c.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) >= DATEADD(MONTH, -12, @ToDate)
                ))
                OR (@Type = 9 AND EXISTS
                (
                    SELECT 1
                    FROM PaymentCardTransaction t1
                    WHERE t1.CardSerialNumber = c.SeriNumberCard
                        AND t1.DocumentDate IS NOT NULL
                        AND CAST(t1.DocumentDate AS DATE) > DATEADD(MONTH, -12, @ToDate)
                ))
                OR (@Type = 10 AND c.UsedDate IS NOT NULL AND CAST(c.UsedDate AS DATE) <> '1900-01-01' AND CAST(c.UsedDate AS DATE) < CAST(GETDATE() AS DATE))
              )
          AND (
                @TextSearch IS NULL OR @TextSearch = ''
                OR t.DocumentNumber   LIKE '%' + @TextSearch + '%'
                OR t.CardSerialNumber LIKE '%' + @TextSearch + '%'
                OR t.CustomerCode     LIKE '%' + @TextSearch + '%'
                OR t.CustomerName     LIKE '%' + @TextSearch + '%'
                OR t.ServiceCode      LIKE '%' + @TextSearch + '%'
                OR t.[Description]    LIKE '%' + @TextSearch + '%'
              )
    )
    SELECT
        BranchCode,
        DocumentType,
        DocumentDate,
        DocumentNumber,
        CustomerCode,
        CustomerName,
        ServiceCode,
        ServiceName,
        CardSerialNumber AS SeriNumberCard,
        [Description],
        QuantityIn,
        QuantityOut,
        AmountIn,
        AmountOut,
        EndingQuantity,
        EndingBalance
    FROM FilteredData
    ORDER BY [Index] ASC
    OFFSET (@Page - 1) * @Size ROWS
    FETCH NEXT @Size ROWS ONLY;
END
