using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASRS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultPricingToProductAndMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Products",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultUnitPrice",
                table: "Products",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Materials",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultUnitPrice",
                table: "Materials",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultUnitPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DefaultUnitPrice",
                table: "Materials");
        }
    }
}
