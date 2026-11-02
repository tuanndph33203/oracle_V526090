using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceRuleDiscountOptionToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "HasDiscount",
                table: "InvoiceRuleDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "HasDiscount",
                table: "InvoiceRuleDetails",
                type: "bit",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
