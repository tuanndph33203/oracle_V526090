using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AnFood.Persistence.Context;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20261022120000_AddQrOrderDiscountAmount")]
    public partial class AddQrOrderDiscountAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "QrOrderDiscountAmount",
                table: "PromotionDiscountConfig",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrOrderDiscountAmount",
                table: "PromotionDiscountConfig");
        }
    }
}
