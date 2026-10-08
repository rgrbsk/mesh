using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class CotacaoItemOrigens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CotacaoItemId",
                table: "ItensSolicitacao",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensSolicitacao_CotacaoItemId",
                table: "ItensSolicitacao",
                column: "CotacaoItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItensSolicitacao_CotacaoItens_CotacaoItemId",
                table: "ItensSolicitacao",
                column: "CotacaoItemId",
                principalTable: "CotacaoItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Itens já cotados antes desta coluna: a linha de cotação apontava
            // para um único item de solicitação.
            migrationBuilder.Sql(
                @"UPDATE ""ItensSolicitacao"" s SET ""CotacaoItemId"" = ci.""Id""
                  FROM ""CotacaoItens"" ci WHERE ci.""ItemSolicitacaoId"" = s.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensSolicitacao_CotacaoItens_CotacaoItemId",
                table: "ItensSolicitacao");

            migrationBuilder.DropIndex(
                name: "IX_ItensSolicitacao_CotacaoItemId",
                table: "ItensSolicitacao");

            migrationBuilder.DropColumn(
                name: "CotacaoItemId",
                table: "ItensSolicitacao");
        }
    }
}
