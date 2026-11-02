using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KitchenStatus",
                table: "SalesOrderItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RemainingQty",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SentQty",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ServedQty",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SoldOutQty",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "KitchenRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KitchenAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenRequestDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitchenRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestQty = table.Column<double>(type: "float", nullable: false),
                    ServedQty = table.Column<double>(type: "float", nullable: false),
                    SoldOutQty = table.Column<double>(type: "float", nullable: false),
                    RemainingQty = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenRequestDetail", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenRequest_BranchCode_RequestCode",
                table: "KitchenRequest",
                columns: new[] { "BranchCode", "RequestCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenRequestDetail_KitchenRequestId_OrderDetailId",
                table: "KitchenRequestDetail",
                columns: new[] { "KitchenRequestId", "OrderDetailId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenRequest");

            migrationBuilder.DropTable(
                name: "KitchenRequestDetail");

            migrationBuilder.DropColumn(
                name: "KitchenStatus",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "RemainingQty",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "SentQty",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "ServedQty",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "SoldOutQty",
                table: "SalesOrderItem");
        }
    }
}
