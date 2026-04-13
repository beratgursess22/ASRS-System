using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASRS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAsrsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsrsCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: true),
                    Col = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsrsCommands", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RackCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Col = table.Column<int>(type: "int", nullable: false),
                    IsOccupied = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastCommandId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RackCells", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RfidEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CardUid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TriggeredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResultCommandId = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RfidRackMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CardUid = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Col = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidRackMaps", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "RackCells",
                columns: new[] { "Id", "Col", "IsOccupied", "LastCommandId", "Row", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 0, false, null, 0, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3220) },
                    { 2, 1, false, null, 0, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 3, 2, false, null, 0, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 4, 3, false, null, 0, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 5, 0, false, null, 1, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 6, 1, false, null, 1, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 7, 2, false, null, 1, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 8, 3, false, null, 1, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 9, 0, false, null, 2, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 10, 1, false, null, 2, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 11, 2, false, null, 2, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) },
                    { 12, 3, false, null, 2, new DateTime(2026, 4, 13, 10, 45, 6, 416, DateTimeKind.Utc).AddTicks(3350) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsrsCommands_Status_CreatedAt",
                table: "AsrsCommands",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RackCells_Row_Col",
                table: "RackCells",
                columns: new[] { "Row", "Col" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfidEvents_TriggeredAt",
                table: "RfidEvents",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RfidRackMaps_CardUid",
                table: "RfidRackMaps",
                column: "CardUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfidRackMaps_Row_Col",
                table: "RfidRackMaps",
                columns: new[] { "Row", "Col" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsrsCommands");

            migrationBuilder.DropTable(
                name: "RackCells");

            migrationBuilder.DropTable(
                name: "RfidEvents");

            migrationBuilder.DropTable(
                name: "RfidRackMaps");
        }
    }
}
