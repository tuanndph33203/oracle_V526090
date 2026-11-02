CREATE OR ALTER PROCEDURE [dbo].[sp_CustomerPointRevenue_Count]
    @CustomerCode   NVARCHAR(50)    = NULL,
    @BranchCode     NVARCHAR(500)   = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH EligibleCustomers AS
    (
        SELECT LTRIM(RTRIM(c.CustomerCode)) AS CustomerCode
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
    )
    SELECT COUNT(1)
    FROM EligibleCustomers;
END;
