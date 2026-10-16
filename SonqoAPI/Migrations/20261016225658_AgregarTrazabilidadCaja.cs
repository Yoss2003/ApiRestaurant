using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonqoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTrazabilidadCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashRegisterId",
                table: "pagos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "pagos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_CashRegisterId",
                table: "pagos",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_UserId",
                table: "pagos",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_pagos_CashRegisters_CashRegisterId",
                table: "pagos",
                column: "CashRegisterId",
                principalTable: "CashRegisters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pagos_usuarios_UserId",
                table: "pagos",
                column: "UserId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pagos_CashRegisters_CashRegisterId",
                table: "pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_pagos_usuarios_UserId",
                table: "pagos");

            migrationBuilder.DropIndex(
                name: "IX_pagos_CashRegisterId",
                table: "pagos");

            migrationBuilder.DropIndex(
                name: "IX_pagos_UserId",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "CashRegisterId",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "pagos");
        }
    }
}
