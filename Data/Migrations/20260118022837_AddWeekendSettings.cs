using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekendSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeekendSettings",
                columns: table => new
                {
                    WeekendSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsFridayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsSaturdayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsSundayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsMondayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsTuesdayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsWednesdayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IsThursdayWeekend = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekendSettings", x => x.WeekendSettingId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeekendSettings");
        }
    }
}
