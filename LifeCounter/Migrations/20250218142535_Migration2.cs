using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCounterAPI.Migrations
{
    /// <inheritdoc />
    public partial class Migration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Games_GameId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Matches_MatchId",
                table: "Players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matches",
                table: "Matches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Games",
                table: "Games");

            migrationBuilder.RenameTable(
                name: "Players",
                newName: "players");

            migrationBuilder.RenameTable(
                name: "Matches",
                newName: "matches");

            migrationBuilder.RenameTable(
                name: "Games",
                newName: "games");

            migrationBuilder.RenameIndex(
                name: "IX_Players_MatchId",
                table: "players",
                newName: "IX_players_MatchId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_GameId",
                table: "matches",
                newName: "IX_matches_GameId");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "matches",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_players",
                table: "players",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_matches",
                table: "matches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_games",
                table: "games",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_games_GameId",
                table: "matches",
                column: "GameId",
                principalTable: "games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_players_matches_MatchId",
                table: "players",
                column: "MatchId",
                principalTable: "matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_matches_games_GameId",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_players_matches_MatchId",
                table: "players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_players",
                table: "players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_matches",
                table: "matches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_games",
                table: "games");

            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "matches");

            migrationBuilder.RenameTable(
                name: "players",
                newName: "Players");

            migrationBuilder.RenameTable(
                name: "matches",
                newName: "Matches");

            migrationBuilder.RenameTable(
                name: "games",
                newName: "Games");

            migrationBuilder.RenameIndex(
                name: "IX_players_MatchId",
                table: "Players",
                newName: "IX_Players_MatchId");

            migrationBuilder.RenameIndex(
                name: "IX_matches_GameId",
                table: "Matches",
                newName: "IX_Matches_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matches",
                table: "Matches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Games",
                table: "Games",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Games_GameId",
                table: "Matches",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Matches_MatchId",
                table: "Players",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
