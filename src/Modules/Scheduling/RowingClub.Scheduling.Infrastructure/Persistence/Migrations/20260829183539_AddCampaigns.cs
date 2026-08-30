using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "customer_packages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePaid",
                table: "customer_packages",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MinLevel = table.Column<int>(type: "integer", nullable: true),
                    MaxLevel = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaigns_lesson_packages_LessonPackageId",
                        column: x => x.LessonPackageId,
                        principalTable: "lesson_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO campaigns ("Id", "LessonPackageId", "StartsAtUtc", "EndsAtUtc", "Price", "MinLevel", "MaxLevel", "CreatedAtUtc")
                SELECT gen_random_uuid(), "Id", "CampaignStartsAtUtc", "CampaignEndsAtUtc", COALESCE("CampaignPrice", "Price"), NULL, NULL, now()
                FROM lesson_packages
                WHERE "CampaignStartsAtUtc" IS NOT NULL AND "CampaignEndsAtUtc" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "CampaignEndsAtUtc",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "CampaignPrice",
                table: "lesson_packages");

            migrationBuilder.DropColumn(
                name: "CampaignStartsAtUtc",
                table: "lesson_packages");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packages_CampaignId",
                table: "customer_packages",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_LessonPackageId",
                table: "campaigns",
                column: "LessonPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_packages_campaigns_CampaignId",
                table: "customer_packages",
                column: "CampaignId",
                principalTable: "campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_packages_campaigns_CampaignId",
                table: "customer_packages");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropIndex(
                name: "IX_customer_packages_CampaignId",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "customer_packages");

            migrationBuilder.DropColumn(
                name: "PricePaid",
                table: "customer_packages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CampaignEndsAtUtc",
                table: "lesson_packages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CampaignPrice",
                table: "lesson_packages",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CampaignStartsAtUtc",
                table: "lesson_packages",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
