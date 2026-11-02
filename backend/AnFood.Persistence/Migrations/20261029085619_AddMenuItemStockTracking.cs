using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemStockTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyQuota",
                table: "MenuItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrackStock",
                table: "MenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "MenuItem",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyQuota",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "IsTrackStock",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "MenuItem");
        }
    }
}
