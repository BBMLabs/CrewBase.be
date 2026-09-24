using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    public partial class AddSiteMessagePhone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "site_messages",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phone",
                table: "site_messages");
        }
    }
}
