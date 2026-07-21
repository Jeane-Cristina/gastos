using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GastosApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MonthlyIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    SavingsGoal = table.Column<decimal>(type: "numeric", nullable: false),
                    ShortTermGoal = table.Column<string>(type: "text", nullable: false),
                    MediumTermGoal = table.Column<string>(type: "text", nullable: false),
                    LongTermGoal = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialProfiles");
        }
    }
}
