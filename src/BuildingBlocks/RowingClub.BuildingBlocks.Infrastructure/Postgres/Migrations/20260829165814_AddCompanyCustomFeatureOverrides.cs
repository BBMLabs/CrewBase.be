using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCustomFeatureOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CustomCanExportData",
                table: "identity_companies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CustomHasAdvancedReports",
                table: "identity_companies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CustomHasAutomaticDuesReminders",
                table: "identity_companies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomMaxEmployees",
                table: "identity_companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomMaxManagers",
                table: "identity_companies",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomCanExportData",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomHasAdvancedReports",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomHasAutomaticDuesReminders",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomMaxEmployees",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomMaxManagers",
                table: "identity_companies");
        }
    }
}
