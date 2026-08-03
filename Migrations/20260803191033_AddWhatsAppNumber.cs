using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastosApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Users");
        }
    }
}
