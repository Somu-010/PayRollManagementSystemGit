using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayRollManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToCompanyCentricPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_BankAccounts_BankAccountId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.RenameColumn(
                name: "BankAccountId",
                table: "PaymentTransactions",
                newName: "CompanyBankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentTransactions_BankAccountId",
                table: "PaymentTransactions",
                newName: "IX_PaymentTransactions_CompanyBankAccountId");

            migrationBuilder.AddColumn<string>(
                name: "ChequeNumber",
                table: "PaymentTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeAccountNumber",
                table: "PaymentTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeBankName",
                table: "PaymentTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileBankingNumber",
                table: "PaymentTransactions",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileBankingProvider",
                table: "PaymentTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileBankingNumber",
                table: "Employees",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MobileBankingProvider",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNotes",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingNumber",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyBankAccounts",
                columns: table => new
                {
                    CompanyBankAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    SwiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBankAccounts", x => x.CompanyBankAccountId);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_CompanyBankAccounts_CompanyBankAccountId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "CompanyBankAccounts");

            migrationBuilder.DropColumn(
                name: "ChequeNumber",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "EmployeeAccountNumber",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "EmployeeBankName",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MobileBankingNumber",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MobileBankingProvider",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MobileBankingNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MobileBankingProvider",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PaymentNotes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RoutingNumber",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "CompanyBankAccountId",
                table: "PaymentTransactions",
                newName: "BankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentTransactions_CompanyBankAccountId",
                table: "PaymentTransactions",
                newName: "IX_PaymentTransactions_BankAccountId");

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    BankAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SwiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.BankAccountId);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_EmployeeId_AccountNumber",
                table: "BankAccounts",
                columns: new[] { "EmployeeId", "AccountNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_BankAccounts_BankAccountId",
                table: "PaymentTransactions",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "BankAccountId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
