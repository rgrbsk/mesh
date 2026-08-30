using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class EtapasEKanban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EtapaId",
                table: "Solicitacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Etapas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_EtapaId",
                table: "Solicitacoes",
                column: "EtapaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitacoes_Etapas_EtapaId",
                table: "Solicitacoes",
                column: "EtapaId",
                principalTable: "Etapas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitacoes_Etapas_EtapaId",
                table: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "Etapas");

            migrationBuilder.DropIndex(
                name: "IX_Solicitacoes_EtapaId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "EtapaId",
                table: "Solicitacoes");
        }
    }
}
