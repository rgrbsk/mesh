using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class Comunicados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Confirmada",
                table: "Notificacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmadaEm",
                table: "Notificacoes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnvioId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remetente",
                table: "Notificacoes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Notificacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confirmada",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "ConfirmadaEm",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "EnvioId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "Remetente",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Notificacoes");
        }
    }
}
