using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MachineCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CashierId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CashierName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ExtendedMinutes = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DifferenceCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransferCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceiverEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftCashCount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Denomination = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCashCount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftExtensionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtendedMinutes = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftExtensionHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shift_BranchCode_CashierId_Status",
                table: "Shift",
                columns: new[] { "BranchCode", "CashierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashCount_ShiftId",
                table: "ShiftCashCount",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftExtensionHistory_ShiftId",
                table: "ShiftExtensionHistory",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shift");

            migrationBuilder.DropTable(
                name: "ShiftCashCount");

            migrationBuilder.DropTable(
                name: "ShiftExtensionHistory");
        }
    }
}
