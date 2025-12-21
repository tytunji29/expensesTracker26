using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expensesTracker26.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "BillsHolders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "BillsHolders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "BillsHolders");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "BillsHolders");
        }
    }
}
