using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AnFood.Persistence.Context;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20261030130000_AddBranchMapUrl")]
    public partial class AddBranchMapUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapUrl",
                table: "Branchs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapUrl",
                table: "Branchs");
        }
    }
}
