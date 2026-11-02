CREATE PROCEDURE GetPaginatedSaleServices
    @Page               INT = 1,
    @Size               INT = 20,
    @CustomerCode       VARCHAR(50)  = NULL,
    @SaleEmployeeCode   VARCHAR(50)  = NULL,
    @SalerCode          VARCHAR(100) = NULL,
    @EmployeeCode       VARCHAR(50)  = NULL,
    @ServiceCode        VARCHAR(100) = NULL,
    @Status             VARCHAR(100) = NULL,
    @BranchCode         VARCHAR(100) = NULL,
    @TextSearch			VARCHAR(100) = NULL,
    @FromDate DATETIME = NULL,
	@ToDate   DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Skip INT = (@Page - 1) * @Size;
    DECLARE @TotalAllRecords INT;

    SELECT 
        @TotalAllRecords = COUNT(1)
    FROM SaleService;

    SELECT
        ss.Id,
        ss.DocumentDate,
        ss.STTRec,
        ss.DocumentCode,
        ss.DocumentNumber,
        ss.Notes,
        ss.IdentityCard,
        ss.CountPackage,
        ss.TotalMoneyPackage,
        ss.DiscountMoneyPackage,
        ROUND(ss.TotalMoneyPay, 0) AS TotalMoneyPay,
        ss.Status,
        ss.CreatedOnDate,
        ss.Vat,
		CASE 
		    WHEN EXISTS (
		        SELECT 1
		        FROM SaleServiceDetail sd
		        WHERE sd.STTRec = ss.STTRec
		          AND sd.IsReturn = 1
		    ) 
		    THEN CAST(0 AS BIT)
		    ELSE CAST(1 AS BIT)
		END AS IsDelete,
        emp.Id   AS CashierId,
        emp.Code AS CashierCode,
        emp.Name AS CashierName,

        br.Id   AS BranchId,
        br.Code AS BranchCode,
        br.Name AS BranchName,

        cus.Id            AS CustomerId,
        cus.CustomerCode,
        COALESCE(ss.CustomerName, cus.CustomerName) AS CustomerName,
        cus.PhoneNumber2,

        @TotalAllRecords AS TotalAllRecords,

        -- ListService
        (
            SELECT STRING_AGG(x.ServiceName, ', ')
            FROM (
                SELECT DISTINCT serv.Name AS ServiceName
                FROM SaleServiceDetail sd
                LEFT JOIN Product serv 
                    ON sd.ServiceCode = serv.Code
                WHERE sd.STTRec = ss.STTRec
                  AND serv.Name IS NOT NULL
            ) x
        ) AS ListService,

        -- ListRoom
        (
            SELECT STRING_AGG(x.RoomName, ', ')
            FROM (
                SELECT DISTINCT rm.Name AS RoomName
                FROM SaleServiceDetail sd
                LEFT JOIN Room rm 
                    ON sd.RoomCode = rm.Code
                WHERE sd.STTRec = ss.STTRec
                  AND rm.Name IS NOT NULL
            ) x
        ) AS ListRoom

    FROM (
        SELECT
            ss.*
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
			)
        ORDER BY ss.CreatedOnDate DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Size ROWS ONLY
    ) ss
    LEFT JOIN Employee emp
        ON ss.CashierCode = emp.Code
    LEFT JOIN Branchs br
        ON ss.BranchCode = br.Code
    LEFT JOIN Customer cus
        ON ss.CustomerCode = cus.CustomerCode
    ORDER BY ss.CreatedOnDate DESC;
END;
