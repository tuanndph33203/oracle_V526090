using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20261028120000_AddMenuOptions")]
    public partial class AddMenuOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "OptionGroupsJson", table: "MenuItem", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SelectedOptionsJson", table: "SalesOrderItem", type: "nvarchar(max)", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SelectedOptionsJson", table: "SalesOrderItem");
            migrationBuilder.DropColumn(name: "OptionGroupsJson", table: "MenuItem");
        }
    }
}
