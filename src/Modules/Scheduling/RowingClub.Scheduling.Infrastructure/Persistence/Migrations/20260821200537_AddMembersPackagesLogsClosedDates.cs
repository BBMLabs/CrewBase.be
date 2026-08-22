using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembersPackagesLogsClosedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LessonPackageId",
                table: "appointments",
                newName: "CustomerPackageId");

            migrationBuilder.AddColumn<int>(
                name: "DefaultReminderMinutes",
                table: "customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailIndex",
                table: "customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "closed_dates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_closed_dates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    RemainingSessions = table.Column<int>(type: "integer", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_packages_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_packages_lesson_packages_LessonPackageId",
                        column: x => x.LessonPackageId,
                        principalTable: "lesson_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_logs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_EmailIndex",
                table: "customers",
                column: "EmailIndex",
                unique: true,
                filter: "\"EmailIndex\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_CustomerPackageId",
                table: "appointments",
                column: "CustomerPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_closed_dates_Date",
                table: "closed_dates",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_packages_CustomerId",
                table: "customer_packages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packages_LessonPackageId",
                table: "customer_packages",
                column: "LessonPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_member_logs_CustomerId_AtUtc",
                table: "member_logs",
                columns: new[] { "CustomerId", "AtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_customer_packages_CustomerPackageId",
                table: "appointments",
                column: "CustomerPackageId",
                principalTable: "customer_packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_customer_packages_CustomerPackageId",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "closed_dates");

            migrationBuilder.DropTable(
                name: "customer_packages");

            migrationBuilder.DropTable(
                name: "member_logs");

            migrationBuilder.DropIndex(
                name: "IX_customers_EmailIndex",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_appointments_CustomerPackageId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DefaultReminderMinutes",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "EmailIndex",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "CustomerPackageId",
                table: "appointments",
                newName: "LessonPackageId");
        }
    }
}
