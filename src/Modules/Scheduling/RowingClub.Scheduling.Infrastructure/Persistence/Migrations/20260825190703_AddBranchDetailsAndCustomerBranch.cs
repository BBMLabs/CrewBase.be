using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchDetailsAndCustomerBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                table: "branches",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "branches",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerEmail",
                table: "branches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerName",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerPhone",
                table: "branches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                table: "branches",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_BranchId",
                table: "customers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_customers_branches_BranchId",
                table: "customers",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_branches_BranchId",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_BranchId",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ManagerEmail",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ManagerName",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ManagerPhone",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "branches");
        }
    }
}
