using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintAgentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintAgent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintAgent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrintAgentPrinter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintAgentPrinter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintAgent_BranchId",
                table: "PrintAgent",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintAgentPrinter_AgentId_PrinterName",
                table: "PrintAgentPrinter",
                columns: new[] { "AgentId", "PrinterName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintAgent");

            migrationBuilder.DropTable(
                name: "PrintAgentPrinter");
        }
    }
}
