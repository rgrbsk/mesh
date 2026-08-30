using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class EscolhaVencedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConviteVencedorId",
                table: "CotacaoItens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscolhidoEm",
                table: "CotacaoItens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscolhidoPorId",
                table: "CotacaoItens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEscolha",
                table: "CotacaoItens",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_ConviteVencedorId",
                table: "CotacaoItens",
                column: "ConviteVencedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_EscolhidoPorId",
                table: "CotacaoItens",
                column: "EscolhidoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CotacaoItens_AspNetUsers_EscolhidoPorId",
                table: "CotacaoItens",
                column: "EscolhidoPorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CotacaoItens_Convites_ConviteVencedorId",
                table: "CotacaoItens",
                column: "ConviteVencedorId",
                principalTable: "Convites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CotacaoItens_AspNetUsers_EscolhidoPorId",
                table: "CotacaoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_CotacaoItens_Convites_ConviteVencedorId",
                table: "CotacaoItens");

            migrationBuilder.DropIndex(
                name: "IX_CotacaoItens_ConviteVencedorId",
                table: "CotacaoItens");

            migrationBuilder.DropIndex(
                name: "IX_CotacaoItens_EscolhidoPorId",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "ConviteVencedorId",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "EscolhidoEm",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "EscolhidoPorId",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "MotivoEscolha",
                table: "CotacaoItens");
        }
    }
}
