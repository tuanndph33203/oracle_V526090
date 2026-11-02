using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CashierUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CashierUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WaiterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaiterName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCount = table.Column<int>(type: "int", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountRate = table.Column<double>(type: "float", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatRate = table.Column<double>(type: "float", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSentToKitchen = table.Column<int>(type: "int", nullable: false),
                    IsReturned = table.Column<int>(type: "int", nullable: false),
                    IsChecked = table.Column<int>(type: "int", nullable: false),
                    IsCancelled = table.Column<int>(type: "int", nullable: false),
                    PromotionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderPayment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderTable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderTable", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_BranchCode_OrderCode",
                table: "SalesOrder",
                columns: new[] { "BranchCode", "OrderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItem_BranchCode_OrderId_LineNo",
                table: "SalesOrderItem",
                columns: new[] { "BranchCode", "OrderId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderTable_BranchCode_OrderId_TableId",
                table: "SalesOrderTable",
                columns: new[] { "BranchCode", "OrderId", "TableId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesOrder");

            migrationBuilder.DropTable(
                name: "SalesOrderItem");

            migrationBuilder.DropTable(
                name: "SalesOrderPayment");

            migrationBuilder.DropTable(
                name: "SalesOrderTable");
        }
    }
}
