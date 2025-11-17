using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Descansario.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToVacations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Vacations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Vacations");
        }
    }
}
