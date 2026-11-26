using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class SupplierProductPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                columns: table => new
                {
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProducts", x => new { x.SupplierId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_ProductId",
                table: "SupplierProducts",
                column: "ProductId");

            // Preserve each product's existing preferred-supplier link. Its quote is unknown,
            // so seed the relationship with zero until staff enter the supplier's actual quote.
            migrationBuilder.Sql("""
                INSERT INTO [SupplierProducts] ([SupplierId], [ProductId], [UnitCost], [UpdatedAtUtc])
                SELECT [SupplierId], [Id], 0, SYSUTCDATETIME()
                FROM [Products]
                WHERE [SupplierId] IS NOT NULL
                  AND EXISTS (SELECT 1 FROM [Suppliers] WHERE [Suppliers].[Id] = [Products].[SupplierId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierProducts");
        }
    }
}
