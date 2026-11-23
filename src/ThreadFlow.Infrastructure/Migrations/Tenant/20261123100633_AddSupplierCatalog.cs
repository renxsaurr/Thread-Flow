using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Tenant;

/// <inheritdoc />
public partial class AddSupplierCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.AddColumn<Guid>(
            name: "SupplierId",
            table: "Products",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_SupplierId",
            table: "Products",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_Suppliers_Name",
            table: "Suppliers",
            column: "Name");

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Suppliers_SupplierId",
            table: "Products",
            column: "SupplierId",
            principalTable: "Suppliers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Products_Suppliers_SupplierId",
            table: "Products");

        migrationBuilder.DropIndex(
            name: "IX_Products_SupplierId",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");

        migrationBuilder.DropTable(name: "Suppliers");
    }
}
