using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIpBlockingAndActivityLogNetworkInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "activity_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "activity_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blocked_ip_addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BlockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_ip_addresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_IpAddress",
                table: "activity_logs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_blocked_ip_addresses_IpAddress",
                table: "blocked_ip_addresses",
                column: "IpAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_ip_addresses");

            migrationBuilder.DropIndex(
                name: "IX_activity_logs_IpAddress",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "activity_logs");
        }
    }
}
