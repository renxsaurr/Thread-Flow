using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ThreadFlow.Infrastructure.Migrations.Master
{
    /// <inheritdoc />
    public partial class SeedInitialTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CloudDatabaseName", "Code", "CompanySize", "CreatedAtUtc", "IsActive", "LocalDatabaseName", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "db78629", "TENANT-A", "Micro", new DateTime(2026, 11, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "ThreadFlow_TenantA", "Tenant A - Micro Company" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "db78631", "TENANT-B", "Small", new DateTime(2026, 11, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "ThreadFlow_TenantB", "Tenant B - Small Company" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "db78630", "TENANT-C", "Medium", new DateTime(2026, 11, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "ThreadFlow_TenantC", "Tenant C - Medium Enterprise Company" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));
        }
    }
}
