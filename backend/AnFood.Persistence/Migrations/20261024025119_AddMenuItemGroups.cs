using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemGroup", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemGroup_MenuGroupId",
                table: "MenuItemGroup",
                column: "MenuGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemGroup_MenuItemId_MenuGroupId",
                table: "MenuItemGroup",
                columns: new[] { "MenuItemId", "MenuGroupId" },
                unique: true);

            migrationBuilder.Sql(@"INSERT INTO MenuItemGroup (Id, MenuItemId, MenuGroupId)
                SELECT NEWID(), Id, MenuGroupId FROM MenuItem WHERE MenuGroupId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemGroup");

        }
    }
}
