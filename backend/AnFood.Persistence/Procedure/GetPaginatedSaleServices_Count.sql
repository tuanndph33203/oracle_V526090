CREATE PROCEDURE GetPaginatedSaleServices_Count
    @CustomerCode       VARCHAR(50)  = NULL,
    @SaleEmployeeCode   VARCHAR(50)  = NULL,
    @SalerCode          VARCHAR(100) = NULL,
    @EmployeeCode       VARCHAR(50)  = NULL,
    @ServiceCode        VARCHAR(100) = NULL,
    @Status             VARCHAR(100) = NULL,
    @BranchCode         VARCHAR(100) = NULL,
    @TextSearch         VARCHAR(100) = NULL,
    @FromDate           DATETIME     = NULL,
    @ToDate             DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1)
    FROM SaleService ss
    WHERE
        (
            @CustomerCode IS NULL
            OR ',' + @CustomerCode + ',' LIKE '%,' + ss.CustomerCode + ',%'
        )
        AND (
            @SaleEmployeeCode IS NULL
            OR ',' + @SaleEmployeeCode + ',' LIKE '%,' + ss.CashierCode + ',%'
        )
        AND (
            @SalerCode IS NULL
            OR ',' + @SalerCode + ',' LIKE '%,' + ss.SalerCode + ',%'
        )
        AND (
            @BranchCode IS NULL
            OR ',' + @BranchCode + ',' LIKE '%,' + ss.BranchCode + ',%'
        )
        AND ss.Status <> '10'
        AND (
            @Status IS NULL
            OR ',' + @Status + ',' LIKE '%,' + ss.Status + ',%'
        )
        AND (
            @ServiceCode IS NULL
            OR EXISTS (
                SELECT 1
                FROM SaleServiceDetail sd
                WHERE sd.STTRec = ss.STTRec
                  AND ',' + @ServiceCode + ',' LIKE '%,' + sd.ServiceCode + ',%'
            )
        )
        AND (
            @EmployeeCode IS NULL
            OR EXISTS (
                SELECT 1
                FROM SaleServiceDetail sd
                WHERE sd.STTRec = ss.STTRec
                  AND (
                        ',' + @EmployeeCode + ',' LIKE '%,' + sd.TherapistCode1 + ',%'
                     OR ',' + @EmployeeCode + ',' LIKE '%,' + sd.TherapistCode2 + ',%'
                     OR ',' + @EmployeeCode + ',' LIKE '%,' + sd.TherapistCode3 + ',%'
                  )
            )
        )
        AND (
            @TextSearch IS NULL
            OR ss.DocumentNumber LIKE '%' + @TextSearch + '%'
        )
        AND (
            @FromDate IS NULL
            OR ss.DocumentDate >= @FromDate
        )
        AND (
            @ToDate IS NULL
            OR ss.DocumentDate < DATEADD(DAY, 1, @ToDate)
        );
END;
