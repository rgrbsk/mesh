using Erp.Data;
using Erp.Model.Solicitacao;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Solicitacao
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class SolicitacaoRepository
    {
        private const string Modulo = "Compras";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly Erp.Repository.Notificacao.NotificacaoRepository _avisos;

        public SolicitacaoRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            Erp.Repository.Notificacao.NotificacaoRepository avisos)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
        }

        /// <summary>Lista aplicando o filtro montado no BbFilterBuilder — a
        /// expressão vai para o Where do EF, não para memória.</summary>
        public async Task<List<SolicitacaoCompra>> Buscar(Expression<Func<SolicitacaoCompra, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Solicitacoes
                .AsNoTracking()
                .Include(s => s.Solicitante)
                .Include(s => s.Etapa)
                .Include(s => s.Itens).ThenInclude(i => i.Produto)
                .Include(s => s.Itens).ThenInclude(i => i.CentroCusto)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderByDescending(s => s.Id).ToListAsync();
        }

        public async Task<SolicitacaoCompra?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Solicitacoes
                .AsNoTracking()
                .Include(s => s.Solicitante)
                .Include(s => s.Etapa)
                .Include(s => s.Itens).ThenInclude(i => i.Produto)
                .Include(s => s.Itens).ThenInclude(i => i.CentroCusto)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Pendentes de decisão que caem no colo de quem responde por estes
        /// centros de custo. A solicitação vem inteira, mas quem chama olha só os
        /// itens dos centros dele.
        /// </summary>
        public async Task<List<SolicitacaoCompra>> ParaAprovar(IEnumerable<int> centrosDoAprovador)
        {
            var centros = centrosDoAprovador.ToList();

            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Solicitacoes
                .AsNoTracking()
                .Include(s => s.Solicitante)
                .Include(s => s.Etapa)
                .Include(s => s.Itens).ThenInclude(i => i.Produto)
                .Include(s => s.Itens).ThenInclude(i => i.CentroCusto)
                .Where(s => s.Status == StatusSolicitacao.Enviada
                         && s.Itens.Any(i => i.Status == StatusItem.Pendente
                                          && centros.Contains(i.CentroCustoId)))
                .OrderBy(s => s.EnviadaEm)
                .ToListAsync();
        }

        public async Task<SolicitacaoCompra> Criar(SolicitacaoCompra solicitacao)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            solicitacao.CriadoEm = DateTime.UtcNow;
            solicitacao.ModificadoEm = solicitacao.CriadoEm;

            LimparNavegacoes(solicitacao);

            contexto.Solicitacoes.Add(solicitacao);
            await contexto.SaveChangesAsync();

            return solicitacao;
        }

        /// <summary>
        /// Salva cabeçalho e itens de uma vez. Os itens da tela são a verdade:
        /// o que sumiu da lista é apagado, o que veio sem Id é inserido. Sem
        /// isso, remover uma linha na tela não removeria nada no banco.
        /// </summary>
        public async Task<SolicitacaoCompra> Atualizar(SolicitacaoCompra solicitacao)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var gravada = await contexto.Solicitacoes
                .Include(s => s.Itens)
                .FirstOrDefaultAsync(s => s.Id == solicitacao.Id)
                ?? throw new InvalidOperationException("Solicitação não encontrada.");

            gravada.Observacao = solicitacao.Observacao;
            gravada.Status = solicitacao.Status;
            gravada.EnviadaEm = solicitacao.EnviadaEm;
            gravada.ModificadoEm = DateTime.UtcNow;

            var idsNaTela = solicitacao.Itens.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

            foreach (var removido in gravada.Itens.Where(i => !idsNaTela.Contains(i.Id)).ToList())
                contexto.ItensSolicitacao.Remove(removido);

            foreach (var item in solicitacao.Itens)
            {
                var alvo = item.Id == 0 ? null : gravada.Itens.FirstOrDefault(i => i.Id == item.Id);

                if (alvo is null)
                {
                    gravada.Itens.Add(new ItemSolicitacao
                    {
                        ProdutoId = item.ProdutoId,
                        Quantidade = item.Quantidade,
                        CentroCustoId = item.CentroCustoId,
                        PrazoDesejado = item.PrazoDesejado,
                        Justificativa = item.Justificativa,
                        Status = item.Status,
                    });
                    continue;
                }

                alvo.ProdutoId = item.ProdutoId;
                alvo.Quantidade = item.Quantidade;
                alvo.CentroCustoId = item.CentroCustoId;
                alvo.PrazoDesejado = item.PrazoDesejado;
                alvo.Justificativa = item.Justificativa;
                alvo.Status = item.Status;
            }

            await contexto.SaveChangesAsync();

            return gravada;
        }

        /// <summary>
        /// Move o cartão de coluna no Kanban. Só antes da aprovação: depois de
        /// enviada a solicitação está na fila de outra pessoa, e arrastar de
        /// volta apagaria a decisão dela.
        /// </summary>
        public async Task MoverParaEtapa(int id, int etapaId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var solicitacao = await contexto.Solicitacoes.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Solicitação não encontrada.");

            if (!solicitacao.Editavel)
                throw new InvalidOperationException(
                    "Solicitação já enviada não volta para as etapas anteriores.");

            solicitacao.EtapaId = etapaId;
            solicitacao.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();
        }

        /// <summary>
        /// Tira do rascunho. A partir daqui a solicitação não se edita mais: ela
        /// já está na fila de alguém.
        /// </summary>
        public async Task Enviar(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var solicitacao = await contexto.Solicitacoes
                .Include(s => s.Itens)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Solicitação não encontrada.");

            if (solicitacao.Itens.Count == 0)
                throw new InvalidOperationException("Uma solicitação sem itens não tem o que aprovar.");

            solicitacao.Status = StatusSolicitacao.Enviada;
            solicitacao.EnviadaEm = DateTime.UtcNow;
            solicitacao.ModificadoEm = solicitacao.EnviadaEm.Value;

            // Devolvida que volta a ser enviada recomeça a decisão de quem tinha
            // devolvido — o resto do que já foi decidido continua valendo.
            foreach (var item in solicitacao.Itens.Where(i => i.Status == StatusItem.Devolvido))
            {
                item.Status = StatusItem.Pendente;
                item.MotivoDecisao = null;
                item.DecididoPorId = null;
                item.DecididoEm = null;
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, Modulo,
                $"Solicitação #{id} enviada para aprovação com {solicitacao.Itens.Count} "
                + (solicitacao.Itens.Count == 1 ? "item." : "itens."),
                solicitacao.SolicitanteId,
                entidade: nameof(SolicitacaoCompra),
                entidadeId: id.ToString());

            // Avisa quem responde pelos centros de custo dos itens. Sem isto o
            // aprovador só descobre que chegou algo se lembrar de abrir a tela.
            var centros = solicitacao.Itens.Select(i => i.CentroCustoId).Distinct().ToList();

            var aprovadores = await contexto.CentrosCusto
                .Where(c => centros.Contains(c.Id) && c.ResponsavelId != null)
                .Select(c => c.ResponsavelId!.Value)
                .ToListAsync();

            await _avisos.CriarParaVarios(
                aprovadores,
                "Solicitação aguardando sua aprovação",
                $"A solicitação #{id} tem itens em centros de custo que você responde.",
                "circle-check",
                "/home/aprovacoes");
        }

        /// <summary>
        /// Decide UM item. Quem decide é o responsável pelo centro de custo
        /// daquele item — a checagem é aqui, com o id de quem está decidindo,
        /// e não só na tela: a tela some, a regra fica.
        /// </summary>
        public async Task Decidir(
            int itemId, StatusItem decisao, string? motivo, Guid decisorId, IEnumerable<int> centrosDoDecisor)
        {
            if (decisao is StatusItem.Recusado or StatusItem.Devolvido
                && string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException(
                    "Recusar ou devolver exige motivo — sem ele o solicitante não sabe o que corrigir.");

            var centros = centrosDoDecisor.ToHashSet();

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var item = await contexto.ItensSolicitacao
                .Include(i => i.Solicitacao!).ThenInclude(s => s.Itens)
                .FirstOrDefaultAsync(i => i.Id == itemId)
                ?? throw new InvalidOperationException("Item não encontrado.");

            if (!centros.Contains(item.CentroCustoId))
                throw new InvalidOperationException(
                    "Este item pertence a um centro de custo que não é seu para decidir.");

            if (item.Solicitacao?.Status != StatusSolicitacao.Enviada)
                throw new InvalidOperationException("Esta solicitação não está aguardando decisão.");

            item.Status = decisao;
            item.MotivoDecisao = motivo;
            item.DecididoPorId = decisorId;
            item.DecididoEm = DateTime.UtcNow;

            ConsolidarCabecalho(item.Solicitacao);

            await contexto.SaveChangesAsync();

            var acao = decisao switch
            {
                StatusItem.Aprovado => Erp.Model.Log.TipoAcao.Aprovacao,
                StatusItem.Recusado => Erp.Model.Log.TipoAcao.Recusa,
                _ => Erp.Model.Log.TipoAcao.Devolucao,
            };

            await _logs.Registrar(
                acao, Modulo,
                $"Item #{item.Id} da solicitação #{item.SolicitacaoId}: {decisao}."
                + (string.IsNullOrWhiteSpace(motivo) ? "" : $" Motivo: {motivo}"),
                decisorId,
                entidade: nameof(SolicitacaoCompra),
                entidadeId: item.SolicitacaoId.ToString());

            // Recusa e devolução voltam para o solicitante — são as decisões que
            // exigem ação dele. Aprovação não notifica item a item: numa
            // solicitação de dez linhas seriam dez avisos iguais.
            if (decisao is StatusItem.Recusado or StatusItem.Devolvido)
                await _avisos.Criar(
                    item.Solicitacao.SolicitanteId,
                    decisao == StatusItem.Recusado
                        ? "Item recusado"
                        : "Solicitação devolvida para ajuste",
                    $"Solicitação #{item.SolicitacaoId}: {motivo}",
                    decisao == StatusItem.Recusado ? "circle-x" : "undo-2",
                    "/home/compras");
        }

        /// <summary>
        /// O estado do cabeçalho é consequência dos itens, nunca digitado.
        /// Enquanto houver item pendente ela continua Enviada; devolução tem
        /// precedência sobre o resto porque devolve o pedido inteiro para a mão
        /// do solicitante ajustar.
        /// </summary>
        private static void ConsolidarCabecalho(SolicitacaoCompra solicitacao)
        {
            solicitacao.ModificadoEm = DateTime.UtcNow;

            if (solicitacao.Itens.Any(i => i.Status == StatusItem.Devolvido))
            {
                solicitacao.Status = StatusSolicitacao.Devolvida;
                return;
            }

            if (solicitacao.Itens.Any(i => i.Status == StatusItem.Pendente))
            {
                solicitacao.Status = StatusSolicitacao.Enviada;
                return;
            }

            var aprovados = solicitacao.Itens.Count(i => i.Status == StatusItem.Aprovado);

            solicitacao.Status = aprovados switch
            {
                0 => StatusSolicitacao.Recusada,
                var n when n == solicitacao.Itens.Count => StatusSolicitacao.Aprovada,
                _ => StatusSolicitacao.AprovadaParcialmente,
            };
        }

        /// <summary>Só rascunho se exclui: o que já foi enviado é histórico de
        /// decisão de outra pessoa.</summary>
        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var solicitacao = await contexto.Solicitacoes.FirstOrDefaultAsync(s => s.Id == id);
            if (solicitacao is null)
                return false;

            if (solicitacao.Status != StatusSolicitacao.Rascunho)
                throw new InvalidOperationException("Só rascunhos podem ser excluídos.");

            contexto.Solicitacoes.Remove(solicitacao);
            await contexto.SaveChangesAsync();

            return true;
        }

        /// <summary>As navegações vêm preenchidas da tela (combos com Include).
        /// Mandá-las ao EF na inserção faria ele tentar inserir produto e centro
        /// de custo de novo.</summary>
        private static void LimparNavegacoes(SolicitacaoCompra solicitacao)
        {
            solicitacao.Solicitante = null;
            solicitacao.Etapa = null;

            foreach (var item in solicitacao.Itens)
            {
                item.Produto = null;
                item.CentroCusto = null;
                item.Solicitacao = null;
                item.DecididoPor = null;
            }
        }
    }
}
