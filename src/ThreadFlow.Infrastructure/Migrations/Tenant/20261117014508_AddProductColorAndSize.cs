using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreadFlow.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddProductColorAndSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Products",
                type: "nvarchar(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Products",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("""
    UPDATE dbo.Products
    SET
        Color = CASE Sku
            WHEN N'DRESS-NVY-S' THEN N'Navy'
            WHEN N'JEANS-BLU-32' THEN N'Blue'
            WHEN N'TSHIRT-BLK-M' THEN N'Black'
        END,
        Size = CASE Sku
            WHEN N'DRESS-NVY-S' THEN N'S'
            WHEN N'JEANS-BLU-32' THEN N'32'
            WHEN N'TSHIRT-BLK-M' THEN N'M'
        END
    WHERE Sku IN (N'DRESS-NVY-S', N'JEANS-BLU-32', N'TSHIRT-BLK-M');
    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Products");
        }
    }
}
