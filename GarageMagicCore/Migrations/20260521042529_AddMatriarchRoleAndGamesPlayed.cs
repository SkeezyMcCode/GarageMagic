using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageMagicCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMatriarchRoleAndGamesPlayed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatriarchGamesPlayed",
                table: "UserStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatriarchGamesPlayed",
                table: "UserStats");
        }
    }
}
