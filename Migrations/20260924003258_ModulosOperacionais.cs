using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class ModulosOperacionais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecoReferencia",
                table: "Produtos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrecoReferenciaEm",
                table: "Produtos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LiberadaEm",
                table: "NotasFiscais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LiberadaParaPagamento",
                table: "NotasFiscais",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LiberadaPorId",
                table: "NotasFiscais",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoLiberacao",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrdemCompraId",
                table: "NotasFiscais",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NivelAtual",
                table: "ItensSolicitacao",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorEstimado",
                table: "ItensSolicitacao",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "BloqueiaAcimaOrcamento",
                table: "CentrosCusto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Alcadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CentroCustoId = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    AprovadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LimiteValor = table.Column<decimal>(type: "numeric", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alcadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alcadas_AspNetUsers_AprovadorId",
                        column: x => x.AprovadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alcadas_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AprovacoesItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemSolicitacaoId = table.Column<int>(type: "integer", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    AprovadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AprovadorNome = table.Column<string>(type: "text", nullable: false),
                    EmNomeDeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmNomeDeNome = table.Column<string>(type: "text", nullable: true),
                    Decisao = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    LimiteNoMomento = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorNoMomento = table.Column<decimal>(type: "numeric", nullable: false),
                    DecididoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AprovacoesItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AprovacoesItem_AspNetUsers_AprovadorId",
                        column: x => x.AprovadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AprovacoesItem_ItensSolicitacao_ItemSolicitacaoId",
                        column: x => x.ItemSolicitacaoId,
                        principalTable: "ItensSolicitacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Delegacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TitularId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubstitutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Delegacoes_AspNetUsers_SubstitutoId",
                        column: x => x.SubstitutoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Delegacoes_AspNetUsers_TitularId",
                        column: x => x.TitularId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CentroCustoId = table.Column<int>(type: "integer", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orcamentos_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdensCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "text", nullable: false),
                    FornecedorId = table.Column<int>(type: "integer", nullable: false),
                    CotacaoId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EmitidaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnviadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrazoEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CondicaoPagamento = table.Column<string>(type: "text", nullable: false),
                    Frete = table.Column<string>(type: "text", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "text", nullable: true),
                    CriadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdensCompra_AspNetUsers_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensCompra_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensCompra_Pessoas_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensOrdemCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdemCompraId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    CotacaoItemId = table.Column<int>(type: "integer", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantidadeRecebida = table.Column<decimal>(type: "numeric", nullable: false),
                    PrazoEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensOrdemCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensOrdemCompra_CotacaoItens_CotacaoItemId",
                        column: x => x.CotacaoItemId,
                        principalTable: "CotacaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensOrdemCompra_OrdensCompra_OrdemCompraId",
                        column: x => x.OrdemCompraId,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensOrdemCompra_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recebimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "text", nullable: false),
                    OrdemCompraId = table.Column<int>(type: "integer", nullable: false),
                    RecebidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecebidoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecebidoPorNome = table.Column<string>(type: "text", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recebimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recebimentos_AspNetUsers_RecebidoPorId",
                        column: x => x.RecebidoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_OrdensCompra_OrdemCompraId",
                        column: x => x.OrdemCompraId,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TitulosPagar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "text", nullable: false),
                    FornecedorId = table.Column<int>(type: "integer", nullable: false),
                    NotaFiscalId = table.Column<int>(type: "integer", nullable: true),
                    OrdemCompraId = table.Column<int>(type: "integer", nullable: true),
                    Emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorPago = table.Column<decimal>(type: "numeric", nullable: false),
                    Parcela = table.Column<int>(type: "integer", nullable: false),
                    TotalParcelas = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "text", nullable: true),
                    CriadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitulosPagar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TitulosPagar_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TitulosPagar_OrdensCompra_OrdemCompraId",
                        column: x => x.OrdemCompraId,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TitulosPagar_Pessoas_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensRecebimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecebimentoId = table.Column<int>(type: "integer", nullable: false),
                    ItemOrdemCompraId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    ComAvaria = table.Column<bool>(type: "boolean", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensRecebimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensRecebimento_ItensOrdemCompra_ItemOrdemCompraId",
                        column: x => x.ItemOrdemCompraId,
                        principalTable: "ItensOrdemCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensRecebimento_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaixasTitulo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TituloPagarId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    FormaPagamento = table.Column<string>(type: "text", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    RegistradaPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistradaPorNome = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaixasTitulo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaixasTitulo_TitulosPagar_TituloPagarId",
                        column: x => x.TituloPagarId,
                        principalTable: "TitulosPagar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_OrdemCompraId",
                table: "NotasFiscais",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_Alcadas_AprovadorId",
                table: "Alcadas",
                column: "AprovadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Alcadas_CentroCustoId_Ordem",
                table: "Alcadas",
                columns: new[] { "CentroCustoId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AprovacoesItem_AprovadorId",
                table: "AprovacoesItem",
                column: "AprovadorId");

            migrationBuilder.CreateIndex(
                name: "IX_AprovacoesItem_ItemSolicitacaoId",
                table: "AprovacoesItem",
                column: "ItemSolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BaixasTitulo_TituloPagarId",
                table: "BaixasTitulo",
                column: "TituloPagarId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegacoes_SubstitutoId_Ativo",
                table: "Delegacoes",
                columns: new[] { "SubstitutoId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Delegacoes_TitularId_Ativo",
                table: "Delegacoes",
                columns: new[] { "TitularId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensOrdemCompra_CotacaoItemId",
                table: "ItensOrdemCompra",
                column: "CotacaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensOrdemCompra_OrdemCompraId",
                table: "ItensOrdemCompra",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensOrdemCompra_ProdutoId",
                table: "ItensOrdemCompra",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensRecebimento_ItemOrdemCompraId_ComAvaria",
                table: "ItensRecebimento",
                columns: new[] { "ItemOrdemCompraId", "ComAvaria" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensRecebimento_RecebimentoId",
                table: "ItensRecebimento",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_CentroCustoId_Ano_Mes",
                table: "Orcamentos",
                columns: new[] { "CentroCustoId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdensCompra_CotacaoId",
                table: "OrdensCompra",
                column: "CotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensCompra_CriadoPorId",
                table: "OrdensCompra",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensCompra_FornecedorId",
                table: "OrdensCompra",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensCompra_Numero",
                table: "OrdensCompra",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_Numero",
                table: "Recebimentos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_OrdemCompraId",
                table: "Recebimentos",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_RecebidoPorId",
                table: "Recebimentos",
                column: "RecebidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_TitulosPagar_FornecedorId",
                table: "TitulosPagar",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_TitulosPagar_NotaFiscalId",
                table: "TitulosPagar",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_TitulosPagar_Numero_Parcela",
                table: "TitulosPagar",
                columns: new[] { "Numero", "Parcela" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TitulosPagar_OrdemCompraId",
                table: "TitulosPagar",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_TitulosPagar_Status_Vencimento",
                table: "TitulosPagar",
                columns: new[] { "Status", "Vencimento" });

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFiscais_OrdensCompra_OrdemCompraId",
                table: "NotasFiscais",
                column: "OrdemCompraId",
                principalTable: "OrdensCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotasFiscais_OrdensCompra_OrdemCompraId",
                table: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "Alcadas");

            migrationBuilder.DropTable(
                name: "AprovacoesItem");

            migrationBuilder.DropTable(
                name: "BaixasTitulo");

            migrationBuilder.DropTable(
                name: "Delegacoes");

            migrationBuilder.DropTable(
                name: "ItensRecebimento");

            migrationBuilder.DropTable(
                name: "Orcamentos");

            migrationBuilder.DropTable(
                name: "TitulosPagar");

            migrationBuilder.DropTable(
                name: "ItensOrdemCompra");

            migrationBuilder.DropTable(
                name: "Recebimentos");

            migrationBuilder.DropTable(
                name: "OrdensCompra");

            migrationBuilder.DropIndex(
                name: "IX_NotasFiscais_OrdemCompraId",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "PrecoReferencia",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PrecoReferenciaEm",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "LiberadaEm",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "LiberadaParaPagamento",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "LiberadaPorId",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "MotivoLiberacao",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "OrdemCompraId",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "NivelAtual",
                table: "ItensSolicitacao");

            migrationBuilder.DropColumn(
                name: "ValorEstimado",
                table: "ItensSolicitacao");

            migrationBuilder.DropColumn(
                name: "BloqueiaAcimaOrcamento",
                table: "CentrosCusto");
        }
    }
}
