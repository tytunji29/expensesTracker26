using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expensesTracker26.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateiiggg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Month",
                table: "BillsHolders",
                newName: "YearId");

            migrationBuilder.AddColumn<int>(
                name: "MonthId",
                table: "BillsHolders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthId",
                table: "BillsHolders");

            migrationBuilder.RenameColumn(
                name: "YearId",
                table: "BillsHolders",
                newName: "Month");
        }
    }
}
