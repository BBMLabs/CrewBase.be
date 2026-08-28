using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySubscriptionBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_company_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IyzicoPaymentReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_company_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identity_company_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IyzicoCustomerReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IyzicoSubscriptionReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentPeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PendingPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PendingPlanEffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_company_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identity_company_payments_CompanyId",
                table: "identity_company_payments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_identity_company_payments_IyzicoPaymentReferenceCode",
                table: "identity_company_payments",
                column: "IyzicoPaymentReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_company_subscriptions_CompanyId",
                table: "identity_company_subscriptions",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_company_subscriptions_IyzicoSubscriptionReferenceC~",
                table: "identity_company_subscriptions",
                column: "IyzicoSubscriptionReferenceCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_company_payments");

            migrationBuilder.DropTable(
                name: "identity_company_subscriptions");
        }
    }
}
