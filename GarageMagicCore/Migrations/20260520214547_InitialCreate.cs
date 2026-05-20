using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageMagicCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SettingKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Quarter = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CurrentPrestigeLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Betrayals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BetrayerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    VictimUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BetrayalDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Betrayals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Betrayals_Users_BetrayerUserId",
                        column: x => x.BetrayerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Betrayals_Users_VictimUserId",
                        column: x => x.VictimUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CommanderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColorIdentity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrestigeLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrestigeLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrestigeLevels_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrestigeLevels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWins = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalLosses = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalMatches = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Wins1v1v1 = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Wins1v1v1v1 = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    WinsSheriff = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SheriffGamesPlayed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SheriffGamesWon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    DeputyGamesPlayed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    DeputyGamesWon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RedGamesPlayed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RedGamesWon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    WinsPerDeckJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStats_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeckId = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchType = table.Column<string>(type: "TEXT", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SheriffUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Matches_Users_SheriffUserId",
                        column: x => x.SheriffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MatchParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckId = table.Column<int>(type: "INTEGER", nullable: true),
                    HiddenRole = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchParticipants_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MatchParticipants_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchWinners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchWinners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchWinners_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchWinners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_SettingKey",
                table: "AppSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Betrayals_BetrayalDate",
                table: "Betrayals",
                column: "BetrayalDate");

            migrationBuilder.CreateIndex(
                name: "IX_Betrayals_BetrayerUserId",
                table: "Betrayals",
                column: "BetrayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Betrayals_VictimUserId",
                table: "Betrayals",
                column: "VictimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_UserId_DeckName",
                table: "Decks",
                columns: new[] { "UserId", "DeckName" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DeckId",
                table: "Matches",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchDate",
                table: "Matches",
                column: "MatchDate");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchType",
                table: "Matches",
                column: "MatchType");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SheriffUserId",
                table: "Matches",
                column: "SheriffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipants_DeckId",
                table: "MatchParticipants",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipants_MatchId_UserId",
                table: "MatchParticipants",
                columns: new[] { "MatchId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipants_UserId",
                table: "MatchParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchWinners_MatchId_UserId",
                table: "MatchWinners",
                columns: new[] { "MatchId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchWinners_UserId",
                table: "MatchWinners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrestigeLevels_SeasonId",
                table: "PrestigeLevels",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PrestigeLevels_UserId_SeasonId_Level",
                table: "PrestigeLevels",
                columns: new[] { "UserId", "SeasonId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_IsActive",
                table: "Seasons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Year_Quarter",
                table: "Seasons",
                columns: new[] { "Year", "Quarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_SeasonId",
                table: "UserStats",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_UserId_SeasonId",
                table: "UserStats",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Betrayals");

            migrationBuilder.DropTable(
                name: "MatchParticipants");

            migrationBuilder.DropTable(
                name: "MatchWinners");

            migrationBuilder.DropTable(
                name: "PrestigeLevels");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
