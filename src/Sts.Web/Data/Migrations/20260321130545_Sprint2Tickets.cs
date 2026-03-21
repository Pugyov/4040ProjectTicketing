using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sts.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2Tickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Tickets",
                newName: "Subject");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tickets",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "Tickets",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Development")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE Tickets t
                INNER JOIN AspNetUsers u ON t.CreatedByUserId = u.Id
                SET t.Team = u.Team
                WHERE t.Team = 'Development';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "Tickets",
                newName: "Title");

            migrationBuilder.UpdateData(
                table: "Tickets",
                keyColumn: "Description",
                keyValue: null,
                column: "Description",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tickets",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
