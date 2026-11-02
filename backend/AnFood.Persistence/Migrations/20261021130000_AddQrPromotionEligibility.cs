using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    public partial class AddQrPromotionEligibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApplyOnlyToQrOrders",
                table: "Promotion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesQrOrder",
                table: "SalesOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.sp_GetInvoiceListReport
                    @BranchID VARCHAR(50) = NULL,
                    @FromDate DATETIME = NULL,
                    @ToDate DATETIME = NULL,
                    @PageNumber INT = 1,
                    @PageSize INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        o.Id AS OrderId,
                        o.OrderDate,
                        o.OpenTime,
                        o.PaymentTime,
                        o.OrderCode,
                        o.SubTotal AS PreTaxAmount,
                        o.VatAmount AS TaxAmount,
                        o.DiscountAmount,
                        o.TotalAmount,
                        o.CustomerName AS CustomerInfo,
                        o.CustomerCount,
                        o.ServiceType,
                        o.FloorName AS AreaName,
                        o.CashierUserName AS CashierName,
                        o.WaiterName,
                        o.Note,
                        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND p.PaymentType = 'Cash'), 0) AS CashAmount,
                        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND p.PaymentType IN ('Transfer', 'QR', 'Bank')), 0) AS TransferAmount,
                        ISNULL((SELECT SUM(p.PaymentAmount) FROM dbo.SalesOrderPayment p WHERE p.OrderId = o.Id AND p.PaymentType = 'Voucher'), 0) AS VoucherAmount,
                        ISNULL((
                            SELECT STRING_AGG(st.TableName, ' + ') WITHIN GROUP (ORDER BY st.TableName)
                            FROM dbo.SalesOrderTable st
                            WHERE st.OrderId = o.Id
                        ), N'Mang về/Giao hàng') AS TableInfo,
                        (
                            SELECT STRING_AGG(promo.PromotionLabel, N', ')
                            FROM (
                                SELECT DISTINCT CONVERT(nvarchar(max), p.Code + N' - ' + p.Name) AS PromotionLabel
                                FROM dbo.SalesOrderItem oi
                                INNER JOIN dbo.Promotion p ON p.Code = oi.PromotionCode
                                WHERE oi.OrderId = o.Id AND NULLIF(oi.PromotionCode, '') IS NOT NULL
                                UNION
                                SELECT CONVERT(nvarchar(max), p.Code + N' - ' + p.Name)
                                FROM dbo.Promotion p
                                WHERE p.Code = o.SelectedPromotionCode
                            ) promo
                        ) AS PromotionInfo,
                        CAST(NULL AS VARCHAR(100)) AS DeliveryPartner
                    INTO #FilteredOrders
                    FROM dbo.SalesOrder o
                    WHERE o.Status = 'Paid'
                        AND (ISNULL(@BranchID, '') = '' OR o.BranchCode = @BranchID)
                        AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= CAST(@FromDate AS DATE))
                        AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= CAST(@ToDate AS DATE));

                    SELECT *
                    FROM #FilteredOrders
                    ORDER BY OrderDate DESC, ISNULL(PaymentTime, OpenTime) DESC, OrderCode DESC
                    OFFSET (@PageNumber - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT
                        COUNT(1) AS TotalRecords,
                        ISNULL(SUM(PreTaxAmount), 0) AS TotalPreTaxAmount,
                        ISNULL(SUM(TaxAmount), 0) AS TotalTaxAmount,
                        ISNULL(SUM(DiscountAmount), 0) AS TotalDiscountAmount,
                        ISNULL(SUM(TotalAmount), 0) AS TotalTotalAmount,
                        ISNULL(SUM(CashAmount), 0) AS TotalCashAmount,
                        ISNULL(SUM(TransferAmount), 0) AS TotalTransferAmount,
                        ISNULL(SUM(VoucherAmount), 0) AS TotalVoucherAmount,
                        ISNULL(SUM(CustomerCount), 0) AS TotalCustomerCount
                    FROM #FilteredOrders;

                    DROP TABLE #FilteredOrders;
                END;
                """, suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ApplyOnlyToQrOrders", table: "Promotion");
            migrationBuilder.DropColumn(name: "IncludesQrOrder", table: "SalesOrder");
        }
    }
}
