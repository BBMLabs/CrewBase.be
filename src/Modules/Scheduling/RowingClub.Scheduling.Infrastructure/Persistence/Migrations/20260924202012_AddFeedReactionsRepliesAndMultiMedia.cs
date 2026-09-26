using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    public partial class AddFeedReactionsRepliesAndMultiMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_post_media",
                table: "post_media");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "post_media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "post_comments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "post_comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_post_media",
                table: "post_media",
                columns: new[] { "PostId", "Order" });

            migrationBuilder.CreateTable(
                name: "comment_likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment_likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comment_likes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_likes_post_comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "post_comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_reactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_reactions_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_reactions_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_ParentCommentId",
                table: "post_comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_comment_likes_CommentId_Club",
                table: "comment_likes",
                column: "CommentId",
                unique: true,
                filter: "\"CustomerId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_comment_likes_CommentId_CustomerId",
                table: "comment_likes",
                columns: new[] { "CommentId", "CustomerId" },
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_comment_likes_CustomerId",
                table: "comment_likes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_post_reactions_CustomerId",
                table: "post_reactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_post_reactions_PostId_Club",
                table: "post_reactions",
                column: "PostId",
                unique: true,
                filter: "\"CustomerId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_post_reactions_PostId_CustomerId",
                table: "post_reactions",
                columns: new[] { "PostId", "CustomerId" },
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments",
                column: "ParentCommentId",
                principalTable: "post_comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                "INSERT INTO post_reactions (\"Id\", \"PostId\", \"CustomerId\", \"Emoji\", \"AtUtc\") " +
                "SELECT md5(\"PostId\"::text || \"CustomerId\"::text)::uuid, \"PostId\", \"CustomerId\", '👍', \"AtUtc\" " +
                "FROM post_likes ON CONFLICT DO NOTHING;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM post_comments WHERE \"CustomerId\" IS NULL;");

            migrationBuilder.Sql("DELETE FROM post_media WHERE \"Order\" > 0;");

            migrationBuilder.DropForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments");

            migrationBuilder.DropTable(
                name: "comment_likes");

            migrationBuilder.DropTable(
                name: "post_reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_post_media",
                table: "post_media");

            migrationBuilder.DropIndex(
                name: "IX_post_comments_ParentCommentId",
                table: "post_comments");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "post_media");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "post_comments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "post_comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_post_media",
                table: "post_media",
                column: "PostId");
        }
    }
}
