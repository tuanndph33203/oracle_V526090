using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiningTableLayoutProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "DiningTable",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "DiningTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Rotate",
                table: "DiningTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "DiningTable",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "DiningTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "X",
                table: "DiningTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Y",
                table: "DiningTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "Rotate",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "X",
                table: "DiningTable");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "DiningTable");
        }
    }
}
