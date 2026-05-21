using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageMagicCore.Migrations
{
    /// <inheritdoc />
    public partial class AddScryfallFieldsToDecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommanderImageUri",
                table: "Decks",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScryfallId",
                table: "Decks",
                type: "TEXT",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommanderImageUri",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "ScryfallId",
                table: "Decks");
        }
    }
}
