using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class Allowencediductioncostanalysistable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IndustryType = table.Column<int>(type: "int", nullable: false),
                    EmployeeLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTemplates", x => x.TemplateId);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTemplateItems",
                columns: table => new
                {
                    TemplateItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    AllowanceDeductionId = table.Column<int>(type: "int", nullable: false),
                    CustomValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTemplateItems", x => x.TemplateItemId);
                    table.ForeignKey(
                        name: "FK_ComponentTemplateItems_AllowanceDeductions_AllowanceDeductionId",
                        column: x => x.AllowanceDeductionId,
                        principalTable: "AllowanceDeductions",
                        principalColumn: "AllowanceDeductionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentTemplateItems_ComponentTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComponentTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplateItems_AllowanceDeductionId",
                table: "ComponentTemplateItems",
                column: "AllowanceDeductionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplateItems_TemplateId",
                table: "ComponentTemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplates_Name",
                table: "ComponentTemplates",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentTemplateItems");

            migrationBuilder.DropTable(
                name: "ComponentTemplates");
        }
    }
}
