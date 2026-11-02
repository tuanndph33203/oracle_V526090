using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQrStaffCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrStaffCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrStaffCalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrStaffCalls_BranchCode_CreatedAt",
                table: "QrStaffCalls",
                columns: new[] { "BranchCode", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrStaffCalls");
        }
    }
}
