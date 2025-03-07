using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCounterAPI.Migrations
{
    /// <inheritdoc />
    public partial class m2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartingLife",
                table: "games",
                newName: "MaxLife");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxLife",
                table: "games",
                newName: "StartingLife");
        }
    }
}
