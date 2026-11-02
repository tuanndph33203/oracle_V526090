using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261028090000_AddQrOrderRatings")]
public partial class AddQrOrderRatings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "MergedIntoOrderId", table: "SalesOrder", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<int>(name: "RatingStars", table: "SalesOrder", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RatingContent", table: "SalesOrder", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MergedIntoOrderId", table: "SalesOrder");
        migrationBuilder.DropColumn(name: "RatingStars", table: "SalesOrder");
        migrationBuilder.DropColumn(name: "RatingContent", table: "SalesOrder");
    }
}
