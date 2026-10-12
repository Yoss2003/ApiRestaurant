using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonqoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCategoriaInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ingredientes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ingredientes");
        }
    }
}
