using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageMagicCore.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsGuest", table: "Users");
        }
    }
}

