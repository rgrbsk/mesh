using Erp.Data;
using Erp.Model.Log;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Log
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê. Aqui isso importa duas vezes: gravar log usa contexto PRÓPRIO,
    /// separado do da operação que está sendo registrada, para o registro
    /// sobreviver a um rollback e para uma falha de log nunca derrubar a
    /// operação de negócio.
    /// </summary>
    public class LogRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly ILogger<LogRepository> _log;

        public LogRepository(IDbContextFactory<AppDbContext> fabrica, ILogger<LogRepository> log)
        {
            _fabrica = fabrica;
            _log = log;
        }

        public async Task<List<RegistroLog>> Buscar(Expression<Func<RegistroLog, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Logs.AsNoTracking().AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            // Mais recente primeiro, e teto: histórico cresce sem parar e a
            // grade não deve puxar o banco inteiro para a memória.
            return await query
                .OrderByDescending(l => l.Quando)
                .Take(2000)
                .ToListAsync();
        }

        /// <summary>
        /// Grava uma linha. Nunca lança: auditoria é efeito colateral, e uma
        /// aprovação não pode falhar porque o log falhou. O erro vai para o
        /// logger da aplicação, onde alguém vê sem quebrar o usuário.
        /// </summary>
        public async Task Registrar(
            TipoAcao acao,
            string modulo,
            string descricao,
            Guid? usuarioId = null,
            string usuarioNome = "",
            string entidade = "",
            string entidadeId = "",
            string? stackTrace = null)
        {
            try
            {
                await using var contexto = await _fabrica.CreateDbContextAsync();

                contexto.Logs.Add(new RegistroLog
                {
                    Quando = DateTime.UtcNow,
                    Acao = acao,
                    Modulo = modulo,
                    Descricao = descricao,
                    UsuarioId = usuarioId,
                    UsuarioNome = usuarioNome,
                    Entidade = entidade,
                    EntidadeId = entidadeId,
                    StackTrace = stackTrace,
                });

                await contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao gravar log: {Descricao}", descricao);
            }
        }

        /// <summary>
        /// Registra uma falha com o rastreamento completo. A mensagem da exceção
        /// entra na descrição; o stack fica no campo protegido, que só quem tem
        /// logs.stacktrace enxerga.
        /// </summary>
        public Task RegistrarErro(
            Exception excecao,
            string modulo,
            string contexto,
            Guid? usuarioId = null,
            string usuarioNome = "") =>
            Registrar(
                TipoAcao.Erro,
                modulo,
                $"{contexto}: {excecao.Message}",
                usuarioId,
                usuarioNome,
                excecao.GetType().Name,
                string.Empty,
                excecao.ToString());
    }
}
