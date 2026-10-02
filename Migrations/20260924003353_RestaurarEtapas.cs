using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <summary>
    /// Recria a tabela Etapas e a coluna Solicitacoes.EtapaId.
    ///
    /// Escrita à mão de propósito: o snapshot do modelo voltou a descrever as
    /// Etapas, mas a migration RemoveEtapas já tinha derrubado a tabela no
    /// banco. Para o EF os dois lados batem, então ele não gera diferença
    /// nenhuma — e o banco ficaria sem a tabela que o modelo jura existir.
    /// O conteúdo daqui é o Down da RemoveEtapas, que é exatamente o desfazer
    /// daquela remoção.
    /// </summary>
    public partial class RestaurarEtapas : Migration
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
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false)
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
