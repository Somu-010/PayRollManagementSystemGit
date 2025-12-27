using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEarlyLeaveTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyLeave",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveByMinutes",
                table: "Attendances",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEarlyLeave",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveByMinutes",
                table: "Attendances");
        }
    }
}
