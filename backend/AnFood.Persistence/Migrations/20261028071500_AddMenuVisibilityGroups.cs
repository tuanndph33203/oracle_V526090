using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261028071500_AddMenuVisibilityGroups")]
public partial class AddMenuVisibilityGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MenuGroupIdsJson",
            table: "MenuVisibilityRules",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "[]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MenuGroupIdsJson", table: "MenuVisibilityRules");
    }
}
