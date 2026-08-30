using Erp.Model.Acesso;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppUsuario = Erp.Model.Usuario.Usuario;

namespace Erp.Repository.PermissoesUsuario
{
    /// <summary>
    /// Permissão é claim do tipo "perm". Ela chega ao usuário por dois caminhos:
    /// DIRETA (claim no próprio usuário, concedida na tela) ou HERDADA (claim do
    /// papel a que ele pertence). O principal soma os dois — quem herda não
    /// precisa da direta, e revogar a direta não tira o que vem do papel.
    ///
    /// Vai pelo UserManager/RoleManager, não pelo DbContext cru: é o Identity que
    /// cuida do security stamp, e sem ele a sessão continuaria com as permissões
    /// antigas até o cookie expirar.
    /// </summary>
    public class PermissoesRepository
    {
        private readonly UserManager<AppUsuario> _usuarios;
        private readonly RoleManager<Papel> _papeis;
        private readonly IDbContextFactory<Erp.Data.AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;

        public PermissoesRepository(
            UserManager<AppUsuario> usuarios,
            RoleManager<Papel> papeis,
            IDbContextFactory<Erp.Data.AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs)
        {
            _usuarios = usuarios;
            _papeis = papeis;
            _fabrica = fabrica;
            _logs = logs;
        }

        /// <summary>Papel e nº de permissões diretas de todo mundo, em duas
        /// consultas — a grade precisa disso por linha, e uma ida ao Identity
        /// por usuário custaria uma consulta por linha da página.</summary>
        public async Task<Dictionary<Guid, (string? Papel, int Diretas)>> Resumo()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var papeis = await (from vinculo in contexto.UserRoles
                                join papel in contexto.Roles on vinculo.RoleId equals papel.Id
                                select new { vinculo.UserId, papel.Name })
                               .ToListAsync();

            var diretas = await contexto.UserClaims
                .Where(c => c.ClaimType == Permissoes.ClaimType)
                .GroupBy(c => c.UserId)
                .Select(g => new { UsuarioId = g.Key, Total = g.Count() })
                .ToListAsync();

            var porUsuario = diretas.ToDictionary(d => d.UsuarioId, d => d.Total);

            return papeis
                .Select(p => p.UserId)
                .Union(porUsuario.Keys)
                .Distinct()
                .ToDictionary(
                    id => id,
                    id => (papeis.FirstOrDefault(p => p.UserId == id)?.Name,
                           porUsuario.TryGetValue(id, out var total) ? total : 0));
        }

        /// <summary>Permissões concedidas direto ao usuário.</summary>
        public async Task<HashSet<string>> Diretas(Guid usuarioId)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
            if (usuario is null)
                return [];

            return (await _usuarios.GetClaimsAsync(usuario))
                .Where(c => c.Type == Permissoes.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();
        }

        /// <summary>
        /// O que cada papel concede. A tela carrega isso uma vez e recalcula o
        /// herdado sozinha quando o usuário troca o papel no combo — sem ida ao
        /// banco a cada mudança, e sem depender de o papel já estar salvo.
        /// </summary>
        public async Task<Dictionary<string, HashSet<string>>> PermissoesPorPapel()
        {
            var mapa = new Dictionary<string, HashSet<string>>();

            foreach (var papel in _papeis.Roles.ToList())
            {
                if (papel.Name is null)
                    continue;

                mapa[papel.Name] = (await _papeis.GetClaimsAsync(papel))
                    .Where(c => c.Type == Permissoes.ClaimType)
                    .Select(c => c.Value)
                    .ToHashSet();
            }

            return mapa;
        }

        /// <summary>
        /// Deixa as permissões diretas exatamente iguais à lista recebida:
        /// concede o que falta e revoga o que sobra. Mexe só no que mudou, pra
        /// não reescrever a tabela de claims a cada salvamento.
        /// </summary>
        public async Task Definir(Guid usuarioId, IEnumerable<string> permissoes)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
            if (usuario is null)
                return;

            // Só chaves do catálogo: a tela não pode inventar permissão que
            // nenhuma policy conhece.
            var desejadas = permissoes.Where(Permissoes.Todas.Contains).ToHashSet();

            var atuais = (await _usuarios.GetClaimsAsync(usuario))
                .Where(c => c.Type == Permissoes.ClaimType)
                .ToList();

            foreach (var sobrando in atuais.Where(c => !desejadas.Contains(c.Value)))
                await _usuarios.RemoveClaimAsync(usuario, sobrando);

            var jaTem = atuais.Select(c => c.Value).ToHashSet();

            foreach (var faltando in desejadas.Except(jaTem))
                await _usuarios.AddClaimAsync(usuario, new Claim(Permissoes.ClaimType, faltando));

            // Derruba as sessões abertas desse usuário na próxima revalidação
            // (5 min), senão ele continuaria com o acesso antigo.
            await _usuarios.UpdateSecurityStampAsync(usuario);

            // Concessão de acesso é o tipo de mudança que se audita: registra o
            // conjunto final, que é o que vale, e não o delta.
            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, "Usuários",
                $"Permissões diretas de {usuario.Nome} {usuario.Sobrenome}".TrimEnd()
                + $" definidas como: {(desejadas.Count == 0 ? "nenhuma" : string.Join(", ", desejadas.Order()))}.",
                entidade: "Usuario",
                entidadeId: usuarioId.ToString());
        }

        /// <summary>Papéis existentes, para o seletor da tela.</summary>
        public Task<List<Papel>> Papeis() =>
            Task.FromResult(_papeis.Roles.OrderBy(p => p.Name).ToList());

        public async Task<IList<string>> PapeisDoUsuario(Guid usuarioId)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());

            return usuario is null ? [] : await _usuarios.GetRolesAsync(usuario);
        }

        /// <summary>Um papel por usuário — nome vazio tira o que houver.</summary>
        public async Task DefinirPapel(Guid usuarioId, string? nomePapel)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
            if (usuario is null)
                return;

            var atuais = await _usuarios.GetRolesAsync(usuario);

            if (atuais.Count == 1 && atuais[0] == nomePapel)
                return;

            if (atuais.Count > 0)
                await _usuarios.RemoveFromRolesAsync(usuario, atuais);

            if (!string.IsNullOrWhiteSpace(nomePapel))
                await _usuarios.AddToRoleAsync(usuario, nomePapel);

            await _usuarios.UpdateSecurityStampAsync(usuario);
        }
    }
}
