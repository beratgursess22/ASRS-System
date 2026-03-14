using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASRS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderStockConsumptionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockConsumed",
                table: "WorkOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StockConsumedAt",
                table: "WorkOrders",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockConsumed",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "StockConsumedAt",
                table: "WorkOrders");
        }
    }
}
