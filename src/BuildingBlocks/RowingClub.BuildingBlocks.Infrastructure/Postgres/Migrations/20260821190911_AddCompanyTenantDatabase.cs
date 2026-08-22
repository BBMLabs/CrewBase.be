using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTenantDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabaseName",
                table: "identity_companies",
                type: "character varying(63)",
                maxLength: 63,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                table: "identity_companies",
                type: "character varying(63)",
                maxLength: 63,
                nullable: false,
                defaultValue: "");

            // Mevcut firmalara adlarından türetilmiş benzersiz bir subdomain/veritabanı adı ver
            // (unique index'ten önce; Id ön eki çakışmayı imkansızlaştırır). Bu firmaların tenant
            // veritabanları ilk erişimde provision edilene kadar fiziksel olarak mevcut olmayabilir.
            migrationBuilder.Sql("""
                UPDATE identity_companies SET
                  "Subdomain" = substr(
                    trim(both '-' from regexp_replace(
                      lower(translate("Name", 'çğıöşüÇĞİÖŞÜ', 'cgiosucgiosu')),
                      '[^a-z0-9]+', '-', 'g')), 1, 40) || '-' || substr("Id"::text, 1, 8),
                  "DatabaseName" = 'tenant_' || replace(
                    substr(
                      trim(both '-' from regexp_replace(
                        lower(translate("Name", 'çğıöşüÇĞİÖŞÜ', 'cgiosucgiosu')),
                        '[^a-z0-9]+', '-', 'g')), 1, 40) || '-' || substr("Id"::text, 1, 8),
                    '-', '_')
                WHERE "Subdomain" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_identity_companies_Subdomain",
                table: "identity_companies",
                column: "Subdomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_identity_companies_Subdomain",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "DatabaseName",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                table: "identity_companies");
        }
    }
}
