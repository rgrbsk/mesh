using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class ProdutosECentrosCusto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As alterações da tabela Pessoas foram removidas à mão: elas já
            // estão no banco pela migration AjustesPessoaCrud, e o scaffolding
            // as repetiu porque o snapshot compilado estava desatualizado.
            migrationBuilder.CreateTable(
                name: "CentrosCusto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosCusto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentrosCusto_AspNetUsers_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Unidade = table.Column<string>(type: "text", nullable: false),
                    SaldoAtual = table.Column<decimal>(type: "numeric", nullable: false),
                    EstoqueMinimo = table.Column<decimal>(type: "numeric", nullable: false),
                    PontoPedido = table.Column<decimal>(type: "numeric", nullable: false),
                    PrazoEntregaDias = table.Column<int>(type: "integer", nullable: false),
                    ConsumoMedioDiario = table.Column<decimal>(type: "numeric", nullable: false),
                    FornecedorPadraoId = table.Column<int>(type: "integer", nullable: true),
                    CentroCustoPadraoId = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_CentrosCusto_CentroCustoPadraoId",
                        column: x => x.CentroCustoPadraoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Produtos_Pessoas_FornecedorPadraoId",
                        column: x => x.FornecedorPadraoId,
                        principalTable: "Pessoas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_Codigo",
                table: "CentrosCusto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_ResponsavelId",
                table: "CentrosCusto",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CentroCustoPadraoId",
                table: "Produtos",
                column: "CentroCustoPadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_FornecedorPadraoId",
                table: "Produtos",
                column: "FornecedorPadraoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "CentrosCusto");
        }
    }
}
