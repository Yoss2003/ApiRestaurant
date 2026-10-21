using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantApi.Migrations
{
    /// <inheritdoc />
    public partial class EntidadMovimientoCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId",
                table: "movimientos_caja",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_PaymentMethodId",
                table: "movimientos_caja",
                column: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_caja_metodos_pago_PaymentMethodId",
                table: "movimientos_caja",
                column: "PaymentMethodId",
                principalTable: "metodos_pago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_caja_metodos_pago_PaymentMethodId",
                table: "movimientos_caja");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_caja_PaymentMethodId",
                table: "movimientos_caja");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "movimientos_caja");
        }
    }
}
