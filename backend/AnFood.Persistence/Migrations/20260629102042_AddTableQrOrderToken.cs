using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableQrOrderToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TableQrOrderToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableQrOrderToken", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TableQrOrderToken_TableId_IsActive",
                table: "TableQrOrderToken",
                columns: new[] { "TableId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TableQrOrderToken_Token",
                table: "TableQrOrderToken",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableQrOrderToken");
        }
    }
}
