using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class AjustesPessoaCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pessoas_Cidades_CidadeId",
                table: "Pessoas");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroEndereco",
                table: "Pessoas",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "IE",
                table: "Pessoas",
                type: "text",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CidadeId",
                table: "Pessoas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CEP",
                table: "Pessoas",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Pessoas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InscricaoMunicipal",
                table: "Pessoas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificadoEm",
                table: "Pessoas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Natureza",
                table: "Pessoas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegimeTributario",
                table: "Pessoas",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pessoas_Cidades_CidadeId",
                table: "Pessoas",
                column: "CidadeId",
                principalTable: "Cidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pessoas_Cidades_CidadeId",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "InscricaoMunicipal",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "ModificadoEm",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "Natureza",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "RegimeTributario",
                table: "Pessoas");

            migrationBuilder.AlterColumn<int>(
                name: "NumeroEndereco",
                table: "Pessoas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "IE",
                table: "Pessoas",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CidadeId",
                table: "Pessoas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CEP",
                table: "Pessoas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pessoas_Cidades_CidadeId",
                table: "Pessoas",
                column: "CidadeId",
                principalTable: "Cidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
