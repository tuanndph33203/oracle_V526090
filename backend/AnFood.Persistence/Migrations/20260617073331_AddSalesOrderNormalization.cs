using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashReceivedAmount",
                table: "SalesOrderPayment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeAmount",
                table: "SalesOrderPayment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "ActualQuantity",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CancelledQuantity",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReturnedQuantity",
                table: "SalesOrderItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "SalesOrder",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "SalesOrder",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "SalesOrder",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "SalesOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "SalesOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FloorName",
                table: "SalesOrder",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "SalesOrder",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "SalesOrder",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFeeNote",
                table: "SalesOrder",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashReceivedAmount",
                table: "SalesOrderPayment");

            migrationBuilder.DropColumn(
                name: "ChangeAmount",
                table: "SalesOrderPayment");

            migrationBuilder.DropColumn(
                name: "ActualQuantity",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "SalesOrderItem");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "FloorName",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "ShippingFeeNote",
                table: "SalesOrder");
        }
    }
}
