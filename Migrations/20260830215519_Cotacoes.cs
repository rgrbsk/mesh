using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class Cotacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cotacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PrazoResposta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotacoes_AspNetUsers_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Convites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CotacaoId = table.Column<int>(type: "integer", nullable: false),
                    PessoaId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cnpj = table.Column<string>(type: "text", nullable: false),
                    RazaoSocial = table.Column<string>(type: "text", nullable: false),
                    Responsavel = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "text", nullable: false),
                    CondicaoPagamento = table.Column<string>(type: "text", nullable: false),
                    Frete = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Convites_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Convites_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CotacaoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CotacaoId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemSolicitacaoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotacaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotacaoItens_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CotacaoItens_ItensSolicitacao_ItemSolicitacaoId",
                        column: x => x.ItemSolicitacaoId,
                        principalTable: "ItensSolicitacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CotacaoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Propostas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConviteFornecedorId = table.Column<int>(type: "integer", nullable: false),
                    CotacaoItemId = table.Column<int>(type: "integer", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric", nullable: true),
                    PrazoEntregaDias = table.Column<int>(type: "integer", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propostas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Propostas_Convites_ConviteFornecedorId",
                        column: x => x.ConviteFornecedorId,
                        principalTable: "Convites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Propostas_CotacaoItens_CotacaoItemId",
                        column: x => x.CotacaoItemId,
                        principalTable: "CotacaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Convites_CotacaoId",
                table: "Convites",
                column: "CotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Convites_PessoaId",
                table: "Convites",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_Convites_Token",
                table: "Convites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_CotacaoId",
                table: "CotacaoItens",
                column: "CotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_ItemSolicitacaoId",
                table: "CotacaoItens",
                column: "ItemSolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_ProdutoId",
                table: "CotacaoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_CriadoPorId",
                table: "Cotacoes",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Propostas_ConviteFornecedorId",
                table: "Propostas",
                column: "ConviteFornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Propostas_CotacaoItemId",
                table: "Propostas",
                column: "CotacaoItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Propostas");

            migrationBuilder.DropTable(
                name: "Convites");

            migrationBuilder.DropTable(
                name: "CotacaoItens");

            migrationBuilder.DropTable(
                name: "Cotacoes");
        }
    }
}
