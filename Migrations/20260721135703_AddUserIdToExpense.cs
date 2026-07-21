using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastosApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Expenses");
        }
    }
}
