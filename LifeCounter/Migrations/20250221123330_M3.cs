using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCounterAPI.Migrations
{
    /// <inheritdoc />
    public partial class M3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartingLifeTotal",
                table: "players",
                newName: "StartingLife");

            migrationBuilder.RenameColumn(
                name: "CurrentLifeTotal",
                table: "players",
                newName: "CurrentLife");

            migrationBuilder.RenameColumn(
                name: "LifeTotal",
                table: "games",
                newName: "StartingLife");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartingLife",
                table: "players",
                newName: "StartingLifeTotal");

            migrationBuilder.RenameColumn(
                name: "CurrentLife",
                table: "players",
                newName: "CurrentLifeTotal");

            migrationBuilder.RenameColumn(
                name: "StartingLife",
                table: "games",
                newName: "LifeTotal");
        }
    }
}
