using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Master;

public partial class AddTenantIdToPlatformAuditEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PlatformAuditEntries",
            type: "uniqueidentifier",
            nullable: true);

        // Associate older tenant events using their existing entity references.
        migrationBuilder.Sql("""
            UPDATE audit
            SET TenantId = tenant.Id
            FROM dbo.PlatformAuditEntries AS audit
            INNER JOIN dbo.AspNetUsers AS account
                ON TRY_CONVERT(uniqueidentifier, audit.EntityId) = account.Id
            INNER JOIN dbo.Tenants AS tenant
                ON tenant.Id = account.TenantId
            WHERE audit.TenantId IS NULL
              AND audit.EntityType = N'PlatformUser';

            UPDATE audit
            SET TenantId = tenant.Id
            FROM dbo.PlatformAuditEntries AS audit
            INNER JOIN dbo.Tenants AS tenant
                ON TRY_CONVERT(uniqueidentifier, audit.EntityId) = tenant.Id
            WHERE audit.TenantId IS NULL
              AND audit.EntityType = N'TenantCompanySettings';

            UPDATE audit
            SET TenantId = application.TenantId
            FROM dbo.PlatformAuditEntries AS audit
            INNER JOIN dbo.CompanyApplications AS application
                ON TRY_CONVERT(uniqueidentifier, audit.EntityId) = application.Id
            WHERE audit.TenantId IS NULL
              AND audit.EntityType = N'CompanyApplication';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAuditEntries_TenantId",
            table: "PlatformAuditEntries",
            column: "TenantId");

        migrationBuilder.AddForeignKey(
            name: "FK_PlatformAuditEntries_Tenants_TenantId",
            table: "PlatformAuditEntries",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PlatformAuditEntries_Tenants_TenantId",
            table: "PlatformAuditEntries");

        migrationBuilder.DropIndex(
            name: "IX_PlatformAuditEntries_TenantId",
            table: "PlatformAuditEntries");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PlatformAuditEntries");
    }
}
