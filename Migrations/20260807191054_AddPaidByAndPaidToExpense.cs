using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastosApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidByAndPaidToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Paid",
                table: "Expenses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidBy",
                table: "Expenses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidBy",
                table: "Expenses");
        }
    }
}
