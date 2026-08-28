using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageExpiryAndCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CampaignEndsAtUtc",
                table: "lesson_packages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CampaignStartsAtUtc",
                table: "lesson_packages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "lesson_packages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidityDays",
                table: "lesson_packages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "customer_packages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReminderDaysBeforeExpiry",
                table: "customer_packages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReferenceCode",
                table: "customer_packages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "customer_packages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Assigned");

            migrationBuilder.AddColumn<string>(
                name: "PackageExpiryReminderDaysCsv",
                table: "company_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "15,7");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packages_ExpiresAtUtc",
                table: "customer_packages",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_packages_ExpiresAtUtc",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "CampaignEndsAtUtc",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "CampaignStartsAtUtc",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "ValidityDays",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "LastReminderDaysBeforeExpiry",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "PaymentReferenceCode",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "PackageExpiryReminderDaysCsv",
                table: "company_settings");
        }
    }
}
