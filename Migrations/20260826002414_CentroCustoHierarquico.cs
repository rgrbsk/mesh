using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class CentroCustoHierarquico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaiId",
                table: "CentrosCusto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_PaiId",
                table: "CentrosCusto",
                column: "PaiId");

            migrationBuilder.AddForeignKey(
                name: "FK_CentrosCusto_CentrosCusto_PaiId",
                table: "CentrosCusto",
                column: "PaiId",
                principalTable: "CentrosCusto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CentrosCusto_CentrosCusto_PaiId",
                table: "CentrosCusto");

            migrationBuilder.DropIndex(
                name: "IX_CentrosCusto_PaiId",
                table: "CentrosCusto");

            migrationBuilder.DropColumn(
                name: "PaiId",
                table: "CentrosCusto");
        }
    }
}
