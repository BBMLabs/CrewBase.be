using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    public partial class AddAppointmentRsvp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RsvpChoice",
                table: "appointments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RsvpDeadlineUtc",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RsvpResolvedAtUtc",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RsvpTokenHash",
                table: "appointments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_RsvpDeadlineUtc",
                table: "appointments",
                column: "RsvpDeadlineUtc",
                filter: "\"RsvpResolvedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_RsvpTokenHash",
                table: "appointments",
                column: "RsvpTokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_RsvpDeadlineUtc",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_RsvpTokenHash",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RsvpChoice",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RsvpDeadlineUtc",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RsvpResolvedAtUtc",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RsvpTokenHash",
                table: "appointments");
        }
    }
}
