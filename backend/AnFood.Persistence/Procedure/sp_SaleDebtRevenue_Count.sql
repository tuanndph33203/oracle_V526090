-- =============================================
-- Stored Procedure: sp_SaleDebtRevenue_Count
-- Description: Count total records for Sale Debt Revenue with filters
-- Note: Used for pagination - returns total count only
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_SaleDebtRevenue_Count]
    @CustomerCode   NVARCHAR(50)    = NULL,
    @BranchCode     NVARCHAR(50)    = NULL,
    @FromDate       DATE            = NULL,
    @ToDate         DATE            = NULL,
    @TextSearch     NVARCHAR(500)   = NULL,
    
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

    -- CTE với calculated balance fields (minimal columns for count)
    ;WITH CalculatedData AS (
        SELECT
            pcu.STTRec,
            -- Calculate balance fields
            ISNULL((SELECT SUM(d.TotalPrice) FROM dbo.PaymentCardUpdateDetail d WHERE d.STTRec = pcu.STTRec), 0) AS CalcOpeningBalance,
            0.00 AS CalcDebit,
            0.00 AS CalcCredit,
            ISNULL((SELECT SUM(d.TotalPrice) FROM dbo.PaymentCardUpdateDetail d WHERE d.STTRec = pcu.STTRec), 0) AS CalcClosingBalance
        FROM dbo.PaymentCardUpdate pcu
        LEFT JOIN dbo.Customer c ON c.CustomerCode = pcu.CustomerCode
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
        SELECT STTRec FROM CalculatedData cd
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
    -- Return total count as scalar
    SELECT COUNT(*) FROM FilteredData;
    
END
