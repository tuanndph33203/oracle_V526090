-- =============================================
-- Stored Procedure: sp_GetSaleDebtRevenue
-- Description: Get Sale Debt Revenue data with pagination
-- Note: Mỗi dòng PaymentCardUpdate = 1 dòng kết quả, Status = '0'
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetSaleDebtRevenue]
    @CustomerCode   NVARCHAR(50)    = NULL,
    @BranchCode     NVARCHAR(50)    = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL,
    @Page           INT             = 1,
    @Size           INT             = 20,
    
    -- Dynamic comparison filters
    @OpeningBalance         NVARCHAR(20)    = NULL,
    @OpeningBalanceValue    DECIMAL(18,2)   = NULL,
    @Debit                  NVARCHAR(20)    = NULL,
    @DebitValue             DECIMAL(18,2)   = NULL,
    @Credit                 NVARCHAR(20)    = NULL,
    @CreditValue            DECIMAL(18,2)   = NULL,
    @ClosingBalance         NVARCHAR(20)    = NULL,
    @ClosingBalanceValue    DECIMAL(18,2)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate pagination parameters
    IF @Page <= 0 SET @Page = 1;
    IF @Size <= 0 SET @Size = 20;

    -- CTE với calculated balance fields
    ;WITH CalculatedData AS (
        SELECT
            pcu.STTRec,
            pcu.DocumentDate,
            pcu.DocumentNumber,
            pcu.CustomerCode,
            pcu.CustomerName,
            pcu.SalerCode,
            pcu.CreatedByUserCode,
            pcu.CreatedByUserName,
            pcu.Notes,
            c.CustomerCode AS CustCode,
            c.CustomerName AS CustName,
            c.PhoneNumber2,
            c.Address,
            e1.Name,
            -- Calculate balance fields
            ISNULL((SELECT SUM(d.TotalPrice) FROM dbo.PaymentCardUpdateDetail d WHERE d.STTRec = pcu.STTRec), 0) AS CalcOpeningBalance,
            0.00 AS CalcDebit,
            0.00 AS CalcCredit,
            ISNULL((SELECT SUM(d.TotalPrice) FROM dbo.PaymentCardUpdateDetail d WHERE d.STTRec = pcu.STTRec), 0) AS CalcClosingBalance
        FROM dbo.PaymentCardUpdate pcu
        LEFT JOIN dbo.Customer c ON c.CustomerCode = pcu.CustomerCode
        LEFT JOIN dbo.Employee e1 ON e1.Code = pcu.SalerCode OR e1.Code = pcu.CreatedByUserCode
        WHERE 
            pcu.Status = '0'
            AND (@CustomerCode IS NULL OR @CustomerCode = '' OR pcu.CustomerCode = @CustomerCode)
            AND (@BranchCode IS NULL OR @BranchCode = '' 
                 OR pcu.BranchCode IN (SELECT TRIM(value) FROM STRING_SPLIT(@BranchCode, ',')))
            AND (@FromDate IS NULL OR CAST(pcu.DocumentDate AS DATE) >= @FromDate)
            AND (@ToDate IS NULL OR CAST(pcu.DocumentDate AS DATE) <= @ToDate)
            AND (
                @TextSearch IS NULL OR @TextSearch = ''
                OR pcu.DocumentNumber LIKE '%' + @TextSearch + '%'
                OR pcu.CustomerCode LIKE '%' + @TextSearch + '%'
                OR pcu.CustomerName LIKE '%' + @TextSearch + '%'
                OR c.PhoneNumber2 LIKE '%' + @TextSearch + '%'
                OR pcu.Notes LIKE '%' + @TextSearch + '%'
                OR EXISTS (
                    SELECT 1 FROM dbo.PaymentCardUpdateDetail d
                    WHERE d.STTRec = pcu.STTRec 
                    AND d.SeriCardNumber LIKE '%' + @TextSearch + '%'
                )
            )
    ),
    -- CTE để lọc dữ liệu với dynamic comparison
    FilteredData AS (
        SELECT * FROM CalculatedData cd
        WHERE 1=1
            -- OpeningBalance filter
            AND (
                @OpeningBalance IS NULL OR @OpeningBalanceValue IS NULL
                OR (@OpeningBalance = 'EQUAL' AND cd.CalcOpeningBalance = @OpeningBalanceValue)
                OR (@OpeningBalance = 'NOT_EQUAL' AND cd.CalcOpeningBalance != @OpeningBalanceValue)
                OR (@OpeningBalance = 'GREATER' AND cd.CalcOpeningBalance > @OpeningBalanceValue)
                OR (@OpeningBalance = 'LESS' AND cd.CalcOpeningBalance < @OpeningBalanceValue)
                OR (@OpeningBalance = 'GREATER_OR_EQUAL' AND cd.CalcOpeningBalance >= @OpeningBalanceValue)
                OR (@OpeningBalance = 'LESS_OR_EQUAL' AND cd.CalcOpeningBalance <= @OpeningBalanceValue)
            )
            -- Debit filter
            AND (
                @Debit IS NULL OR @DebitValue IS NULL
                OR (@Debit = 'EQUAL' AND cd.CalcDebit = @DebitValue)
                OR (@Debit = 'NOT_EQUAL' AND cd.CalcDebit != @DebitValue)
                OR (@Debit = 'GREATER' AND cd.CalcDebit > @DebitValue)
                OR (@Debit = 'LESS' AND cd.CalcDebit < @DebitValue)
                OR (@Debit = 'GREATER_OR_EQUAL' AND cd.CalcDebit >= @DebitValue)
                OR (@Debit = 'LESS_OR_EQUAL' AND cd.CalcDebit <= @DebitValue)
            )
            -- Credit filter
            AND (
                @Credit IS NULL OR @CreditValue IS NULL
                OR (@Credit = 'EQUAL' AND cd.CalcCredit = @CreditValue)
                OR (@Credit = 'NOT_EQUAL' AND cd.CalcCredit != @CreditValue)
                OR (@Credit = 'GREATER' AND cd.CalcCredit > @CreditValue)
                OR (@Credit = 'LESS' AND cd.CalcCredit < @CreditValue)
                OR (@Credit = 'GREATER_OR_EQUAL' AND cd.CalcCredit >= @CreditValue)
                OR (@Credit = 'LESS_OR_EQUAL' AND cd.CalcCredit <= @CreditValue)
            )
            -- ClosingBalance filter
            AND (
                @ClosingBalance IS NULL OR @ClosingBalanceValue IS NULL
                OR (@ClosingBalance = 'EQUAL' AND cd.CalcClosingBalance = @ClosingBalanceValue)
                OR (@ClosingBalance = 'NOT_EQUAL' AND cd.CalcClosingBalance != @ClosingBalanceValue)
                OR (@ClosingBalance = 'GREATER' AND cd.CalcClosingBalance > @ClosingBalanceValue)
                OR (@ClosingBalance = 'LESS' AND cd.CalcClosingBalance < @ClosingBalanceValue)
                OR (@ClosingBalance = 'GREATER_OR_EQUAL' AND cd.CalcClosingBalance >= @ClosingBalanceValue)
                OR (@ClosingBalance = 'LESS_OR_EQUAL' AND cd.CalcClosingBalance <= @ClosingBalanceValue)
            )
    )
    -- Get paginated results
    SELECT
        ROW_NUMBER() OVER (ORDER BY fd.DocumentDate DESC, fd.DocumentNumber DESC) AS Stt,
        ISNULL(fd.CustCode, fd.CustomerCode) AS CustomerCode,
        ISNULL(fd.CustName, fd.CustomerName) AS CustomerName,
        ISNULL(fd.PhoneNumber2, '') AS PhoneNumber,
        ISNULL(fd.Address, '') AS Address,
        
        fd.CalcOpeningBalance AS OpeningBalance,
        fd.CalcDebit AS Debit,
        fd.CalcCredit AS Credit,
        fd.CalcClosingBalance AS ClosingBalance,
        
        -- Số Seri thẻ PK (lấy từ detail, gộp lại)
        STUFF((
            SELECT ', ' + d.SeriCardNumber
            FROM dbo.PaymentCardUpdateDetail d
            WHERE d.STTRec = fd.STTRec AND d.SeriCardNumber IS NOT NULL
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS SerialNumber,
        
        -- Mã dịch vụ (lấy từ detail, gộp lại)
        STUFF((
            SELECT DISTINCT ', ' + d.ServiceCode
            FROM dbo.PaymentCardUpdateDetail d
            WHERE d.STTRec = fd.STTRec AND d.ServiceCode IS NOT NULL
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ServiceCode,
        
        -- Tên dịch vụ (lấy từ detail join service, gộp lại)
        STUFF((
            SELECT DISTINCT ', ' + s.Name
            FROM dbo.PaymentCardUpdateDetail d
            LEFT JOIN dbo.Product s ON s.Code = d.ServiceCode
            WHERE d.STTRec = fd.STTRec AND s.Name IS NOT NULL
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ServiceName,
        
        -- Mã NV1 (Nhân viên - từ SalerCode hoặc CreatedBy)
        ISNULL(fd.SalerCode, fd.CreatedByUserCode) AS EmployeeCode1,
        
        -- Tên NV1
        ISNULL(fd.Name, fd.CreatedByUserName) AS EmployeeName1,
        
        -- Mã NV2 (nếu có nhiều nhân viên tham gia)
        NULL AS EmployeeCode2,
        
        -- Tên NV2
        NULL AS EmployeeName2
        
    FROM FilteredData fd
    ORDER BY 
        fd.DocumentDate DESC,
        fd.DocumentNumber DESC
    OFFSET (@Page - 1) * @Size ROWS
    FETCH NEXT @Size ROWS ONLY;
    
END
