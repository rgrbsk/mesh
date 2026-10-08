using System.Text.RegularExpressions;
using Erp.Data;
using Erp.Data.Tenancy;
using Erp.Model.Acesso;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Empresa = Erp.Model.Empresa.Empresa;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Service.Plataforma
{
    /// <summary>
    /// Rotina de criação de tenant: cria o banco, aplica as migrations, carrega
    /// os dados de referência, registra a empresa no catálogo e cria os
    /// usuários. Tudo ou nada — se um passo falha, o que já foi feito é desfeito
    /// (inclusive o banco), para não sobrar tenant pela metade.
    /// </summary>
    public sealed partial class ProvisionamentoTenant
    {
        public sealed record NovoUsuario(string Nome, string Sobrenome, string Email, string Papel);

        public sealed record Pedido(string Empresa, string Cnpj, string Banco, string SenhaInicial, List<NovoUsuario> Usuarios);

        public sealed record Resultado(Guid EmpresaId, string Banco, int Usuarios);

        private readonly ConexoesTenant _conexoes;
        private readonly UserManager<Usuario> _usuarios;
        private readonly RoleManager<Papel> _papeis;
        private readonly Erp.Repository.Log.LogRepository _logs;

        public ProvisionamentoTenant(
            ConexoesTenant conexoes, UserManager<Usuario> usuarios, RoleManager<Papel> papeis,
            Erp.Repository.Log.LogRepository logs)
        {
            _conexoes = conexoes;
            _usuarios = usuarios;
            _papeis = papeis;
            _logs = logs;
        }

        public const string Prefixo = "mesh_";

        [GeneratedRegex("^mesh_[a-z][a-z0-9_]{1,40}$")]
        private static partial Regex NomeValido();

        /// <summary>"Metalúrgica São José" → "mesh_metalurgica_sao_jose".</summary>
        public static string SugerirBanco(string empresa)
        {
            var semAcento = new string(empresa.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray()).ToLowerInvariant();

            var slug = Regex.Replace(semAcento, "[^a-z0-9]+", "_").Trim('_');
            if (slug.Length > 0 && char.IsDigit(slug[0]))
                slug = "t" + slug;

            return Prefixo + (slug.Length > 40 ? slug[..40].TrimEnd('_') : slug);
        }

        public async Task<List<string>> PapeisDisponiveis() =>
            await _papeis.Roles
                .Where(r => r.Name != null && r.Name != Permissoes.PapelSuperAdmin)
                .Select(r => r.Name!)
                .OrderBy(n => n)
                .ToListAsync();

        public async Task<Resultado> Provisionar(Pedido pedido, Guid porQuem, Func<string, Task> passo)
        {
            await Validar(pedido);

            var banco = pedido.Banco;
            var bancoCriado = false;
            Guid? empresaId = null;
            var criados = new List<Usuario>();

            try
            {
                await passo($"Criando o banco {banco}");
                await using (var servidor = new NpgsqlConnection(_conexoes.ConnectionStringServidor()))
                {
                    await servidor.OpenAsync();
                    await using var criar = servidor.CreateCommand();
                    criar.CommandText = $"CREATE DATABASE \"{banco}\" ENCODING 'UTF8' TEMPLATE template0";
                    await criar.ExecuteNonQueryAsync();
                }
                bancoCriado = true;

                await passo("Criando as tabelas");
                await using var tenant = _conexoes.Criar(banco);
                await CriarEsquema(tenant);

                await passo("Carregando os municípios do IBGE");
                await CidadeSeeder.SeedAsync(tenant);

                await passo("Criando as etapas padrão do Kanban");
                await DbSeeder.SemearEtapas(tenant);

                await passo("Registrando a empresa no catálogo");
                var empresa = new Empresa
                {
                    Nome = pedido.Empresa.Trim(),
                    CNPJ = pedido.Cnpj.Trim(),
                    Banco = banco,
                    Ativa = true,
                };

                await using (var central = _conexoes.Central())
                {
                    central.Empresas.Add(empresa);
                    await central.SaveChangesAsync();
                }
                empresaId = empresa.Id;

                // Cópia no banco do tenant: os usuários espelhados apontam para ela.
                tenant.Empresas.Add(new Empresa
                {
                    Id = empresa.Id, Nome = empresa.Nome, CNPJ = empresa.CNPJ,
                    Banco = banco, Ativa = true, CriadoEm = empresa.CriadoEm,
                });
                await tenant.SaveChangesAsync();

                foreach (var novo in pedido.Usuarios)
                {
                    await passo($"Criando o usuário {novo.Email}");

                    var usuario = new Usuario
                    {
                        UserName = novo.Email.Trim(),
                        Email = novo.Email.Trim(),
                        EmailConfirmed = true,
                        Nome = novo.Nome.Trim(),
                        Sobrenome = novo.Sobrenome.Trim(),
                        Cargo = novo.Papel,
                        EmpresaId = empresa.Id,
                        Status = Erp.Model.Usuario.StatusUsuario.Ativo,
                        DataCadastro = DateTime.UtcNow,
                        DataModificacao = DateTime.UtcNow,
                    };

                    var criado = await _usuarios.CreateAsync(usuario, pedido.SenhaInicial);
                    if (!criado.Succeeded)
                        throw new InvalidOperationException(
                            $"{novo.Email}: {string.Join(" ", criado.Errors.Select(e => e.Description))}");

                    criados.Add(usuario);
                    await _usuarios.AddToRoleAsync(usuario, novo.Papel);
                    await EspelhoUsuarios.Gravar(tenant, usuario);
                }

                await passo("Pronto");

                await _logs.Registrar(
                    Erp.Model.Log.TipoAcao.Criacao, "Plataforma",
                    $"Tenant {empresa.Nome} criado no banco {banco} com {criados.Count} "
                    + (criados.Count == 1 ? "usuário." : "usuários."),
                    porQuem,
                    entidade: "Empresa",
                    entidadeId: empresa.Id.ToString());

                return new Resultado(empresa.Id, banco, criados.Count);
            }
            catch
            {
                await passo("Falhou — desfazendo o que foi criado");
                await Desfazer(banco, bancoCriado, empresaId, criados);
                throw;
            }
        }

        /// <summary>
        /// Cria as tabelas a partir do modelo ATUAL e marca todas as migrations
        /// como aplicadas. Repetir o histórico do zero não é confiável — ele
        /// carrega ajustes feitos à mão ao longo do projeto (coluna criada duas
        /// vezes, por exemplo). Com a linha de base marcada, as migrations NOVAS
        /// passam a ser aplicadas neste banco normalmente na subida do sistema.
        /// </summary>
        private static async Task CriarEsquema(AppDbContext tenant)
        {
            await tenant.Database.EnsureCreatedAsync();

            var historico = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IHistoryRepository>(tenant);
            await tenant.Database.ExecuteSqlRawAsync(historico.GetCreateIfNotExistsScript());

            var versao = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0.0";
            foreach (var migration in tenant.Database.GetMigrations())
                await tenant.Database.ExecuteSqlRawAsync(
                    historico.GetInsertScript(new Microsoft.EntityFrameworkCore.Migrations.HistoryRow(migration, versao)));
        }

        private async Task Validar(Pedido pedido)
        {
            if (string.IsNullOrWhiteSpace(pedido.Empresa))
                throw new InvalidOperationException("Informe o nome da empresa.");

            if (!NomeValido().IsMatch(pedido.Banco))
                throw new InvalidOperationException(
                    "Nome da base inválido: use mesh_ seguido de letras minúsculas, números ou _.");

            if (pedido.Usuarios.Count == 0)
                throw new InvalidOperationException("Informe ao menos o administrador do tenant.");

            var emails = pedido.Usuarios.Select(u => u.Email.Trim().ToLowerInvariant()).ToList();
            if (emails.Any(string.IsNullOrWhiteSpace) || emails.Distinct().Count() != emails.Count)
                throw new InvalidOperationException("Cada usuário precisa de um e-mail, sem repetir.");

            foreach (var email in emails)
                if (await _usuarios.FindByEmailAsync(email) is not null)
                    throw new InvalidOperationException($"{email} já é usuário de outro tenant.");

            var papeis = await PapeisDisponiveis();
            if (pedido.Usuarios.Any(u => !papeis.Contains(u.Papel)))
                throw new InvalidOperationException("Papel inválido em algum usuário.");

            foreach (var validador in _usuarios.PasswordValidators)
            {
                var resultado = await validador.ValidateAsync(_usuarios, new Usuario(), pedido.SenhaInicial);
                if (!resultado.Succeeded)
                    throw new InvalidOperationException(
                        "Senha inicial: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));
            }

            await using var central = _conexoes.Central();
            if (await central.Empresas.AnyAsync(e => e.Banco == pedido.Banco) || _conexoes.EhCentral(pedido.Banco))
                throw new InvalidOperationException($"A base {pedido.Banco} já pertence a outro tenant.");

            await using var servidor = new NpgsqlConnection(_conexoes.ConnectionStringServidor());
            await servidor.OpenAsync();
            await using var existe = servidor.CreateCommand();
            existe.CommandText = "SELECT 1 FROM pg_database WHERE datname = @nome";
            existe.Parameters.AddWithValue("nome", pedido.Banco);
            if (await existe.ExecuteScalarAsync() is not null)
                throw new InvalidOperationException($"Já existe um banco chamado {pedido.Banco} no servidor.");
        }

        private async Task Desfazer(string banco, bool bancoCriado, Guid? empresaId, List<Usuario> criados)
        {
            foreach (var usuario in criados)
            {
                try { await _usuarios.DeleteAsync(usuario); } catch { }
            }

            if (empresaId is { } id)
            {
                try
                {
                    await using var central = _conexoes.Central();
                    await central.Empresas.Where(e => e.Id == id).ExecuteDeleteAsync();
                }
                catch { }
            }

            if (bancoCriado)
            {
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    await using var servidor = new NpgsqlConnection(_conexoes.ConnectionStringServidor());
                    await servidor.OpenAsync();
                    await using var apagar = servidor.CreateCommand();
                    apagar.CommandText = $"DROP DATABASE IF EXISTS \"{banco}\" WITH (FORCE)";
                    await apagar.ExecuteNonQueryAsync();
                }
                catch { }
            }
        }
    }
}
