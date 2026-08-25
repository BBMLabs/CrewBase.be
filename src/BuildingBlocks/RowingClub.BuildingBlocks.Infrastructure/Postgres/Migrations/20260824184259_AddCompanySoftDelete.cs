using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "identity_companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "identity_companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "identity_companies");
        }
    }
}
