using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSalaryColumnsFromDesignation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumSalary",
                table: "Designations");

            migrationBuilder.DropColumn(
                name: "MinimumSalary",
                table: "Designations");

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "CompanyBankAccounts",
                type: "decimal(28,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaximumSalary",
                table: "Designations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumSalary",
                table: "Designations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "CompanyBankAccounts",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(28,2)",
                oldNullable: true);
        }
    }
}
