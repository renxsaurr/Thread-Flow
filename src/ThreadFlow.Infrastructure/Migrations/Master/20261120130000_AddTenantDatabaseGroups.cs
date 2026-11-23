using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Master;

public partial class AddTenantDatabaseGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TenantDatabaseGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                CompanySize = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                LocalDatabasePrefix = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_TenantDatabaseGroups", x => x.Id));

        migrationBuilder.InsertData(
            table: "TenantDatabaseGroups",
            columns: ["Id", "Code", "Name", "CompanySize", "LocalDatabasePrefix"],
            values: new object[,]
            {
                { new Guid("20000000-0000-0000-0000-000000000001"), "TENANT-A", "Tenant A — Micro hosting group", "Micro", "ThreadFlow_TenantA" },
                { new Guid("20000000-0000-0000-0000-000000000002"), "TENANT-B", "Tenant B — Small hosting group", "Small", "ThreadFlow_TenantB" },
                { new Guid("20000000-0000-0000-0000-000000000003"), "TENANT-C", "Tenant C — Medium hosting group", "Medium", "ThreadFlow_TenantC" }
            });

        migrationBuilder.AddColumn<Guid>(
            name: "DatabaseGroupId",
            table: "Tenants",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE dbo.Tenants
            SET DatabaseGroupId = CASE CompanySize
                WHEN N'Small' THEN '20000000-0000-0000-0000-000000000002'
                WHEN N'Medium' THEN '20000000-0000-0000-0000-000000000003'
                ELSE '20000000-0000-0000-0000-000000000001'
            END;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "DatabaseGroupId",
            table: "Tenants",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantDatabaseGroups_Code",
            table: "TenantDatabaseGroups",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantDatabaseGroups_CompanySize",
            table: "TenantDatabaseGroups",
            column: "CompanySize",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_DatabaseGroupId",
            table: "Tenants",
            column: "DatabaseGroupId");

        migrationBuilder.AddForeignKey(
            name: "FK_Tenants_TenantDatabaseGroups_DatabaseGroupId",
            table: "Tenants",
            column: "DatabaseGroupId",
            principalTable: "TenantDatabaseGroups",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Tenants_TenantDatabaseGroups_DatabaseGroupId",
            table: "Tenants");

        migrationBuilder.DropIndex(
            name: "IX_Tenants_DatabaseGroupId",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "DatabaseGroupId",
            table: "Tenants");

        migrationBuilder.DropTable(name: "TenantDatabaseGroups");
    }
}
