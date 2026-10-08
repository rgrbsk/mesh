using Erp.Data;
using Erp.Model.Log;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Plataforma
{
    /// <summary>
    /// Consultas do painel do dono da aplicação: saúde do banco, eventos de
    /// sistema e uso por usuário. Tudo leitura — nada aqui altera dado.
    /// </summary>
    public class PlataformaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly Erp.Data.Tenancy.ConexoesTenant _conexoes;

        /// <summary>Bancos de todos os tenants (o central entra uma vez só).</summary>
        private async Task<List<string>> Bancos()
        {
            await using var central = _conexoes.Central();
            var bancos = await central.Empresas.Select(e => e.Banco).ToListAsync();
            return bancos.Append("").Select(_conexoes.Normalizar).Distinct().ToList();
        }

        /// <summary>Roda a mesma consulta no banco de cada tenant e junta. Um banco
        /// fora do ar não derruba o console — só fica de fora da soma.</summary>
        private async Task<List<T>> EmCadaBanco<T>(Func<AppDbContext, Task<List<T>>> consulta)
        {
            var tudo = new List<T>();
            foreach (var banco in await Bancos())
            {
                try
                {
                    await using var contexto = _conexoes.Criar(banco);
                    tudo.AddRange(await consulta(contexto));
                }
                catch (Exception)
                {
                }
            }
            return tudo;
        }
        private readonly Microsoft.AspNetCore.Identity.UserManager<Erp.Model.Usuario.Usuario> _usuarios;

        public PlataformaRepository(
            Erp.Data.Tenancy.FabricaCentral fabrica,
            Erp.Data.Tenancy.ConexoesTenant conexoes,
            Erp.Repository.Log.LogRepository logs,
            Microsoft.AspNetCore.Identity.UserManager<Erp.Model.Usuario.Usuario> usuarios)
        {
            _fabrica = fabrica;
            _conexoes = conexoes;
            _logs = logs;
            _usuarios = usuarios;
        }

        /// <summary>
        /// Restringe o acesso de um usuário pelo bloqueio do Identity: o login é
        /// recusado até a data (ou até alguém liberar) e a sessão aberta cai na
        /// próxima revalidação. Fica fora da tela de usuários do tenant — quem
        /// administra a empresa não desfaz uma restrição da plataforma.
        /// </summary>
        public async Task RestringirAcesso(Guid usuarioId, DateTimeOffset? ate, string? motivo, Guid porQuem)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString())
                ?? throw new InvalidOperationException("Usuário não encontrado.");

            if (await _usuarios.IsInRoleAsync(usuario, Erp.Model.Acesso.Permissoes.PapelSuperAdmin))
                throw new InvalidOperationException("O dono da aplicação não pode ser restringido.");

            await _usuarios.SetLockoutEnabledAsync(usuario, true);
            await _usuarios.SetLockoutEndDateAsync(usuario, ate ?? DateTimeOffset.MaxValue);
            await _usuarios.UpdateSecurityStampAsync(usuario);

            var nome = $"{usuario.Nome} {usuario.Sobrenome}".Trim();
            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, "Plataforma",
                $"Acesso de {nome} restrito {(ate is { } d ? $"até {d.ToLocalTime():dd/MM/yyyy HH:mm}" : "até ser liberado")}"
                + (string.IsNullOrWhiteSpace(motivo) ? "." : $". Motivo: {motivo.Trim()}"),
                porQuem,
                entidade: "Usuario",
                entidadeId: usuario.Id.ToString());
        }

        public async Task LiberarAcesso(Guid usuarioId, Guid porQuem)
        {
            var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString())
                ?? throw new InvalidOperationException("Usuário não encontrado.");

            await _usuarios.SetLockoutEndDateAsync(usuario, null);
            await _usuarios.ResetAccessFailedCountAsync(usuario);

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, "Plataforma",
                $"Acesso de {$"{usuario.Nome} {usuario.Sobrenome}".Trim()} liberado.",
                porQuem,
                entidade: "Usuario",
                entidadeId: usuario.Id.ToString());
        }

        // ---------------- Tenants ----------------

        public sealed class UsuarioDoTenant
        {
            public Guid Id { get; set; }
            public string Nome { get; set; } = "";
            public string Email { get; set; } = "";
            public bool Ativo { get; set; }
            /// <summary>Até quando o acesso está restrito pela plataforma.
            /// DateTimeOffset.MaxValue = até alguém liberar.</summary>
            public DateTimeOffset? RestritoAte { get; set; }
            public bool Restrito => RestritoAte is { } ate && ate > DateTimeOffset.UtcNow;
        }

        public sealed class Tenant
        {
            public Guid Id { get; set; }
            public string Nome { get; set; } = "";
            public string Cnpj { get; set; } = "";
            public string Banco { get; set; } = "";
            public bool Ativa { get; set; }
            public DateTime CriadoEm { get; set; }
            public DateTime? SuspensaEm { get; set; }
            public List<UsuarioDoTenant> Usuarios { get; set; } = new();
            public DateTime? UltimoLogin { get; set; }
            public string? UltimoLoginDe { get; set; }
            public int Acoes7Dias { get; set; }
            public DateTime? UltimaAcao { get; set; }
            public string? UltimaAcaoDescricao { get; set; }
            public string? UltimaAcaoDe { get; set; }
        }

        public sealed record AtividadeRecente(DateTime Quando, string Usuario, string Tenant, string Modulo, string Descricao);

        public sealed class VisaoTenants
        {
            public List<Tenant> Tenants { get; set; } = new();
            public List<AtividadeRecente> Atividade { get; set; } = new();
        }

        public async Task<VisaoTenants> Tenants()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var empresas = await contexto.Empresas.AsNoTracking().OrderBy(e => e.CriadoEm).ToListAsync();

            var usuarios = await contexto.Usuarios.AsNoTracking()
                .Where(u => u.EmpresaId != null)
                .Select(u => new { u.Id, u.EmpresaId, u.Nome, u.Sobrenome, u.Email, u.Status, u.LockoutEnd })
                .ToListAsync();

            var tenantDe = usuarios.ToDictionary(u => u.Id, u => u.EmpresaId!.Value);
            var nomeDe = usuarios.ToDictionary(u => u.Id, u => $"{u.Nome} {u.Sobrenome}".Trim());

            var ultimosLogins = await contexto.LogsSistema
                .Where(l => l.UsuarioId != null && l.Evento == TipoEventoSistema.Login)
                .GroupBy(l => l.UsuarioId)
                .Select(g => new { Id = g.Key!.Value, Quando = g.Max(x => x.Quando) })
                .ToListAsync();

            var semana = DateTime.UtcNow.AddDays(-7);
            var acoesSemana = await EmCadaBanco(c => c.Logs
                .Where(l => l.UsuarioId != null && l.Quando >= semana)
                .GroupBy(l => l.UsuarioId)
                .Select(g => new { Id = g.Key!.Value, Total = g.Count() })
                .ToListAsync());

            var recentes = (await EmCadaBanco(c => c.Logs.AsNoTracking()
                    .Where(l => l.UsuarioId != null)
                    .OrderByDescending(l => l.Quando)
                    .Take(300)
                    .Select(l => new { l.Quando, l.UsuarioId, l.UsuarioNome, l.Modulo, l.Descricao })
                    .ToListAsync()))
                .OrderByDescending(l => l.Quando)
                .Take(300)
                .ToList();

            var visao = new VisaoTenants();

            foreach (var empresa in empresas)
            {
                var deles = usuarios.Where(u => u.EmpresaId == empresa.Id).ToList();
                var ids = deles.Select(u => u.Id).ToHashSet();
                var login = ultimosLogins.Where(l => ids.Contains(l.Id)).OrderByDescending(l => l.Quando).FirstOrDefault();
                var acao = recentes.FirstOrDefault(r => ids.Contains(r.UsuarioId!.Value));

                visao.Tenants.Add(new Tenant
                {
                    Id = empresa.Id,
                    Nome = empresa.Nome,
                    Cnpj = empresa.CNPJ,
                    Banco = _conexoes.Normalizar(empresa.Banco),
                    Ativa = empresa.Ativa,
                    CriadoEm = empresa.CriadoEm,
                    SuspensaEm = empresa.SuspensaEm,
                    Usuarios = deles.Select(u => new UsuarioDoTenant
                    {
                        Id = u.Id,
                        Nome = $"{u.Nome} {u.Sobrenome}".Trim(),
                        Email = u.Email ?? "",
                        Ativo = u.Status == Erp.Model.Usuario.StatusUsuario.Ativo,
                        RestritoAte = u.LockoutEnd,
                    }).OrderBy(u => u.Nome).ToList(),
                    UltimoLogin = login?.Quando,
                    UltimoLoginDe = login is null ? null : nomeDe.GetValueOrDefault(login.Id),
                    Acoes7Dias = acoesSemana.Where(a => ids.Contains(a.Id)).Sum(a => a.Total),
                    UltimaAcao = acao?.Quando,
                    UltimaAcaoDescricao = acao?.Descricao,
                    UltimaAcaoDe = acao?.UsuarioNome,
                });
            }

            var nomeTenant = empresas.ToDictionary(e => e.Id, e => e.Nome);

            visao.Atividade = recentes
                .Take(10)
                .Select(r => new AtividadeRecente(
                    r.Quando,
                    r.UsuarioNome,
                    tenantDe.TryGetValue(r.UsuarioId!.Value, out var t) ? nomeTenant.GetValueOrDefault(t, "") : "",
                    r.Modulo,
                    r.Descricao))
                .ToList();

            return visao;
        }

        /// <summary>
        /// Suspende ou reativa um tenant. Suspenso, ninguém da empresa entra e
        /// as sessões abertas caem na revalidação seguinte (até 5 minutos).
        /// </summary>
        public async Task DefinirSituacao(Guid empresaId, bool ativa, Guid porQuem)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var empresa = await contexto.Empresas.FirstOrDefaultAsync(e => e.Id == empresaId)
                ?? throw new InvalidOperationException("Empresa não encontrada.");

            if (empresa.Ativa == ativa)
                return;

            empresa.Ativa = ativa;
            empresa.SuspensaEm = ativa ? null : DateTime.UtcNow;
            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, "Plataforma",
                $"Tenant {empresa.Nome} {(ativa ? "reativado" : "suspenso")}.",
                porQuem,
                entidade: "Empresa",
                entidadeId: empresa.Id.ToString());
        }

        // ---------------- Comunicados ----------------

        public enum DestinoComunicado { Todos, Tenant, Usuarios }

        public sealed class ComunicadoEnviado
        {
            public Guid EnvioId { get; set; }
            public Erp.Model.Notificacao.TipoNotificacao Tipo { get; set; }
            public string Titulo { get; set; } = "";
            public DateTime EnviadoEm { get; set; }
            public int Destinatarios { get; set; }
            public int Confirmados { get; set; }
        }

        /// <summary>
        /// Recado ou nota de atualização para muita gente de uma vez. Cada
        /// destinatário recebe a sua cópia (é ela que ele confirma), todas com o
        /// mesmo EnvioId. Vai só para usuários ativos dos tenants — o dono não
        /// manda comunicado para si.
        /// </summary>
        public async Task<int> EnviarComunicado(
            Erp.Model.Notificacao.TipoNotificacao tipo, string titulo, string mensagem,
            DestinoComunicado destino, Guid? tenantId, IReadOnlyCollection<Guid>? usuarios, Guid porQuem)
        {
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(mensagem))
                throw new InvalidOperationException("Informe título e mensagem.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var consulta = contexto.Usuarios
                .Where(u => u.EmpresaId != null && u.Status == Erp.Model.Usuario.StatusUsuario.Ativo);

            consulta = destino switch
            {
                DestinoComunicado.Tenant => consulta.Where(u => u.EmpresaId == tenantId),
                DestinoComunicado.Usuarios => consulta.Where(u => usuarios!.Contains(u.Id)),
                _ => consulta,
            };

            var destinatarios = await consulta.Select(u => new { u.Id, u.EmpresaId }).ToListAsync();
            if (destinatarios.Count == 0)
                throw new InvalidOperationException("Nenhum destinatário ativo.");

            var ids = destinatarios.Select(d => d.Id).ToList();
            var bancoDe = await contexto.Empresas.ToDictionaryAsync(e => e.Id, e => e.Banco);

            var envio = Guid.CreateVersion7();
            var agora = DateTime.UtcNow;
            var nota = tipo == Erp.Model.Notificacao.TipoNotificacao.NotaAtualizacao;

            // A notificação mora no banco do tenant de cada destinatário.
            foreach (var grupo in destinatarios.GroupBy(d => _conexoes.Normalizar(bancoDe.GetValueOrDefault(d.EmpresaId!.Value))))
            {
            await using var doTenant = _conexoes.Criar(grupo.Key);
            doTenant.Notificacoes.AddRange(grupo.Select(d => d.Id).Select(id => new Erp.Model.Notificacao.Notificacao
            {
                DestinatarioId = id,
                Tipo = tipo,
                EnvioId = envio,
                Remetente = "Equipe da plataforma",
                Titulo = titulo.Trim(),
                Mensagem = mensagem.Trim(),
                Icone = nota ? "sparkles" : "megaphone",
                Link = "/home/painel",
                CriadaEm = agora,
            }));

            await doTenant.SaveChangesAsync();
            }

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, "Plataforma",
                $"{(nota ? "Nota de atualização" : "Recado")} \"{titulo.Trim()}\" enviado para {ids.Count} "
                + (ids.Count == 1 ? "usuário." : "usuários."),
                porQuem,
                entidade: "Comunicado",
                entidadeId: envio.ToString());

            return ids.Count;
        }

        public async Task<List<ComunicadoEnviado>> ComunicadosEnviados()
        {
            var partes = await EmCadaBanco(c => c.Notificacoes
                .Where(n => n.EnvioId != null)
                .GroupBy(n => new { n.EnvioId, n.Tipo, n.Titulo })
                .Select(g => new ComunicadoEnviado
                {
                    EnvioId = g.Key.EnvioId!.Value,
                    Tipo = g.Key.Tipo,
                    Titulo = g.Key.Titulo,
                    EnviadoEm = g.Min(n => n.CriadaEm),
                    Destinatarios = g.Count(),
                    Confirmados = g.Count(n => n.Confirmada),
                })
                .ToListAsync());

            // Um envio para vários tenants tem um pedaço em cada banco.
            return partes
                .GroupBy(p => p.EnvioId)
                .Select(g => new ComunicadoEnviado
                {
                    EnvioId = g.Key,
                    Tipo = g.First().Tipo,
                    Titulo = g.First().Titulo,
                    EnviadoEm = g.Min(p => p.EnviadoEm),
                    Destinatarios = g.Sum(p => p.Destinatarios),
                    Confirmados = g.Sum(p => p.Confirmados),
                })
                .OrderByDescending(c => c.EnviadoEm)
                .Take(30)
                .ToList();
        }

        // ---------------- Origem dos acessos ----------------

        public sealed class OrigemAcesso
        {
            public string Nome { get; set; } = "";
            public double Lat { get; set; }
            public double Lon { get; set; }
            public int Acessos { get; set; }
            public int Usuarios { get; set; }
        }

        public sealed class Origens
        {
            public List<OrigemAcesso> Pontos { get; set; } = new();
            public int Logins { get; set; }
            public int SemLocalizacao { get; set; }
        }

        public async Task<Origens> OrigensDosAcessos(int dias)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();
            var desde = DateTime.UtcNow.AddDays(-dias);

            var logins = await contexto.LogsSistema
                .Where(l => l.Evento == TipoEventoSistema.Login && l.Quando >= desde)
                .Select(l => new { l.UsuarioId, l.Cidade, l.Pais, l.Latitude, l.Longitude })
                .ToListAsync();

            return new Origens
            {
                Logins = logins.Count,
                SemLocalizacao = logins.Count(l => l.Latitude is null || l.Longitude is null),
                Pontos = logins
                    .Where(l => l.Latitude is not null && l.Longitude is not null)
                    .GroupBy(l => new { Lat = Math.Round(l.Latitude!.Value, 2), Lon = Math.Round(l.Longitude!.Value, 2) })
                    .Select(g => new OrigemAcesso
                    {
                        Nome = string.Join(", ", new[] { g.First().Cidade, g.First().Pais }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Lat = g.Key.Lat,
                        Lon = g.Key.Lon,
                        Acessos = g.Count(),
                        Usuarios = g.Select(l => l.UsuarioId).Distinct().Count(),
                    })
                    .OrderByDescending(o => o.Acessos)
                    .ToList(),
            };
        }

        // ---------------- Saúde do banco ----------------

        public sealed class TabelaLinhas
        {
            public string Tabela { get; set; } = "";
            public long Linhas { get; set; }
        }

        public sealed class SaudeBanco
        {
            public bool Online { get; set; }
            public string? Erro { get; set; }
            public double LatenciaMs { get; set; }
            public string Versao { get; set; } = "";
            public long TamanhoBytes { get; set; }
            public long Conexoes { get; set; }
            public int MigracoesAplicadas { get; set; }
            public List<string> MigracoesPendentes { get; set; } = new();
            public string? UltimaMigracao { get; set; }
            public List<TabelaLinhas> MaioresTabelas { get; set; } = new();
        }

        public async Task<SaudeBanco> Banco()
        {
            var saude = new SaudeBanco();

            try
            {
                await using var contexto = await _fabrica.CreateDbContextAsync();

                var relogio = System.Diagnostics.Stopwatch.StartNew();
                await contexto.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\"").ToListAsync();
                saude.LatenciaMs = relogio.Elapsed.TotalMilliseconds;
                saude.Online = true;

                saude.Versao = (await contexto.Database
                    .SqlQueryRaw<string>("SELECT current_setting('server_version') AS \"Value\"")
                    .ToListAsync()).FirstOrDefault() ?? "";

                saude.TamanhoBytes = (await contexto.Database
                    .SqlQueryRaw<long>("SELECT pg_database_size(current_database()) AS \"Value\"")
                    .ToListAsync()).FirstOrDefault();

                saude.Conexoes = (await contexto.Database
                    .SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_stat_activity WHERE datname = current_database()")
                    .ToListAsync()).FirstOrDefault();

                var aplicadas = (await contexto.Database.GetAppliedMigrationsAsync()).ToList();
                saude.MigracoesAplicadas = aplicadas.Count;
                saude.UltimaMigracao = aplicadas.LastOrDefault();
                saude.MigracoesPendentes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();

                saude.MaioresTabelas = await contexto.Database
                    .SqlQueryRaw<TabelaLinhas>(
                        "SELECT relname AS \"Tabela\", n_live_tup AS \"Linhas\" " +
                        "FROM pg_stat_user_tables ORDER BY n_live_tup DESC LIMIT 8")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                saude.Online = false;
                saude.Erro = ex.Message;
            }

            return saude;
        }

        // ---------------- Eventos de sistema ----------------

        public sealed record EventoRecente(DateTime Quando, string Mensagem, string Usuario, bool TemStackTrace);

        public sealed class ResumoEventos
        {
            public Dictionary<TipoEventoSistema, int> PorTipo { get; set; } = new();
            public List<EventoRecente> UltimasExcecoes { get; set; } = new();
            public List<EventoRecente> UltimosLoginsFalhos { get; set; } = new();
            /// <summary>Por hora local das últimas 24 h: (exceções, logins falhos).</summary>
            public List<(DateTime Hora, int Excecoes, int Falhos)> PorHora { get; set; } = new();
        }

        public async Task<ResumoEventos> Eventos(DateTime desde)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var resumo = new ResumoEventos
            {
                PorTipo = await contexto.LogsSistema
                    .Where(l => l.Quando >= desde)
                    .GroupBy(l => l.Evento)
                    .Select(g => new { g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Total),
            };

            resumo.UltimasExcecoes = (await contexto.LogsSistema
                .Where(l => l.Evento == TipoEventoSistema.Excecao)
                .OrderByDescending(l => l.Quando)
                .Take(8)
                .Select(l => new { l.Quando, l.Mensagem, l.UsuarioNome, l.StackTrace })
                .ToListAsync())
                .Select(l => new EventoRecente(l.Quando, l.Mensagem, l.UsuarioNome, !string.IsNullOrWhiteSpace(l.StackTrace)))
                .ToList();

            resumo.UltimosLoginsFalhos = (await contexto.LogsSistema
                .Where(l => l.Evento == TipoEventoSistema.LoginFalho)
                .OrderByDescending(l => l.Quando)
                .Take(8)
                .Select(l => new { l.Quando, l.Mensagem, l.Identificacao, l.Ip })
                .ToListAsync())
                .Select(l => new EventoRecente(l.Quando, $"{l.Mensagem} {l.Ip}".Trim(), l.Identificacao, false))
                .ToList();

            var dia = DateTime.UtcNow.AddHours(-24);
            var recentes = await contexto.LogsSistema
                .Where(l => l.Quando >= dia
                         && (l.Evento == TipoEventoSistema.Excecao || l.Evento == TipoEventoSistema.LoginFalho))
                .Select(l => new { l.Quando, l.Evento })
                .ToListAsync();

            var inicio = DateTime.Now.AddHours(-23);
            inicio = new DateTime(inicio.Year, inicio.Month, inicio.Day, inicio.Hour, 0, 0, DateTimeKind.Local);

            resumo.PorHora = Enumerable.Range(0, 24)
                .Select(h => inicio.AddHours(h))
                .Select(hora => (hora,
                    recentes.Count(r => r.Evento == TipoEventoSistema.Excecao && Mesmahora(r.Quando, hora)),
                    recentes.Count(r => r.Evento == TipoEventoSistema.LoginFalho && Mesmahora(r.Quando, hora))))
                .ToList();

            return resumo;
        }

        private static bool Mesmahora(DateTime utc, DateTime horaLocal)
        {
            var local = utc.ToLocalTime();
            return local >= horaLocal && local < horaLocal.AddHours(1);
        }

        // ---------------- Uso por usuário ----------------

        public sealed class MetricaUsuario
        {
            public Guid Id { get; set; }
            public string Nome { get; set; } = "";
            public string Email { get; set; } = "";
            public string Papel { get; set; } = "";
            public bool Ativo { get; set; }
            public DateTime? UltimoLogin { get; set; }
            public int Logins { get; set; }
            public int LoginsFalhos { get; set; }
            public int Acoes { get; set; }
            public string? ModuloPrincipal { get; set; }
        }

        public sealed class UsoUsuarios
        {
            public List<MetricaUsuario> Usuarios { get; set; } = new();
            public List<(DateTime Dia, int Ativos)> AtivosPorDia { get; set; } = new();
            public List<(string Modulo, int Acoes)> AcoesPorModulo { get; set; } = new();
        }

        public async Task<UsoUsuarios> Uso(int dias)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();
            var desde = DateTime.UtcNow.AddDays(-dias);

            var usuarios = await contexto.Usuarios.AsNoTracking()
                .Select(u => new { u.Id, u.Nome, u.Sobrenome, u.Email, u.Status })
                .ToListAsync();

            var papeis = await (from vinculo in contexto.UserRoles
                                join papel in contexto.Roles on vinculo.RoleId equals papel.Id
                                select new { vinculo.UserId, papel.Name })
                               .ToListAsync();

            var logins = await contexto.LogsSistema
                .Where(l => l.UsuarioId != null && l.Evento == TipoEventoSistema.Login)
                .GroupBy(l => l.UsuarioId)
                .Select(g => new
                {
                    Id = g.Key,
                    Ultimo = g.Max(x => x.Quando),
                    NoPeriodo = g.Count(x => x.Quando >= desde),
                })
                .ToListAsync();

            var falhos = await contexto.LogsSistema
                .Where(l => l.Evento == TipoEventoSistema.LoginFalho && l.Quando >= desde)
                .GroupBy(l => l.Identificacao.ToLower())
                .Select(g => new { Email = g.Key, Total = g.Count() })
                .ToListAsync();

            var acoes = await EmCadaBanco(c => c.Logs
                .Where(l => l.UsuarioId != null && l.Quando >= desde)
                .GroupBy(l => new { l.UsuarioId, l.Modulo })
                .Select(g => new { g.Key.UsuarioId, g.Key.Modulo, Total = g.Count() })
                .ToListAsync());

            var uso = new UsoUsuarios();

            uso.Usuarios = usuarios.Select(u =>
            {
                var login = logins.FirstOrDefault(l => l.Id == u.Id);
                var dele = acoes.Where(a => a.UsuarioId == u.Id).ToList();

                return new MetricaUsuario
                {
                    Id = u.Id,
                    Nome = $"{u.Nome} {u.Sobrenome}".Trim(),
                    Email = u.Email ?? "",
                    Papel = papeis.FirstOrDefault(p => p.UserId == u.Id)?.Name ?? "",
                    Ativo = u.Status == Erp.Model.Usuario.StatusUsuario.Ativo,
                    UltimoLogin = login?.Ultimo,
                    Logins = login?.NoPeriodo ?? 0,
                    LoginsFalhos = falhos.FirstOrDefault(f => f.Email == (u.Email ?? "").ToLower())?.Total ?? 0,
                    Acoes = dele.Sum(a => a.Total),
                    ModuloPrincipal = dele.OrderByDescending(a => a.Total).FirstOrDefault()?.Modulo,
                };
            })
            .OrderByDescending(m => m.UltimoLogin ?? DateTime.MinValue)
            .ToList();

            uso.AcoesPorModulo = acoes
                .GroupBy(a => a.Modulo)
                .Select(g => (g.Key, g.Sum(a => a.Total)))
                .OrderByDescending(x => x.Item2)
                .Take(8)
                .ToList();

            // Ativo no dia = entrou OU fez alguma ação naquele dia.
            var inicioDias = DateTime.Today.AddDays(-13);
            var desdeDias = inicioDias.ToUniversalTime();

            var marcas = (await contexto.LogsSistema
                    .Where(l => l.UsuarioId != null && l.Evento == TipoEventoSistema.Login && l.Quando >= desdeDias)
                    .Select(l => new { l.UsuarioId, l.Quando })
                    .ToListAsync())
                .Concat(await EmCadaBanco(c => c.Logs
                    .Where(l => l.UsuarioId != null && l.Quando >= desdeDias)
                    .Select(l => new { l.UsuarioId, l.Quando })
                    .ToListAsync()))
                .ToList();

            uso.AtivosPorDia = Enumerable.Range(0, 14)
                .Select(d => inicioDias.AddDays(d))
                .Select(dia => (dia, marcas
                    .Where(m => m.Quando.ToLocalTime().Date == dia)
                    .Select(m => m.UsuarioId)
                    .Distinct()
                    .Count()))
                .ToList();

            return uso;
        }
    }
}
