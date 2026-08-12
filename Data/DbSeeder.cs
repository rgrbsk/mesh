using System.Security.Claims;
using Erp.Model.Acesso;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Empresa = Erp.Model.Empresa.Empresa;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Data
{
    /// <summary>
    /// Seed de desenvolvimento: aplica as migrations, cria o papel Administrador
    /// com todas as permissões e um usuário demo. Idempotente.
    /// </summary>
    public static class DbSeeder
    {
        public const string DemoEmail = "admin@demo.com";
        public const string DemoSenha = "Senha@123";
        public const string PapelAdmin = "Administrador";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<AppDbContext>();
            var userManager = sp.GetRequiredService<UserManager<Usuario>>();
            var roleManager = sp.GetRequiredService<RoleManager<Papel>>();

            // Cria o banco (se não existir) e aplica as migrations.
            await db.Database.MigrateAsync();

            if (!await db.Empresas.AnyAsync())
            {
                db.Empresas.Add(new Empresa
                {
                    Nome = "Empresa Demo",
                    CNPJ = "00.000.000/0001-00",
                });
                await db.SaveChangesAsync();
            }

            // Papel Administrador com o catálogo inteiro de permissões. Rodar de novo
            // depois de acrescentar constantes em Permissoes adiciona só as que faltam.
            var admin = await roleManager.FindByNameAsync(PapelAdmin);
            if (admin is null)
            {
                admin = new Papel(PapelAdmin) { Descricao = "Acesso total ao sistema" };
                await roleManager.CreateAsync(admin);
            }

            var atuais = (await roleManager.GetClaimsAsync(admin))
                .Where(c => c.Type == Permissoes.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permissao in Permissoes.Todas.Except(atuais))
                await roleManager.AddClaimAsync(admin, new Claim(Permissoes.ClaimType, permissao));

            if (await userManager.FindByEmailAsync(DemoEmail) is not null)
                return;

            var usuario = new Usuario
            {
                UserName = DemoEmail,
                Email = DemoEmail,
                EmailConfirmed = true,
                Nome = "Administrador",
            };

            var resultado = await userManager.CreateAsync(usuario, DemoSenha);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    "Falha ao criar o usuário demo: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(usuario, PapelAdmin);
        }
    }
}
