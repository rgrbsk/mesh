using Erp.Data;
using Erp.Model.Solicitacao;
using Erp.Repository.Aprovacao;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Solicitacao
{
    /// <summary>Uma solicitação com os itens que ESTE usuário pode decidir agora
    /// — o resto dos itens vem junto, mas apenas para dar contexto.</summary>
    public sealed record ItemNaFila(
        ItemSolicitacao Item,
        DegrauResolvido Degrau,
        bool UltimoDegrau);

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
        private readonly AlcadaRepository _alcadas;
        private readonly Erp.Repository.Orcamento.OrcamentoRepository _orcamentos;

        public SolicitacaoRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            Erp.Repository.Notificacao.NotificacaoRepository avisos,
            AlcadaRepository alcadas,
            Erp.Repository.Orcamento.OrcamentoRepository orcamentos)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
            _alcadas = alcadas;
            _orcamentos = orcamentos;
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
                .Include(s => s.Itens).ThenInclude(i => i.Aprovacoes)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// A fila de UM aprovador.
        ///
        /// Com cadeia de alçada, pertencer ao centro não basta: o item só é
        /// desta pessoa se ela for o aprovador do DEGRAU em que ele está. Por
        /// isso a consulta busca grosso — os centros em que ela decide em algum
        /// degrau — e o degrau exato é conferido depois, sobre um conjunto já
        /// pequeno.
        /// </summary>
        public async Task<List<(SolicitacaoCompra Solicitacao, List<ItemNaFila> Itens)>> ParaAprovar(Guid aprovadorId)
        {
            var centros = await _alcadas.CentrosOndeDecide(aprovadorId);

            if (centros.Count == 0)
                return new();

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var solicitacoes = await contexto.Solicitacoes
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

            // Uma resolução por centro, não por item: numa fila de trinta itens
            // de cinco centros seriam trinta resoluções idênticas.
            var cadeias = new Dictionary<int, List<DegrauResolvido>>();

            foreach (var centroId in centros)
                cadeias[centroId] = await _alcadas.ResolverCadeia(centroId);

            var resultado = new List<(SolicitacaoCompra, List<ItemNaFila>)>();

            foreach (var solicitacao in solicitacoes)
            {
                var meus = new List<ItemNaFila>();

                foreach (var item in solicitacao.Itens.Where(i => i.Status == StatusItem.Pendente))
                {
                    if (!cadeias.TryGetValue(item.CentroCustoId, out var cadeia))
                        continue;

                    var degrau = cadeia.FirstOrDefault(d => d.Nivel == item.NivelAtual);

                    if (degrau is null || degrau.AprovadorId != aprovadorId)
                        continue;

                    meus.Add(new ItemNaFila(item, degrau, degrau.Encerra(item.ValorEstimado)));
                }

                if (meus.Count > 0)
                    resultado.Add((solicitacao, meus));
            }

            return resultado;
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
        ///
        /// É aqui que o VALOR de cada item é congelado, pelo preço de
        /// referência do produto. Ele resolve a alçada e estima o impacto no
        /// orçamento; congelá-lo evita que um reajuste de preço no cadastro
        /// mude, no meio do caminho, quem tem competência para decidir.
        /// </summary>
        public async Task Enviar(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var solicitacao = await contexto.Solicitacoes
                .Include(s => s.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Solicitação não encontrada.");

            if (solicitacao.Itens.Count == 0)
                throw new InvalidOperationException("Uma solicitação sem itens não tem o que aprovar.");

            solicitacao.Status = StatusSolicitacao.Enviada;
            solicitacao.EnviadaEm = DateTime.UtcNow;
            solicitacao.ModificadoEm = solicitacao.EnviadaEm.Value;

            foreach (var item in solicitacao.Itens)
            {
                item.ValorEstimado = item.Quantidade * (item.Produto?.PrecoReferencia ?? 0m);

                // Devolvida que volta a ser enviada recomeça a decisão de quem
                // tinha devolvido — o resto do que já foi decidido continua
                // valendo. A cadeia também recomeça: o item voltou a ser outro.
                if (item.Status == StatusItem.Devolvido)
                {
                    item.Status = StatusItem.Pendente;
                    item.MotivoDecisao = null;
                    item.DecididoPorId = null;
                    item.DecididoEm = null;
                    item.NivelAtual = 1;
                }
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, Modulo,
                $"Solicitação #{id} enviada para aprovação com {solicitacao.Itens.Count} "
                + (solicitacao.Itens.Count == 1 ? "item." : "itens.")
                + $" Valor estimado: {solicitacao.Itens.Sum(i => i.ValorEstimado):C2}.",
                solicitacao.SolicitanteId,
                entidade: nameof(SolicitacaoCompra),
                entidadeId: id.ToString());

            await AvisarAprovadoresDoNivel(solicitacao, nivel: 1);
        }

        /// <summary>
        /// Decide UM item.
        ///
        /// Quem decide é o aprovador do degrau em que o item está — a checagem
        /// é aqui, com o id de quem está decidindo, e não só na tela: a tela
        /// some, a regra fica.
        ///
        /// A novidade em relação à aprovação de degrau único: decisão favorável
        /// pode NÃO encerrar o item. Se o valor passa do teto deste degrau, o
        /// item sobe um nível e continua pendente — agora na fila de outra
        /// pessoa.
        /// </summary>
        public async Task Decidir(
            int itemId, StatusItem decisao, string? motivo, Guid decisorId, string decisorNome)
        {
            if (decisao is StatusItem.Recusado or StatusItem.Devolvido
                && string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException(
                    "Recusar ou devolver exige motivo — sem ele o solicitante não sabe o que corrigir.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var item = await contexto.ItensSolicitacao
                .Include(i => i.Solicitacao!).ThenInclude(s => s.Itens)
                .Include(i => i.Produto)
                .Include(i => i.CentroCusto)
                .FirstOrDefaultAsync(i => i.Id == itemId)
                ?? throw new InvalidOperationException("Item não encontrado.");

            if (item.Solicitacao?.Status != StatusSolicitacao.Enviada)
                throw new InvalidOperationException("Esta solicitação não está aguardando decisão.");

            if (item.Status != StatusItem.Pendente)
                throw new InvalidOperationException("Este item já foi decidido.");

            var cadeia = await _alcadas.ResolverCadeia(item.CentroCustoId);
            var degrau = cadeia.FirstOrDefault(d => d.Nivel == item.NivelAtual);

            if (degrau is null)
                throw new InvalidOperationException(
                    "O centro de custo deste item não tem aprovador configurado para este nível.");

            if (degrau.AprovadorId != decisorId)
                throw new InvalidOperationException(
                    "Este item não está no seu degrau de aprovação.");

            // Registra o passo ANTES de mexer no item: é este registro que
            // responde quem decidiu o quê em cada nível, e ele tem que existir
            // mesmo quando a decisão não encerra o item.
            contexto.AprovacoesItem.Add(new Erp.Model.Aprovacao.AprovacaoItem
            {
                ItemSolicitacaoId = item.Id,
                Nivel = item.NivelAtual,
                AprovadorId = decisorId,
                AprovadorNome = decisorNome,
                EmNomeDeId = degrau.PorSubstituto ? degrau.TitularId : null,
                EmNomeDeNome = degrau.PorSubstituto ? degrau.TitularNome : null,
                Decisao = decisao,
                Motivo = motivo,
                LimiteNoMomento = degrau.Limite,
                ValorNoMomento = item.ValorEstimado,
                DecididoEm = DateTime.UtcNow,
            });

            var promovido = false;

            if (decisao == StatusItem.Aprovado)
            {
                // O orçamento só é conferido na aprovação que ENCERRA o item:
                // conferir a cada degrau avisaria a mesma coisa três vezes.
                if (degrau.Encerra(item.ValorEstimado))
                {
                    var (_, _, estoura, bloqueia) = await _orcamentos.Simular(
                        item.CentroCustoId, item.ValorEstimado,
                        item.Solicitacao.EnviadaEm ?? DateTime.UtcNow);

                    if (bloqueia)
                        throw new InvalidOperationException(
                            $"Aprovar este item estoura o orçamento de {item.CentroCusto?.Nome}, "
                            + "que está configurado para bloquear. Reveja a verba ou o pedido.");

                    item.Status = StatusItem.Aprovado;
                    item.MotivoDecisao = motivo;
                    item.DecididoPorId = decisorId;
                    item.DecididoEm = DateTime.UtcNow;

                    if (estoura)
                        await _logs.Registrar(
                            Erp.Model.Log.TipoAcao.Aprovacao, Modulo,
                            $"Item #{item.Id} aprovado ACIMA do orçamento de {item.CentroCusto?.Nome}.",
                            decisorId,
                            entidade: nameof(SolicitacaoCompra),
                            entidadeId: item.SolicitacaoId.ToString());
                }
                else
                {
                    // Aprovado neste degrau, mas o valor passa do teto: sobe.
                    item.NivelAtual += 1;
                    promovido = true;
                }
            }
            else
            {
                item.Status = decisao;
                item.MotivoDecisao = motivo;
                item.DecididoPorId = decisorId;
                item.DecididoEm = DateTime.UtcNow;
            }

            ConsolidarCabecalho(item.Solicitacao);

            await contexto.SaveChangesAsync();

            var acao = decisao switch
            {
                StatusItem.Aprovado => Erp.Model.Log.TipoAcao.Aprovacao,
                StatusItem.Recusado => Erp.Model.Log.TipoAcao.Recusa,
                _ => Erp.Model.Log.TipoAcao.Devolucao,
            };

            var porQuem = degrau.PorSubstituto ? $" (em nome de {degrau.TitularNome})" : "";

            await _logs.Registrar(
                acao, Modulo,
                $"Item #{item.Id} da solicitação #{item.SolicitacaoId}, nível {degrau.Nivel}: {decisao}{porQuem}."
                + (promovido ? $" Valor de {item.ValorEstimado:C2} passa do teto do degrau — segue para o nível {item.NivelAtual}." : "")
                + (string.IsNullOrWhiteSpace(motivo) ? "" : $" Motivo: {motivo}"),
                decisorId,
                entidade: nameof(SolicitacaoCompra),
                entidadeId: item.SolicitacaoId.ToString());

            if (promovido)
            {
                await AvisarAprovadorDoItem(item, item.NivelAtual);
                return;
            }

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

        // ------------------------------------------------------------------

        /// <summary>Avisa, uma vez cada, quem responde pelo nível informado nos
        /// centros de custo dos itens da solicitação.</summary>
        private async Task AvisarAprovadoresDoNivel(SolicitacaoCompra solicitacao, int nivel)
        {
            var destinatarios = new HashSet<Guid>();

            foreach (var centroId in solicitacao.Itens.Select(i => i.CentroCustoId).Distinct())
            {
                var degrau = await _alcadas.DegrauDe(centroId, nivel);

                if (degrau is not null)
                    destinatarios.Add(degrau.AprovadorId);
            }

            if (destinatarios.Count == 0)
                return;

            await _avisos.CriarParaVarios(
                destinatarios.ToList(),
                "Solicitação aguardando sua aprovação",
                $"A solicitação #{solicitacao.Id} tem itens que você decide.",
                "circle-check",
                "/home/aprovacoes");
        }

        private async Task AvisarAprovadorDoItem(ItemSolicitacao item, int nivel)
        {
            var degrau = await _alcadas.DegrauDe(item.CentroCustoId, nivel);

            if (degrau is null)
                return;

            await _avisos.Criar(
                degrau.AprovadorId,
                "Item aguardando sua aprovação",
                $"Solicitação #{item.SolicitacaoId}: {item.Produto?.Descricao} "
                + $"({item.ValorEstimado:C2}) subiu para o seu nível.",
                "circle-check",
                "/home/aprovacoes");
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
