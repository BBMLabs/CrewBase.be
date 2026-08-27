using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomMaxBoats",
                table: "identity_companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomMaxBranches",
                table: "identity_companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomMaxMembers",
                table: "identity_companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "identity_companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Mico");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMaxBoats",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomMaxBranches",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "CustomMaxMembers",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "identity_companies");
        }
    }
}
