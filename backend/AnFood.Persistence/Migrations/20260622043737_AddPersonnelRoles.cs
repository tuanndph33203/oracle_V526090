using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IsCollaborator",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsEmployee",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsLoyalCustomer",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsSupplier",
                table: "Employee",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCollaborator",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "IsEmployee",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "IsLoyalCustomer",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "IsSupplier",
                table: "Employee");
        }
    }
}
