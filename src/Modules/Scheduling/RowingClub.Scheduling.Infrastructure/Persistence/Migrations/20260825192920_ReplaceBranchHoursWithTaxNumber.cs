using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBranchHoursWithTaxNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "branches");

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "branches",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "branches");

            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                table: "branches",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                table: "branches",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }
    }
}
