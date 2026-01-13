using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeAndLatePenaltySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LatePenaltyPerDay",
                table: "CompanySettings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxLateDaysAllowed",
                table: "CompanySettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeRate",
                table: "CompanySettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatePenaltyPerDay",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "MaxLateDaysAllowed",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "OvertimeRate",
                table: "CompanySettings");
        }
    }
}
