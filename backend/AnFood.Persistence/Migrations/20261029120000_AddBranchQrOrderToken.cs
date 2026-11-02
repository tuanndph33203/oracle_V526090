using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnFood.Persistence.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261029120000_AddBranchQrOrderToken")]
public partial class AddBranchQrOrderToken : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BranchQrOrderToken",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedOnDate = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BranchQrOrderToken", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_BranchQrOrderToken_Token", table: "BranchQrOrderToken", column: "Token", unique: true);
        migrationBuilder.CreateIndex(name: "IX_BranchQrOrderToken_BranchId_IsActive", table: "BranchQrOrderToken", columns: new[] { "BranchId", "IsActive" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "BranchQrOrderToken");
}
