CREATE OR ALTER PROCEDURE dbo.sp_GetDetailedRevenueReport
    @BranchID VARCHAR(50) = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- CTE to fetch matching Paid orders (Invoices)
    WITH FilteredOrders AS (
        SELECT 
            o.Id,
            o.OrderCode,
            o.OrderDate,
            o.OpenTime,
            o.PaymentTime,
            o.TotalAmount,
            o.SubTotal AS PreTaxAmount,
            o.VatAmount AS TaxAmount,
            o.DiscountAmount AS DiscountPreTaxAmount,
            o.CustomerName,
            o.WaiterName AS WaiterName,
            o.CashierUserName AS CashierName,
            o.ServiceType,
            o.FloorName,
            o.CustomerCount,
            o.Note,
            s.ShiftName AS ShiftName
        FROM dbo.SalesOrder o
        LEFT JOIN dbo.Shift s ON o.BranchCode = s.BranchCode 
                             AND o.CashierUserId = s.CashierId
                             AND o.OrderDate >= s.OpenTime 
                             AND (s.CloseTime IS NULL OR o.OrderDate <= s.CloseTime)
        WHERE 
            o.Status = 'Paid'
            AND (ISNULL(@BranchID, '') = '' OR o.BranchCode = @BranchID)
            AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
            AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE))
    ),
    PaginatedOrders AS (
        SELECT 
            Id,
            OrderCode,
            OrderDate,
            OpenTime,
            PaymentTime,
            TotalAmount,
            PreTaxAmount,
            TaxAmount,
            DiscountPreTaxAmount,
            CustomerName,
            WaiterName,
            CashierName,
            ServiceType,
            FloorName,
            CustomerCount,
            Note,
            ShiftName
        FROM FilteredOrders
        ORDER BY OrderDate DESC, ISNULL(PaymentTime, OpenTime) DESC, OrderCode DESC
        OFFSET (@PageNumber - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY
    )
    SELECT * INTO #TempOrders FROM PaginatedOrders;

    -- Result Set 1: Master Invoices on current page
    SELECT 
        o.Id AS OrderId,
        o.OrderCode,
        o.OrderDate,
        o.OpenTime,
        o.PaymentTime,
        o.TotalAmount,
        o.PreTaxAmount,
        o.TaxAmount,
        o.DiscountPreTaxAmount,
        o.CustomerName,
        o.WaiterName AS ServerName,
        o.CashierName,
        o.ShiftName,
        o.ServiceType,
        o.FloorName,
        o.CustomerCount,
        o.Note,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND p.PaymentType = 'Cash'), 0) AS CashAmount,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND p.PaymentType IN ('Transfer', 'QR', 'Bank', 'CreditCard', 'Card')), 0) AS TransferAmount,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND (p.PaymentType LIKE '%Voucher%' OR p.PaymentType LIKE '%Khuyến mãi%' OR p.PaymentType = 'Discount')), 0) AS VoucherAmount,
        -- Aggregate tables connected to this order
        ISNULL((
            SELECT STRING_AGG(st.TableName, ' + ') WITHIN GROUP (ORDER BY st.TableName)
            FROM dbo.SalesOrderTable st
            WHERE st.OrderId = o.Id
        ), N'Mang về/Giao hàng') AS TableName
    FROM #TempOrders o
    ORDER BY o.OrderDate DESC, ISNULL(o.PaymentTime, o.OpenTime) DESC, o.OrderCode DESC;

    -- Result Set 2: Detail items of the invoices on current page
    SELECT 
        oi.OrderId,
        oi.ItemCode,
        oi.ItemName,
        COALESCE(u.Name, NULLIF(oi.UnitName, ''), N'—') AS UnitName,
        (oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) AS Quantity,
        oi.UnitPrice,
        oi.TotalAmount,
        oi.Amount AS PreTaxAmount,
        oi.VatAmount AS TaxAmount,
        oi.DiscountAmount AS DiscountPreTaxAmount,
        oi.VatRate AS TaxRate,
        mi.ItemType,
        mg.Name AS MenuGroup
    FROM dbo.SalesOrderItem oi
    INNER JOIN #TempOrders t ON oi.OrderId = t.Id
    LEFT JOIN dbo.MenuItem mi ON oi.ItemId = mi.Id
    LEFT JOIN dbo.Unit u ON mi.UnitId = u.Id
    LEFT JOIN dbo.MenuGroup mg ON mi.MenuGroupId = mg.Id
    WHERE ISNULL(oi.IsCancelled, 0) = 0 AND ISNULL(oi.IsReturned, 0) = 0 AND (oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) > 0
    ORDER BY t.OrderDate DESC, ISNULL(t.PaymentTime, t.OpenTime) DESC, t.OrderCode DESC, oi.[LineNo]; 

    -- Result Set 3: Total count and global aggregates of all matching Paid orders
    SELECT 
        COUNT(1) AS TotalRecords,
        ISNULL(SUM(o.TotalAmount), 0) AS TotalTotalAmount,
        ISNULL(SUM(o.SubTotal), 0) AS TotalPreTaxAmount,
        ISNULL(SUM(o.VatAmount), 0) AS TotalTaxAmount,
        ISNULL(SUM(o.DiscountAmount), 0) AS TotalDiscountPreTaxAmount,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p INNER JOIN dbo.SalesOrder o2 ON p.OrderId = o2.Id WHERE o2.Status = 'Paid' AND p.PaymentType = 'Cash' AND (ISNULL(@BranchID, '') = '' OR o2.BranchCode = @BranchID) AND (@FromDate IS NULL OR CAST(o2.OrderDate AS DATE) >= CAST(@FromDate AS DATE)) AND (@ToDate IS NULL OR CAST(o2.OrderDate AS DATE) <= CAST(@ToDate AS DATE))), 0) AS TotalCashAmount,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p INNER JOIN dbo.SalesOrder o2 ON p.OrderId = o2.Id WHERE o2.Status = 'Paid' AND p.PaymentType IN ('Transfer', 'QR', 'Bank', 'CreditCard', 'Card') AND (ISNULL(@BranchID, '') = '' OR o2.BranchCode = @BranchID) AND (@FromDate IS NULL OR CAST(o2.OrderDate AS DATE) >= CAST(@FromDate AS DATE)) AND (@ToDate IS NULL OR CAST(o2.OrderDate AS DATE) <= CAST(@ToDate AS DATE))), 0) AS TotalTransferAmount,
        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p INNER JOIN dbo.SalesOrder o2 ON p.OrderId = o2.Id WHERE o2.Status = 'Paid' AND (p.PaymentType LIKE '%Voucher%' OR p.PaymentType LIKE '%Khuyến mãi%' OR p.PaymentType = 'Discount') AND (ISNULL(@BranchID, '') = '' OR o2.BranchCode = @BranchID) AND (@FromDate IS NULL OR CAST(o2.OrderDate AS DATE) >= CAST(@FromDate AS DATE)) AND (@ToDate IS NULL OR CAST(o2.OrderDate AS DATE) <= CAST(@ToDate AS DATE))), 0) AS TotalVoucherAmount,
        -- Sum of quantities of all detail items in all matching orders
        ISNULL((
            SELECT SUM(oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0))
            FROM dbo.SalesOrderItem oi
            INNER JOIN dbo.SalesOrder o ON oi.OrderId = o.Id
            WHERE o.Status = 'Paid'
              AND ISNULL(oi.IsCancelled, 0) = 0
              AND ISNULL(oi.IsReturned, 0) = 0
              AND (oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) > 0
              AND (ISNULL(@BranchID, '') = '' OR o.BranchCode = @BranchID)
              AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
              AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE))
        ), 0) AS TotalQuantity
    FROM dbo.SalesOrder o
    WHERE 
        o.Status = 'Paid'
        AND (ISNULL(@BranchID, '') = '' OR o.BranchCode = @BranchID)
        AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
        AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE));

    DROP TABLE #TempOrders;
END;