using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASRS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InspectionNumber = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UsageDecision = table.Column<int>(type: "int", nullable: true),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    PurchaseOrderItemId = table.Column<int>(type: "int", nullable: true),
                    WorkOrderId = table.Column<int>(type: "int", nullable: true),
                    InspectedByUserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InspectionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityInspections_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QualityInspections_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Users_InspectedByUserId",
                        column: x => x.InspectedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QualityInspections_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QualityDefects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QualityInspectionId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityDefects_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QualityInspectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QualityInspectionId = table.Column<int>(type: "int", nullable: false),
                    CharacteristicName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPassed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityInspectionItems_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CapaActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QualityDefectId = table.Column<int>(type: "int", nullable: false),
                    ActionDescription = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsibleUserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionNote = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapaActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapaActions_QualityDefects_QualityDefectId",
                        column: x => x.QualityDefectId,
                        principalTable: "QualityDefects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CapaActions_Users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CapaActions_QualityDefectId",
                table: "CapaActions",
                column: "QualityDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_CapaActions_ResponsibleUserId",
                table: "CapaActions",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefects_QualityInspectionId",
                table: "QualityDefects",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspectionItems_QualityInspectionId",
                table: "QualityInspectionItems",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_CreatedAt",
                table: "QualityInspections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectedByUserId",
                table: "QualityInspections",
                column: "InspectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionNumber",
                table: "QualityInspections",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionType",
                table: "QualityInspections",
                column: "InspectionType");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_PurchaseOrderId",
                table: "QualityInspections",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_PurchaseOrderItemId",
                table: "QualityInspections",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_Status",
                table: "QualityInspections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_WorkOrderId",
                table: "QualityInspections",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapaActions");

            migrationBuilder.DropTable(
                name: "QualityInspectionItems");

            migrationBuilder.DropTable(
                name: "QualityDefects");

            migrationBuilder.DropTable(
                name: "QualityInspections");
        }
    }
}
