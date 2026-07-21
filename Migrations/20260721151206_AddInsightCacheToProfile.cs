using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastosApi.Migrations
{
    /// <inheritdoc />
    public partial class AddInsightCacheToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastInsight",
                table: "FinancialProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInsightGeneratedAt",
                table: "FinancialProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastInsight",
                table: "FinancialProfiles");

            migrationBuilder.DropColumn(
                name: "LastInsightGeneratedAt",
                table: "FinancialProfiles");
        }
    }
}
