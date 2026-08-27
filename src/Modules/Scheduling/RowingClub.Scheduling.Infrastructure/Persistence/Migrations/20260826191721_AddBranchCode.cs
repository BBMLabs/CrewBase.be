using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowingClub.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "branches",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            // Var olan şubelere geriye dönük tekil kod ataması (6 rakam + 2 büyük harf) - EF'in
            // scaffold ettiği boş varsayılan değer, birden fazla şube varsa benzersizlik indeksini
            // ihlal eder; bu yüzden elle rastgele/benzersiz kod üretilir.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    branch_row RECORD;
                    new_code text;
                BEGIN
                    FOR branch_row IN SELECT "Id" FROM branches WHERE "Code" IS NULL LOOP
                        LOOP
                            new_code := lpad(floor(random() * 1000000)::int::text, 6, '0') ||
                                        chr(65 + floor(random() * 26)::int) ||
                                        chr(65 + floor(random() * 26)::int);
                            EXIT WHEN NOT EXISTS (SELECT 1 FROM branches WHERE "Code" = new_code);
                        END LOOP;
                        UPDATE branches SET "Code" = new_code WHERE "Id" = branch_row."Id";
                    END LOOP;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "branches",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_Code",
                table: "branches",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branches_Code",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "branches");
        }
    }
}
