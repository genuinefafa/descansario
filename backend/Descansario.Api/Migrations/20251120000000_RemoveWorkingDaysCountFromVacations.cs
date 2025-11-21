using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Descansario.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkingDaysCountFromVacations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar la columna WorkingDaysCount de la tabla Vacations
            // Este campo se calculará dinámicamente usando WorkingDaysCalculator
            migrationBuilder.DropColumn(
                name: "WorkingDaysCount",
                table: "Vacations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Si necesitamos hacer rollback, recrear la columna
            migrationBuilder.AddColumn<int>(
                name: "WorkingDaysCount",
                table: "Vacations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
