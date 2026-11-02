using Microsoft.EntityFrameworkCore.Migrations;

namespace AnFood.Persistence.Migrations
{
    public partial class AddPriorityToMenuGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "MenuGroup",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "MenuGroup");
        }
    }
}
