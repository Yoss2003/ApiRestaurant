using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SonqoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "platos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "mesas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "ingredientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "CashRegisters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_RestaurantId",
                table: "usuarios",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_platos_RestaurantId",
                table: "platos",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_RestaurantId",
                table: "pedidos",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_mesas_RestaurantId",
                table: "mesas",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_ingredientes_RestaurantId",
                table: "ingredientes",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_RestaurantId",
                table: "CashRegisters",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisters_Restaurants_RestaurantId",
                table: "CashRegisters",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ingredientes_Restaurants_RestaurantId",
                table: "ingredientes",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_mesas_Restaurants_RestaurantId",
                table: "mesas",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedidos_Restaurants_RestaurantId",
                table: "pedidos",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_platos_Restaurants_RestaurantId",
                table: "platos",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_Restaurants_RestaurantId",
                table: "usuarios",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisters_Restaurants_RestaurantId",
                table: "CashRegisters");

            migrationBuilder.DropForeignKey(
                name: "FK_ingredientes_Restaurants_RestaurantId",
                table: "ingredientes");

            migrationBuilder.DropForeignKey(
                name: "FK_mesas_Restaurants_RestaurantId",
                table: "mesas");

            migrationBuilder.DropForeignKey(
                name: "FK_pedidos_Restaurants_RestaurantId",
                table: "pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_platos_Restaurants_RestaurantId",
                table: "platos");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_Restaurants_RestaurantId",
                table: "usuarios");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_RestaurantId",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "IX_platos_RestaurantId",
                table: "platos");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_RestaurantId",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_mesas_RestaurantId",
                table: "mesas");

            migrationBuilder.DropIndex(
                name: "IX_ingredientes_RestaurantId",
                table: "ingredientes");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_RestaurantId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "platos");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "mesas");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "ingredientes");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "CashRegisters");
        }
    }
}
