using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AnFood.Persistence.Context;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20261026120000_AddMenuItemMustTry")]
    public partial class AddMenuItemMustTry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMustTry",
                table: "MenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsMustTry", table: "MenuItem");
        }
    }
}
