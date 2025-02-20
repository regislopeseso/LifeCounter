using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCounterAPI.Migrations
{
    /// <inheritdoc />
    public partial class Mi2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FixedMaxLife",
                table: "games",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedMaxLife",
                table: "games");
        }
    }
}
