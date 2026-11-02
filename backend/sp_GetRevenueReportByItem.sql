CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueReportByItem
    @BranchID VARCHAR(50) = NULL,
    @ReportType INT = 2, -- 1: Daily, 2: By Item, 3: Invoice List, 4: Detailed by Item
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- CTE to gather raw items from orders that are Paid (true revenue)
    WITH RawData AS (
        SELECT 
            oi.ItemId,
            oi.ItemCode,
            oi.ItemName,
            COALESCE(u.Name, NULLIF(oi.UnitName, ''), N'—') AS UnitName,
            (oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) AS Quantity,
            oi.Amount,
            oi.VatAmount,
            oi.TotalAmount,
            -- Khuyến mại theo đơn giá đã bao gồm thuế VAT (tính theo số lượng thực bán):
            CASE 
                WHEN oi.DiscountRate > 0 AND COALESCE(NULLIF(mi.Amount, 0), mi.SalePrice + mi.FixedTaxAmount, 0) > 0 
                    THEN ROUND((oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) * (COALESCE(NULLIF(mi.Amount, 0), mi.SalePrice + mi.FixedTaxAmount, 0) * (oi.DiscountRate / 100.0)), 0)
                WHEN oi.DiscountAmount > 0 AND oi.Amount > 0 
                    THEN ROUND(oi.DiscountAmount * (1.0 + (ISNULL(oi.VatRate, 0) / 100.0)), 0)
                ELSE oi.DiscountAmount 
            END AS GrossDiscountAmount,
            COALESCE(NULLIF(mi.Amount, 0), mi.SalePrice + mi.FixedTaxAmount, 0) AS MenuItemGrossPrice,
            mi.ItemType,
            mg.Name AS MenuGroup
        FROM dbo.SalesOrderItem oi
        INNER JOIN dbo.SalesOrder o ON oi.OrderId = o.Id
        LEFT JOIN dbo.MenuItem mi ON oi.ItemId = mi.Id
        LEFT JOIN dbo.Unit u ON mi.UnitId = u.Id
        LEFT JOIN dbo.MenuGroup mg ON mi.MenuGroupId = mg.Id
        WHERE 
            (ISNULL(@BranchID, '') = '' OR o.BranchCode = @BranchID)
            AND o.Status = 'Paid'
            AND ISNULL(oi.IsCancelled, 0) = 0
            AND ISNULL(oi.IsReturned, 0) = 0
            AND (oi.Quantity - ISNULL(oi.ReturnedQuantity, 0) - ISNULL(oi.CancelledQuantity, 0)) > 0
            AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
            AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE))
    ),
    GroupedData AS (
        SELECT 
            ItemId,
            ItemCode,
            ItemName,
            MAX(UnitName) AS UnitName,
            MAX(MenuItemGrossPrice) AS MenuItemGrossPrice,
            SUM(Quantity) AS TotalQty,
            SUM(Amount) AS TotalRawAmount,
            SUM(GrossDiscountAmount) AS TotalDiscountAmount,
            SUM(VatAmount) AS TotalVatAmount,
            SUM(TotalAmount) AS TotalRevenue,
            MAX(MenuGroup) AS MenuGroup,
            MAX(CASE ItemType WHEN 1 THEN N'Món ăn' WHEN 2 THEN N'Đồ uống' ELSE N'Khác' END) AS ItemType
        FROM RawData
        GROUP BY ItemId, ItemCode, ItemName
    ),
    FinalData AS (
        SELECT 
            ROW_NUMBER() OVER (ORDER BY TotalRevenue DESC, ItemCode) AS STT,
            ItemId,
            ItemCode,
            ItemName,
            UnitName,
            TotalQty AS Quantity,
            COALESCE(NULLIF(MenuItemGrossPrice, 0), CASE WHEN TotalQty > 0 THEN (TotalRevenue + TotalDiscountAmount) / TotalQty ELSE 0 END) AS AvgPrice,
            TotalRevenue AS Total,                                        -- Tổng (Tiền hàng - KM)
            TotalRevenue AS Revenue,
            (TotalRevenue + TotalDiscountAmount) AS ItemAmount,           -- Tiền hàng (Đơn giá có VAT * SL)
            TotalDiscountAmount AS DiscountAmount,                        -- Khuyến mại (Đơn giá có VAT * %CK)
            ItemType,
            MenuGroup
        FROM GroupedData
    )
    SELECT * INTO #TempReport FROM FinalData;

    -- Result Set 1: Paginated data for report table
    SELECT 
        STT,
        ItemId,
        ItemCode,
        ItemName,
        UnitName,
        Quantity,
        AvgPrice,
        Total,
        Revenue,
        ItemAmount,
        DiscountAmount,
        ItemType,
        MenuGroup
    FROM #TempReport
    ORDER BY STT
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    -- Result Set 2: Global totals for the footer
    SELECT 
        COUNT(1) AS TotalRecords,
        ISNULL(SUM(Quantity), 0) AS TotalQuantity,
        ISNULL(SUM(Total), 0) AS TotalTotal,
        ISNULL(SUM(Revenue), 0) AS TotalRevenue,
        ISNULL(SUM(ItemAmount), 0) AS TotalItemAmount,
        ISNULL(SUM(DiscountAmount), 0) AS TotalDiscountAmount
    FROM #TempReport;

    DROP TABLE #TempReport;
END;
GO
