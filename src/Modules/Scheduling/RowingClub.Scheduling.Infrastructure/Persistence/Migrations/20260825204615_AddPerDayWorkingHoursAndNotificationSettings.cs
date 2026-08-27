using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerDayWorkingHoursAndNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "OpenDaysMask",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "company_settings");

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnCancellation",
                table: "company_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewAppointment",
                table: "company_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendCustomerReminders",
                table: "company_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "company_day_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanySettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    OpeningTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ClosingTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_day_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_day_schedules_company_settings_CompanySettingsId",
                        column: x => x.CompanySettingsId,
                        principalTable: "company_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_day_schedules_CompanySettingsId_Day",
                table: "company_day_schedules",
                columns: new[] { "CompanySettingsId", "Day" },
                unique: true);

            // Var olan firma ayarları için haftanın 7 günü de varsayılan 09:00-18:00 açık olarak tohumlanır.
            migrationBuilder.Sql(@"
                INSERT INTO company_day_schedules (""Id"", ""CompanySettingsId"", ""Day"", ""IsOpen"", ""OpeningTime"", ""ClosingTime"")
                SELECT gen_random_uuid(), cs.""Id"", d.day, true, TIME '09:00:00', TIME '18:00:00'
                FROM company_settings cs
                CROSS JOIN generate_series(0, 6) AS d(day);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_day_schedules");

            migrationBuilder.DropColumn(
                name: "NotifyOnCancellation",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewAppointment",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "SendCustomerReminders",
                table: "company_settings");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClosingTime",
                table: "company_settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "OpenDaysMask",
                table: "company_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpeningTime",
                table: "company_settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
