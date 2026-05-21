using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageMagicCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRenegadeMatriarchAndFinalRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RedGamesWon",
                table: "UserStats",
                newName: "RenegadeGamesWon");

            migrationBuilder.RenameColumn(
                name: "RedGamesPlayed",
                table: "UserStats",
                newName: "RenegadeGamesPlayed");

            migrationBuilder.AddColumn<int>(
                name: "MatriarchTriggered",
                table: "UserStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MatriarchWins",
                table: "UserStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OutlawGamesPlayed",
                table: "UserStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OutlawGamesWon",
                table: "UserStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FinalRole",
                table: "MatchParticipants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatriarchUserId",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatriarchUserId",
                table: "Matches",
                column: "MatriarchUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_MatriarchUserId",
                table: "Matches",
                column: "MatriarchUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_MatriarchUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_MatriarchUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatriarchTriggered",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MatriarchWins",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "OutlawGamesPlayed",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "OutlawGamesWon",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "FinalRole",
                table: "MatchParticipants");

            migrationBuilder.DropColumn(
                name: "MatriarchUserId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "RenegadeGamesWon",
                table: "UserStats",
                newName: "RedGamesWon");

            migrationBuilder.RenameColumn(
                name: "RenegadeGamesPlayed",
                table: "UserStats",
                newName: "RedGamesPlayed");
        }
    }
}
