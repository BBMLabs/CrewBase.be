using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCustomMaxInstructors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomMaxInstructors",
                table: "identity_companies",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMaxInstructors",
                table: "identity_companies");
        }
    }
}
