using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class SolicitacaoCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    EnviadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_AspNetUsers_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensSolicitacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitacaoId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    CentroCustoId = table.Column<int>(type: "integer", nullable: false),
                    PrazoDesejado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Justificativa = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MotivoDecisao = table.Column<string>(type: "text", nullable: true),
                    DecididoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecididoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensSolicitacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensSolicitacao_AspNetUsers_DecididoPorId",
                        column: x => x.DecididoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensSolicitacao_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensSolicitacao_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensSolicitacao_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensSolicitacao_CentroCustoId",
                table: "ItensSolicitacao",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensSolicitacao_DecididoPorId",
                table: "ItensSolicitacao",
                column: "DecididoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensSolicitacao_ProdutoId",
                table: "ItensSolicitacao",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensSolicitacao_SolicitacaoId",
                table: "ItensSolicitacao",
                column: "SolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_SolicitanteId",
                table: "Solicitacoes",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensSolicitacao");

            migrationBuilder.DropTable(
                name: "Solicitacoes");
        }
    }
}
