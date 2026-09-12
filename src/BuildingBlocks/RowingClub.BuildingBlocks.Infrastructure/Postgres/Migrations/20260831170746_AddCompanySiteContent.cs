using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySiteContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutText",
                table: "identity_companies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Kulübümüz, her seviyeden kürekçiye açık; deneyimli eğitmenlerimiz eşliğinde güvenli ve keyifli bir kürek deneyimi sunuyoruz. İster ilk kez küreğe oturacak olun ister yarışlara hazırlanıyor olun, sizin için uygun tekne ve programı birlikte bulalım.");

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "identity_companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Suyla tanışmanın en keyifli yolu.");

            migrationBuilder.CreateTable(
                name: "identity_company_gallery_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_company_gallery_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identity_company_gallery_images_identity_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "identity_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identity_company_gallery_images_CompanyId",
                table: "identity_company_gallery_images",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_company_gallery_images");

            migrationBuilder.DropColumn(
                name: "AboutText",
                table: "identity_companies");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "identity_companies");
        }
    }
}
