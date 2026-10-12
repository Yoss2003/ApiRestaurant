using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonqoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablesDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "mesas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "mesas");
        }
    }
}
