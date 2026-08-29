using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPackagePaymentReferenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE customer_packages cp
                SET "PaymentReferenceCode" = NULL
                FROM (
                    SELECT "Id",
                           ROW_NUMBER() OVER (PARTITION BY "PaymentReferenceCode" ORDER BY "AssignedAtUtc", "Id") AS rn
                    FROM customer_packages
                    WHERE "PaymentReferenceCode" IS NOT NULL
                ) ranked
                WHERE cp."Id" = ranked."Id" AND ranked.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_customer_packages_PaymentReferenceCode",
                table: "customer_packages",
                column: "PaymentReferenceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_packages_PaymentReferenceCode",
                table: "customer_packages");
        }
    }
}
