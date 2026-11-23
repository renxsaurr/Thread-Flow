using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ThreadFlow.Infrastructure.data.Tenant;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Tenant;

[DbContext(typeof(TenantDbContext))]
[Migration("20261122130100_AddBranchIdToCustomerInteractions")]
public partial class AddBranchIdToCustomerInteractions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "BranchId",
            table: "CustomerInteractions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CustomerInteractions_BranchId",
            table: "CustomerInteractions",
            column: "BranchId");

        migrationBuilder.AddForeignKey(
            name: "FK_CustomerInteractions_Branches_BranchId",
            table: "CustomerInteractions",
            column: "BranchId",
            principalTable: "Branches",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CustomerInteractions_Branches_BranchId",
            table: "CustomerInteractions");

        migrationBuilder.DropIndex(
            name: "IX_CustomerInteractions_BranchId",
            table: "CustomerInteractions");

        migrationBuilder.DropColumn(
            name: "BranchId",
            table: "CustomerInteractions");
    }
}
