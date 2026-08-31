using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Notificacao
{
    using Notificacao = Erp.Model.Notificacao.Notificacao;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê. Notificar nunca lança: um aviso que falha não pode impedir a
    /// aprovação que o gerou.
    /// </summary>
    public class NotificacaoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly ILogger<NotificacaoRepository> _log;

        public NotificacaoRepository(
            IDbContextFactory<AppDbContext> fabrica, ILogger<NotificacaoRepository> log)
        {
            _fabrica = fabrica;
            _log = log;
        }

        /// <summary>As mais recentes de uma pessoa, lidas ou não.</summary>
        public async Task<List<Notificacao>> Buscar(Guid destinatarioId, int quantas = 30)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Notificacoes
                .AsNoTracking()
                .Where(n => n.DestinatarioId == destinatarioId)
                .OrderByDescending(n => n.CriadaEm)
                .Take(quantas)
                .ToListAsync();
        }

        public async Task<int> NaoLidas(Guid destinatarioId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Notificacoes
                .CountAsync(n => n.DestinatarioId == destinatarioId && !n.Lida);
        }

        public async Task Criar(
            Guid destinatarioId, string titulo, string mensagem, string icone = "bell", string link = "")
        {
            try
            {
                await using var contexto = await _fabrica.CreateDbContextAsync();

                contexto.Notificacoes.Add(new Notificacao
                {
                    DestinatarioId = destinatarioId,
                    Titulo = titulo,
                    Mensagem = mensagem,
                    Icone = icone,
                    Link = link,
                    CriadaEm = DateTime.UtcNow,
                });

                await contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao notificar {Destinatario}: {Titulo}", destinatarioId, titulo);
            }
        }

        /// <summary>
        /// Avisa várias pessoas de uma vez, sem repetir destinatário. Uma
        /// solicitação com dois itens do mesmo centro de custo geraria dois
        /// avisos idênticos para o mesmo aprovador.
        /// </summary>
        public async Task CriarParaVarios(
            IEnumerable<Guid> destinatarios, string titulo, string mensagem,
            string icone = "bell", string link = "")
        {
            var unicos = destinatarios.Distinct().ToList();

            if (unicos.Count == 0)
                return;

            try
            {
                await using var contexto = await _fabrica.CreateDbContextAsync();

                var agora = DateTime.UtcNow;

                contexto.Notificacoes.AddRange(unicos.Select(id => new Notificacao
                {
                    DestinatarioId = id,
                    Titulo = titulo,
                    Mensagem = mensagem,
                    Icone = icone,
                    Link = link,
                    CriadaEm = agora,
                }));

                await contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao notificar {Quantos} destinatários: {Titulo}",
                    unicos.Count, titulo);
            }
        }

        public async Task MarcarLida(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var notificacao = await contexto.Notificacoes.FirstOrDefaultAsync(n => n.Id == id);
            if (notificacao is null || notificacao.Lida)
                return;

            notificacao.Lida = true;
            notificacao.LidaEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();
        }

        public async Task MarcarTodasLidas(Guid destinatarioId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var agora = DateTime.UtcNow;

            await contexto.Notificacoes
                .Where(n => n.DestinatarioId == destinatarioId && !n.Lida)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.Lida, true)
                    .SetProperty(n => n.LidaEm, agora));
        }
    }
}
