using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class TenantsEGeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "LogsSistema",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "LogsSistema",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "LogsSistema",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "LogsSistema",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Ativa",
                table: "Empresas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensaEm",
                table: "Empresas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmpresaId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Empresas_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Quem já existe pertence à empresa que já existia; o dono da
            // aplicação fica fora de qualquer tenant.
            migrationBuilder.Sql(
                @"UPDATE ""AspNetUsers"" u
                  SET ""EmpresaId"" = (SELECT ""Id"" FROM ""Empresas"" ORDER BY ""CriadoEm"" LIMIT 1)
                  WHERE ""EmpresaId"" IS NULL
                    AND NOT EXISTS (SELECT 1 FROM ""AspNetUserRoles"" ur
                                    JOIN ""AspNetRoles"" r ON r.""Id"" = ur.""RoleId""
                                    WHERE ur.""UserId"" = u.""Id"" AND r.""Name"" = 'SuperAdmin');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Empresas_EmpresaId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "LogsSistema");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "LogsSistema");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "LogsSistema");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "LogsSistema");

            migrationBuilder.DropColumn(
                name: "Ativa",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "SuspensaEm",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "AspNetUsers");
        }
    }
}
