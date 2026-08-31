using Erp.Data;
using Erp.Model.Log;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Log
{
    /// <summary>
    /// Grava e lê o log técnico. Como o de negócio: contexto próprio e nunca
    /// lança — registrar um login não pode impedir o login de acontecer.
    /// </summary>
    public class LogSistemaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly ILogger<LogSistemaRepository> _log;

        public LogSistemaRepository(
            IDbContextFactory<AppDbContext> fabrica, ILogger<LogSistemaRepository> log)
        {
            _fabrica = fabrica;
            _log = log;
        }

        public async Task<List<LogSistema>> Buscar(Expression<Func<LogSistema, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.LogsSistema.AsNoTracking().AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query
                .OrderByDescending(l => l.Quando)
                .Take(2000)
                .ToListAsync();
        }

        /// <summary>Os últimos acessos de um usuário — é o que o perfil mostra
        /// para a pessoa reconhecer (ou não) de onde entraram na conta dela.</summary>
        public async Task<List<LogSistema>> AcessosDoUsuario(Guid usuarioId, int quantos = 10)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.LogsSistema
                .AsNoTracking()
                .Where(l => l.UsuarioId == usuarioId
                         && (l.Evento == TipoEventoSistema.Login
                          || l.Evento == TipoEventoSistema.LoginFalho))
                .OrderByDescending(l => l.Quando)
                .Take(quantos)
                .ToListAsync();
        }

        public async Task Registrar(LogSistema registro)
        {
            try
            {
                await using var contexto = await _fabrica.CreateDbContextAsync();

                registro.Quando = DateTime.UtcNow;
                registro.Dispositivo = DescreverDispositivo(registro.UserAgent);

                contexto.LogsSistema.Add(registro);
                await contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao gravar log de sistema: {Evento}", registro.Evento);
            }
        }

        public Task RegistrarExcecao(
            Exception excecao, string contexto, Guid? usuarioId = null, string usuarioNome = "") =>
            Registrar(new LogSistema
            {
                Evento = TipoEventoSistema.Excecao,
                Mensagem = $"{contexto}: {excecao.Message}",
                UsuarioId = usuarioId,
                UsuarioNome = usuarioNome,
                Detalhes = excecao.GetType().FullName,
                StackTrace = excecao.ToString(),
            });

        /// <summary>
        /// Resumo legível do User-Agent. É heurística de propósito: reconhecer
        /// navegador exatamente é um problema sem fim, e aqui o objetivo é só
        /// dar ao usuário uma pista do tipo "Chrome no Windows" para ele
        /// reconhecer o próprio acesso.
        /// </summary>
        public static string DescreverDispositivo(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "";

            var navegador =
                userAgent.Contains("Edg/") ? "Edge" :
                userAgent.Contains("OPR/") || userAgent.Contains("Opera") ? "Opera" :
                userAgent.Contains("Firefox") ? "Firefox" :
                // Chrome tem que vir DEPOIS de Edge e Opera: os dois se declaram
                // Chrome no User-Agent, e a ordem inversa marcaria todo mundo
                // como Chrome.
                userAgent.Contains("Chrome") ? "Chrome" :
                userAgent.Contains("Safari") ? "Safari" :
                "Navegador desconhecido";

            var sistema =
                userAgent.Contains("Windows") ? "Windows" :
                userAgent.Contains("Android") ? "Android" :
                userAgent.Contains("iPhone") || userAgent.Contains("iPad") ? "iOS" :
                userAgent.Contains("Mac OS") ? "macOS" :
                userAgent.Contains("Linux") ? "Linux" :
                "";

            return string.IsNullOrEmpty(sistema) ? navegador : $"{navegador} · {sistema}";
        }
    }
}
