using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "instructors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "boats",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_instructors_BranchId",
                table: "instructors",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_boats_BranchId",
                table: "boats",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_boats_branches_BranchId",
                table: "boats",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_instructors_branches_BranchId",
                table: "instructors",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_boats_branches_BranchId",
                table: "boats");

            migrationBuilder.DropForeignKey(
                name: "FK_instructors_branches_BranchId",
                table: "instructors");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropIndex(
                name: "IX_instructors_BranchId",
                table: "instructors");

            migrationBuilder.DropIndex(
                name: "IX_boats_BranchId",
                table: "boats");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "instructors");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "boats");
        }
    }
}
