using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    public partial class AddFeedPolls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPoll",
                table: "posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PollClosesOn",
                table: "posts",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BlockedByUserId",
                table: "blocked_ip_addresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_options_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_votes",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_votes", x => new { x.PostId, x.CustomerId });
                    table.ForeignKey(
                        name: "FK_poll_votes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_votes_poll_options_OptionId",
                        column: x => x.OptionId,
                        principalTable: "poll_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_votes_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_poll_options_PostId_Order",
                table: "poll_options",
                columns: new[] { "PostId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_CustomerId",
                table: "poll_votes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_OptionId",
                table: "poll_votes",
                column: "OptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "poll_votes");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropColumn(
                name: "IsPoll",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "PollClosesOn",
                table: "posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "BlockedByUserId",
                table: "blocked_ip_addresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
