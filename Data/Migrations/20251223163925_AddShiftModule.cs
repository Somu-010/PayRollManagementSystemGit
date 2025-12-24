using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    ShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakDuration = table.Column<int>(type: "int", nullable: false),
                    GracePeriod = table.Column<int>(type: "int", nullable: false),
                    LateMarkAfter = table.Column<int>(type: "int", nullable: false),
                    HalfDayHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FullDayHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedEmployees = table.Column<int>(type: "int", nullable: false),
                    IsNightShift = table.Column<bool>(type: "bit", nullable: false),
                    IsWeekendShift = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.ShiftId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ShiftId",
                table: "Employees",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ShiftName",
                table: "Shifts",
                column: "ShiftName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ShiftId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Employees");
        }
    }
}
