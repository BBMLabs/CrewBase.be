using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogsAndSiteMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BlockedByUserId",
                table: "blocked_ip_addresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BlockedByUserId",
                table: "blocked_ip_addresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
