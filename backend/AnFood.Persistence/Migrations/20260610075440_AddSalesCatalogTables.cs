using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashDrawer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDrawer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashDrawerBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDrawerBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiningTable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FloorPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningTable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    MaterialGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShelfLife = table.Column<int>(type: "int", nullable: true),
                    ShelfLifeUnit = table.Column<int>(type: "int", nullable: true),
                    MinimumStockQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PreparationAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    CourseOrder = table.Column<int>(type: "int", nullable: false),
                    MenuGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FixedTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreparationAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemServicePreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePreferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemServicePreference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreparationArea",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceType = table.Column<int>(type: "int", nullable: false),
                    LabelPrintTiming = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationArea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreparationAreaBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreparationAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationAreaBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reason", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DefaultExtraCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePreference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkShift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShiftType = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    BreakStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    BreakEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShift", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkShiftBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShiftBranch", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawer_Code",
                table: "CashDrawer",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerBranch_CashDrawerId_BranchId",
                table: "CashDrawerBranch",
                columns: new[] { "CashDrawerId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningTable_BranchId_Code",
                table: "DiningTable",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlan_BranchId_Code",
                table: "FloorPlan",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Material_Code",
                table: "Material",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBranch_MaterialId_BranchId",
                table: "MaterialBranch",
                columns: new[] { "MaterialId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialGroup_Code",
                table: "MaterialGroup",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroup_Code",
                table: "MenuGroup",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_Code",
                table: "MenuItem",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemBranch_MenuItemId_BranchId",
                table: "MenuItemBranch",
                columns: new[] { "MenuItemId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemServicePreference_MenuItemId_ServicePreferenceId",
                table: "MenuItemServicePreference",
                columns: new[] { "MenuItemId", "ServicePreferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentType_Code",
                table: "PaymentType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationArea_Code",
                table: "PreparationArea",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationAreaBranch_PreparationAreaId_BranchId",
                table: "PreparationAreaBranch",
                columns: new[] { "PreparationAreaId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reason_Type_Code",
                table: "Reason",
                columns: new[] { "Type", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePreference_Code",
                table: "ServicePreference",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRate_Code",
                table: "TaxRate",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Code",
                table: "Warehouse",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBranch_WarehouseId_BranchId",
                table: "WarehouseBranch",
                columns: new[] { "WarehouseId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkShift_Code",
                table: "WorkShift",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkShiftBranch_WorkShiftId_BranchId",
                table: "WorkShiftBranch",
                columns: new[] { "WorkShiftId", "BranchId" },
                unique: true);

            // Seed PaymentType
            migrationBuilder.InsertData(
                table: "PaymentType",
                columns: new[] { "Id", "Code", "Name", "Status" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "CASH", "Tiền mặt", 1 },
                    { Guid.NewGuid(), "BANK_TRANSFER", "Chuyển khoản", 1 },
                    { Guid.NewGuid(), "CARD", "Thẻ", 1 },
                    { Guid.NewGuid(), "VOUCHER", "Voucher", 1 },
                    { Guid.NewGuid(), "OTHER", "Khác", 1 }
                });

            // Seed Reason
            migrationBuilder.InsertData(
                table: "Reason",
                columns: new[] { "Id", "Type", "Code", "Name", "Status" },
                values: new object[,]
                {
                    { Guid.NewGuid(), 1, "HEG_MON", "Hết món", 1 },
                    { Guid.NewGuid(), 1, "KHACH_DOI", "Khách đổi ý", 1 },
                    { Guid.NewGuid(), 2, "KHACH_VE", "Khách đi về", 1 },
                    { Guid.NewGuid(), 2, "NHAP_SAI", "Nhân viên nhập sai", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashDrawer");

            migrationBuilder.DropTable(
                name: "CashDrawerBranch");

            migrationBuilder.DropTable(
                name: "DiningTable");

            migrationBuilder.DropTable(
                name: "FloorPlan");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "MaterialBranch");

            migrationBuilder.DropTable(
                name: "MaterialGroup");

            migrationBuilder.DropTable(
                name: "MenuGroup");

            migrationBuilder.DropTable(
                name: "MenuItem");

            migrationBuilder.DropTable(
                name: "MenuItemBranch");

            migrationBuilder.DropTable(
                name: "MenuItemServicePreference");

            migrationBuilder.DropTable(
                name: "PaymentType");

            migrationBuilder.DropTable(
                name: "PreparationArea");

            migrationBuilder.DropTable(
                name: "PreparationAreaBranch");

            migrationBuilder.DropTable(
                name: "Reason");

            migrationBuilder.DropTable(
                name: "ServicePreference");

            migrationBuilder.DropTable(
                name: "TaxRate");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "WarehouseBranch");

            migrationBuilder.DropTable(
                name: "WorkShift");

            migrationBuilder.DropTable(
                name: "WorkShiftBranch");
        }
    }
}
