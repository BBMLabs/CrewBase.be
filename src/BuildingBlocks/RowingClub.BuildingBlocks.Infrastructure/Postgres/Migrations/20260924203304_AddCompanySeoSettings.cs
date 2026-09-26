using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    public partial class AddCompanySeoSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowIndexing",
                table: "identity_companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleAnalyticsId",
                table: "identity_companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSiteVerification",
                table: "identity_companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "identity_companies",
                type: "character varying(170)",
                maxLength: 170,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoKeywords",
                table: "identity_companies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "identity_companies",
                type: "character varying(70)",
                maxLength: 70,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowIndexing",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "GoogleAnalyticsId",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "GoogleSiteVerification",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "SeoKeywords",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "identity_companies");
        }
    }
}
