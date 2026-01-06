using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expensesTracker26.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndexFromBillsHolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillsHolders_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders");

            migrationBuilder.CreateIndex(
                name: "IX_BillsHolders_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders",
                columns: new[] { "IncomeSourceId", "MonthId", "YearId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillsHolders_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders");

            migrationBuilder.CreateIndex(
                name: "IX_BillsHolders_IncomeSourceId_MonthId_YearId",
                table: "BillsHolders",
                columns: new[] { "IncomeSourceId", "MonthId", "YearId" },
                unique: true);
        }
    }
}
