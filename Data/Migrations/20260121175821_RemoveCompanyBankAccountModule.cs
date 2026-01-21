using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyBankAccountModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_CompanyBankAccounts_CompanyBankAccountId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "CompanyBankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CompanyBankAccountId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyBankAccountId",
                table: "PaymentTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyBankAccountId",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CompanyBankAccounts",
                columns: table => new
                {
                    CompanyBankAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(28,2)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SwiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBankAccounts", x => x.CompanyBankAccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CompanyBankAccountId",
                table: "PaymentTransactions",
                column: "CompanyBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBankAccounts_AccountNumber",
                table: "CompanyBankAccounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_CompanyBankAccounts_CompanyBankAccountId",
                table: "PaymentTransactions",
                column: "CompanyBankAccountId",
                principalTable: "CompanyBankAccounts",
                principalColumn: "CompanyBankAccountId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
