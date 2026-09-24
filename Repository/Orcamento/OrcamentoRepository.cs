using Erp.Data;
using Erp.Model.Orcamento;
using Erp.Model.Solicitacao;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Orcamento
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// Cuida da verba dos centros de custo e, sobretudo, do CONSUMO dela. O
    /// consumo nunca é gravado: ele é somado no momento da consulta, porque
    /// cancelar uma ordem de compra ou recusar uma solicitação tem que devolver
    /// a verba na hora. Um número congelado passaria a mentir no primeiro
    /// estorno.
    /// </summary>
    public class OrcamentoRepository
    {
        private const string Modulo = "Orçamento";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;

        public OrcamentoRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs)
        {
            _fabrica = fabrica;
            _logs = logs;
        }

        // ------------------------------------------------------------------
        // Cadastro da verba
        // ------------------------------------------------------------------

        public async Task<List<Model.Orcamento.Orcamento>> Buscar(int ano, int? centroCustoId = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Orcamentos
                .AsNoTracking()
                .Include(o => o.CentroCusto)
                .Where(o => o.Ano == ano);

            if (centroCustoId is not null)
                query = query.Where(o => o.CentroCustoId == centroCustoId);

            return await query
                .OrderBy(o => o.CentroCusto!.Codigo)
                .ThenBy(o => o.Mes)
                .ToListAsync();
        }

        /// <summary>
        /// Grava a verba de um centro para os doze meses de uma vez. Salvar mês
        /// a mês dava margem a um ano pela metade, que a tela de consumo
        /// exibiria como orçamento zero em vez de "não cadastrado".
        /// </summary>
        public async Task SalvarAno(int centroCustoId, int ano, decimal[] valoresPorMes, Guid autorId)
        {
            if (valoresPorMes.Length != 12)
                throw new InvalidOperationException("São esperados doze valores, um por mês.");

            if (valoresPorMes.Any(v => v < 0))
                throw new InvalidOperationException("Verba negativa não faz sentido.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var existentes = await contexto.Orcamentos
                .Where(o => o.CentroCustoId == centroCustoId && o.Ano == ano)
                .ToListAsync();

            for (var mes = 1; mes <= 12; mes++)
            {
                var valor = valoresPorMes[mes - 1];
                var atual = existentes.FirstOrDefault(o => o.Mes == mes);

                if (atual is null)
                {
                    contexto.Orcamentos.Add(new Model.Orcamento.Orcamento
                    {
                        CentroCustoId = centroCustoId,
                        Ano = ano,
                        Mes = mes,
                        Valor = valor,
                    });
                    continue;
                }

                atual.Valor = valor;
                atual.ModificadoEm = DateTime.UtcNow;
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Verba do centro #{centroCustoId} definida para {ano}: "
                + $"{valoresPorMes.Sum():C2} no ano.",
                autorId,
                entidade: nameof(Model.Orcamento.Orcamento),
                entidadeId: $"{centroCustoId}/{ano}");
        }

        // ------------------------------------------------------------------
        // Consumo
        // ------------------------------------------------------------------

        /// <summary>
        /// A situação de UM centro numa competência.
        ///
        /// Três números, e a relação entre eles importa: COMPROMETIDO é o que
        /// já virou pedido ao fornecedor; PREVISTO é o que foi aprovado e
        /// ainda não virou pedido, avaliado pelo preço de referência;
        /// REALIZADO é a parte do comprometido que já tem documento fiscal
        /// liberado — ou seja, está DENTRO do comprometido, e por isso não
        /// entra na soma do consumo.
        /// </summary>
        public async Task<ConsumoOrcamento> Consumo(int centroCustoId, int ano, int mes)
        {
            var todos = await ConsumoDeTodos(ano, mes, centroCustoId);

            return todos.FirstOrDefault()
                ?? new ConsumoOrcamento(centroCustoId, "", ano, mes, 0m, 0m, 0m, 0m);
        }

        /// <summary>Consumo de todos os centros ativos numa competência — é o
        /// que alimenta a tela de orçamento e o painel.</summary>
        public async Task<List<ConsumoOrcamento>> ConsumoDeTodos(int ano, int mes, int? apenasCentroId = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fim = inicio.AddMonths(1);

            var centros = await contexto.CentrosCusto
                .AsNoTracking()
                .Where(c => c.Ativo && (apenasCentroId == null || c.Id == apenasCentroId))
                .Select(c => new { c.Id, c.Codigo, c.Nome })
                .ToListAsync();

            var ids = centros.Select(c => c.Id).ToList();

            var verbas = await contexto.Orcamentos
                .AsNoTracking()
                .Where(o => o.Ano == ano && o.Mes == mes && ids.Contains(o.CentroCustoId))
                .ToDictionaryAsync(o => o.CentroCustoId, o => o.Valor);

            // ---- Previsto: aprovado, ainda sem pedido ----
            // O item aprovado que já entrou numa ordem de compra sai daqui e
            // passa a contar como comprometido — senão o mesmo gasto seria
            // somado duas vezes.
            var itensEmOrdem = await contexto.ItensOrdemCompra
                .AsNoTracking()
                .Where(i => i.CotacaoItemId != null)
                .Select(i => i.CotacaoItem!.ItemSolicitacaoId)
                .Where(id => id != null)
                .Select(id => id!.Value)
                .ToListAsync();

            var previstos = await contexto.ItensSolicitacao
                .AsNoTracking()
                .Where(i => i.Status == StatusItem.Aprovado
                         && ids.Contains(i.CentroCustoId)
                         && i.Solicitacao!.EnviadaEm >= inicio
                         && i.Solicitacao!.EnviadaEm < fim
                         && !itensEmOrdem.Contains(i.Id))
                .GroupBy(i => i.CentroCustoId)
                .Select(g => new { CentroId = g.Key, Valor = g.Sum(i => i.ValorEstimado) })
                .ToListAsync();

            // ---- Comprometido: ordem de compra emitida ----
            var itensOrdem = await contexto.ItensOrdemCompra
                .AsNoTracking()
                .Include(i => i.CotacaoItem)
                .Include(i => i.Produto)
                .Where(i => i.OrdemCompra!.Status != Erp.Model.Compra.StatusOrdemCompra.Cancelada
                         && i.OrdemCompra!.Status != Erp.Model.Compra.StatusOrdemCompra.Rascunho
                         && i.OrdemCompra!.EmitidaEm >= inicio
                         && i.OrdemCompra!.EmitidaEm < fim)
                .Select(i => new
                {
                    i.Id,
                    i.Quantidade,
                    i.PrecoUnitario,
                    ItemSolicitacaoId = i.CotacaoItem != null ? i.CotacaoItem.ItemSolicitacaoId : null,
                    CentroPadraoId = i.Produto != null ? i.Produto.CentroCustoPadraoId : null,
                    NotaLiberada = contexto.NotasFiscais.Any(n =>
                        n.OrdemCompraId == i.OrdemCompraId && n.LiberadaParaPagamento),
                })
                .ToListAsync();

            // O centro de um item de pedido vem da solicitação que o originou;
            // item avulso, sem cotação, cai no centro padrão do produto.
            var centrosPorItemSolicitacao = await contexto.ItensSolicitacao
                .AsNoTracking()
                .Select(i => new { i.Id, i.CentroCustoId })
                .ToDictionaryAsync(i => i.Id, i => i.CentroCustoId);

            var comprometido = new Dictionary<int, decimal>();
            var realizado = new Dictionary<int, decimal>();

            foreach (var item in itensOrdem)
            {
                int? centro = item.ItemSolicitacaoId is not null
                    && centrosPorItemSolicitacao.TryGetValue(item.ItemSolicitacaoId.Value, out var c)
                        ? c
                        : item.CentroPadraoId;

                if (centro is null || !ids.Contains(centro.Value))
                    continue;

                var valor = item.Quantidade * item.PrecoUnitario;

                comprometido[centro.Value] = comprometido.GetValueOrDefault(centro.Value) + valor;

                if (item.NotaLiberada)
                    realizado[centro.Value] = realizado.GetValueOrDefault(centro.Value) + valor;
            }

            return centros
                .Select(c => new ConsumoOrcamento(
                    c.Id,
                    $"{c.Codigo} — {c.Nome}",
                    ano,
                    mes,
                    verbas.GetValueOrDefault(c.Id),
                    previstos.FirstOrDefault(p => p.CentroId == c.Id)?.Valor ?? 0m,
                    comprometido.GetValueOrDefault(c.Id),
                    realizado.GetValueOrDefault(c.Id)))
                .OrderBy(c => c.CentroCustoNome)
                .ToList();
        }

        /// <summary>
        /// O que aconteceria com o orçamento se este valor fosse aprovado
        /// agora. É o que a tela de aprovação mostra antes de o aprovador
        /// clicar — e é o mesmo cálculo que o servidor refaz na hora de
        /// decidir, quando o centro bloqueia o estouro.
        /// </summary>
        public async Task<(ConsumoOrcamento Antes, decimal Depois, bool Estoura, bool Bloqueia)>
            Simular(int centroCustoId, decimal valor, DateTime? competencia = null)
        {
            var quando = competencia ?? DateTime.UtcNow;
            var antes = await Consumo(centroCustoId, quando.Year, quando.Month);

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var bloqueia = await contexto.CentrosCusto
                .AsNoTracking()
                .Where(c => c.Id == centroCustoId)
                .Select(c => c.BloqueiaAcimaOrcamento)
                .FirstOrDefaultAsync();

            var depois = antes.Consumido + valor;

            // Centro sem verba cadastrada não estoura: não há teto definido, e
            // travar por ausência de cadastro puniria quem ainda não configurou.
            var estoura = antes.Orcado > 0 && depois > antes.Orcado;

            return (antes, depois, estoura, bloqueia && estoura);
        }
    }
}
