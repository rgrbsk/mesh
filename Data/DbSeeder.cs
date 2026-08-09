using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Empresa = Erp.Model.Empresa.Empresa;
using Usuario = Erp.Model.Usuario.Usuario;
using UsuarioEmpresa = Erp.Model.Usuario.UsuarioEmpresa;

namespace Erp.Data
{
    /// <summary>
    /// Seed de desenvolvimento: aplica as migrations e cria um usuário demo
    /// (com senha em hash) vinculado a uma empresa. Idempotente — só semeia se vazio.
    /// </summary>
    public static class DbSeeder
    {
        public const string DemoEmail = "admin@demo.com";
        public const string DemoSenha = "Senha@123";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

            // Cria o banco (se não existir) e aplica as migrations.
            await db.Database.MigrateAsync();

            if (await db.Usuarios.AnyAsync())
                return; // já semeado

            var empresa = new Empresa
            {
                Nome = "Empresa Demo",
                CNPJ = "00.000.000/0001-00",
            };
            db.Empresas.Add(empresa);

            var usuario = new Usuario
            {
                Nome = "Administrador",
                Email = DemoEmail,
            };
            usuario.SenhaHash = hasher.HashPassword(usuario, DemoSenha);
            db.Usuarios.Add(usuario);

            db.Vinculos.Add(new UsuarioEmpresa
            {
                UsuarioId = usuario.Id,
                EmpresaId = empresa.Id,
            });

            await db.SaveChangesAsync();
        }
    }
}
