using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ThreadFlow.Infrastructure.data.Master;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Master;

[DbContext(typeof(MasterDbContext))]
[Migration("20261122130000_AddPlatformUserBranchAssignment")]
public partial class AddPlatformUserBranchAssignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "BranchId",
            table: "AspNetUsers",
            type: "uniqueidentifier",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BranchId",
            table: "AspNetUsers");
    }
}
