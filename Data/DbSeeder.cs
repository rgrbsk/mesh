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

            // Dados de conveniência vêm DEPOIS do acesso, de propósito: se um
            // seed de dado falhar, o Program.cs engole a exceção e o resto do
            // método não roda. Com esta ordem, o que se perde é a etapa de
            // exemplo — nunca as permissões, que trancariam todo mundo fora.
            await GarantirSuperAdmin(userManager, roleManager);

            await SemearEtapas(db);

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

        public const string SuperAdminEmail = "superadmin@demo.com";

        /// <summary>
        /// O dono da aplicação: papel SuperAdmin com o catálogo inteiro mais a
        /// permissão da plataforma, que não está no catálogo. Idempotente — cria
        /// o que falta e não mexe no que já existe (senha trocada continua
        /// trocada).
        /// </summary>
        private static async Task GarantirSuperAdmin(UserManager<Usuario> userManager, RoleManager<Papel> roleManager)
        {
            var papel = await roleManager.FindByNameAsync(Permissoes.PapelSuperAdmin);
            if (papel is null)
            {
                papel = new Papel(Permissoes.PapelSuperAdmin) { Descricao = "Dono da aplicação" };
                await roleManager.CreateAsync(papel);
            }

            // O dono opera a plataforma, não a empresa: não é um usuário comum
            // e não enxerga os módulos do ERP. A única permissão é a dele.
            var atuais = (await roleManager.GetClaimsAsync(papel))
                .Where(c => c.Type == Permissoes.ClaimType)
                .ToList();

            foreach (var sobra in atuais.Where(c => c.Value != Permissoes.Plataforma))
                await roleManager.RemoveClaimAsync(papel, sobra);

            if (atuais.All(c => c.Value != Permissoes.Plataforma))
                await roleManager.AddClaimAsync(papel, new Claim(Permissoes.ClaimType, Permissoes.Plataforma));

            var usuario = await userManager.FindByEmailAsync(SuperAdminEmail);
            if (usuario is null)
            {
                usuario = new Usuario
                {
                    UserName = SuperAdminEmail,
                    Email = SuperAdminEmail,
                    EmailConfirmed = true,
                    Nome = "Super",
                    Sobrenome = "Admin",
                    Cargo = "Dono da aplicação",
                    Status = Erp.Model.Usuario.StatusUsuario.Ativo,
                };

                var resultado = await userManager.CreateAsync(usuario, DemoSenha);
                if (!resultado.Succeeded)
                    throw new InvalidOperationException(
                        "Falha ao criar o superadmin: " +
                        string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(usuario, Permissoes.PapelSuperAdmin))
                await userManager.AddToRoleAsync(usuario, Permissoes.PapelSuperAdmin);

            // Acima dos tenants: não pertence a nenhuma empresa.
            if (usuario.EmpresaId is not null)
            {
                usuario.EmpresaId = null;
                await userManager.UpdateAsync(usuario);
            }
        }

        /// <summary>Duas etapas para o Kanban não abrir sem nenhuma coluna antes
        /// da aprovação. São sugestão: quem tem etapas.gerir renomeia, reordena
        /// ou apaga em Adicionais.</summary>
        internal static async Task SemearEtapas(AppDbContext db)
        {
            if (await db.Etapas.AnyAsync())
                return;

            db.Etapas.AddRange(
                new Erp.Model.Etapa.Etapa
                {
                    Nome = "Elaboração", Ordem = 1, Cor = "#89CFF0",
                    Descricao = "Montando a lista de itens.",
                },
                new Erp.Model.Etapa.Etapa
                {
                    Nome = "Conferência", Ordem = 2, Cor = "#E4A0F7",
                    Descricao = "Revisão antes de mandar para aprovação.",
                });

            await db.SaveChangesAsync();
        }
    }
}
