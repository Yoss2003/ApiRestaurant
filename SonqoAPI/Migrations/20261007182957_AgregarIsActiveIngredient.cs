using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonqoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIsActiveIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ingredientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ingredientes");
        }
    }
}
