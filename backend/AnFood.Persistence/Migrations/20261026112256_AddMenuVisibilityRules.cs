using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuVisibilityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuVisibilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MenuItemIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchedulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuVisibilityRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuVisibilityRules_BranchCode_IsEnabled",
                table: "MenuVisibilityRules",
                columns: new[] { "BranchCode", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuVisibilityRules");
        }
    }
}
