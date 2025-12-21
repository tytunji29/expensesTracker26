using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expensesTracker26.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthYearToBillsHolderUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillsHolders_ExpenseId_IncomeSourceId",
                table: "BillsHolders");

            migrationBuilder.CreateIndex(
                name: "IX_BillsHolders_ExpenseId_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders",
                columns: new[] { "ExpenseId", "IncomeSourceId", "MonthId", "YearId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillsHolders_ExpenseId_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders");

            migrationBuilder.CreateIndex(
                name: "IX_BillsHolders_ExpenseId_IncomeSourceId",
                table: "BillsHolders",
                columns: new[] { "ExpenseId", "IncomeSourceId" },
                unique: true);
        }
    }
}
