using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySiteThemeAndSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColorHex",
                table: "identity_companies",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#ea580c");

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "identity_companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FontFamily",
                table: "identity_companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Roboto");

            migrationBuilder.AddColumn<string>(
                name: "HeroAnimation",
                table: "identity_companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "fade");

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "identity_companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedinUrl",
                table: "identity_companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColorHex",
                table: "identity_companies",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#155e75");

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "identity_companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColorHex",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "FontFamily",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "HeroAnimation",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "LinkedinUrl",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "PrimaryColorHex",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "identity_companies");
        }
    }
}
