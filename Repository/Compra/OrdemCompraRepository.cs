using Erp.Data;
using Erp.Model.Compra;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Compra
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// A ordem de compra é o documento que faltava entre a escolha do vencedor
    /// e a chegada da mercadoria. É contra ela que o recebimento dá entrada e
    /// que a nota fiscal é conferida.
    /// </summary>
    public class OrdemCompraRepository
    {
        private const string Modulo = "Ordens de compra";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly Erp.Repository.Notificacao.NotificacaoRepository _avisos;

        public OrdemCompraRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            Erp.Repository.Notificacao.NotificacaoRepository avisos)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
        }

        public async Task<List<OrdemCompra>> Buscar(Expression<Func<OrdemCompra, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.OrdensCompra
                .AsNoTracking()
                .Include(o => o.Fornecedor)
                .Include(o => o.CriadoPor)
                .Include(o => o.Itens).ThenInclude(i => i.Produto)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderByDescending(o => o.Id).ToListAsync();
        }

        public async Task<OrdemCompra?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.OrdensCompra
                .AsNoTracking()
                .Include(o => o.Fornecedor)
                .Include(o => o.Cotacao)
                .Include(o => o.CriadoPor)
                .Include(o => o.Itens).ThenInclude(i => i.Produto)
                .Include(o => o.Itens).ThenInclude(i => i.CotacaoItem)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>Pedidos que ainda têm saldo a receber — é o que alimenta a
        /// tela de recebimento.</summary>
        public async Task<List<OrdemCompra>> PendentesDeRecebimento()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.OrdensCompra
                .AsNoTracking()
                .Include(o => o.Fornecedor)
                .Include(o => o.Itens).ThenInclude(i => i.Produto)
                .Where(o => o.Status == StatusOrdemCompra.Enviada
                         || o.Status == StatusOrdemCompra.ParcialmenteRecebida)
                .OrderBy(o => o.PrazoEntrega ?? o.EmitidaEm)
                .ToListAsync();
        }

        /// <summary>
        /// Gera as ordens de compra de uma cotação encerrada: UMA POR
        /// FORNECEDOR vencedor.
        ///
        /// A divisão por fornecedor não é detalhe de organização — é o que
        /// torna o pedido enviável. Um documento que misturasse dois
        /// fornecedores não poderia ser mandado para nenhum dos dois.
        ///
        /// Idempotente: item já incluído numa ordem não entra de novo, então
        /// rodar duas vezes não duplica a compra.
        /// </summary>
        public async Task<List<OrdemCompra>> GerarDeCotacao(int cotacaoId, Guid criadoPorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var cotacao = await contexto.Cotacoes
                .Include(c => c.Itens).ThenInclude(i => i.Produto)
                .Include(c => c.Itens).ThenInclude(i => i.Propostas)
                .Include(c => c.Convites).ThenInclude(c => c.Pessoa)
                .FirstOrDefaultAsync(c => c.Id == cotacaoId)
                ?? throw new InvalidOperationException("Cotação não encontrada.");

            var jaGerados = await contexto.ItensOrdemCompra
                .Where(i => i.CotacaoItemId != null
                         && i.CotacaoItem!.CotacaoId == cotacaoId
                         && i.OrdemCompra!.Status != StatusOrdemCompra.Cancelada)
                .Select(i => i.CotacaoItemId!.Value)
                .ToListAsync();

            var vencedores = cotacao.Itens
                .Where(i => i.ConviteVencedorId is not null && !jaGerados.Contains(i.Id))
                .ToList();

            if (vencedores.Count == 0)
                throw new InvalidOperationException(
                    "Não há item com vencedor pendente de pedido nesta cotação.");

            var geradas = new List<OrdemCompra>();

            foreach (var grupo in vencedores.GroupBy(i => i.ConviteVencedorId!.Value))
            {
                var convite = cotacao.Convites.First(c => c.Id == grupo.Key);

                // O convite pode ter sido para um e-mail solto, sem cadastro.
                // Nesse caso não há a quem emitir o pedido: o comprador precisa
                // cadastrar o fornecedor antes.
                if (convite.PessoaId is null)
                    throw new InvalidOperationException(
                        $"O fornecedor \"{convite.Identificacao}\" venceu itens mas não está "
                        + "cadastrado. Cadastre-o antes de gerar o pedido.");

                var ordem = new OrdemCompra
                {
                    Numero = await ProximoNumero(contexto),
                    FornecedorId = convite.PessoaId.Value,
                    CotacaoId = cotacaoId,
                    Status = StatusOrdemCompra.Rascunho,
                    CondicaoPagamento = convite.CondicaoPagamento,
                    Frete = convite.Frete,
                    CriadoPorId = criadoPorId,
                    EmitidaEm = DateTime.UtcNow,
                    CriadoEm = DateTime.UtcNow,
                    ModificadoEm = DateTime.UtcNow,
                };

                foreach (var item in grupo)
                {
                    var proposta = item.Propostas
                        .FirstOrDefault(p => p.ConviteFornecedorId == grupo.Key);

                    // Vencedor sem preço não deveria existir — EscolherVencedor
                    // recusa —, mas se existir é melhor falhar aqui do que
                    // emitir um pedido a preço zero.
                    if (proposta?.PrecoUnitario is null)
                        throw new InvalidOperationException(
                            $"O item \"{item.Produto?.Descricao}\" não tem preço na proposta vencedora.");

                    var prazo = proposta.PrazoEntregaDias is > 0
                        ? DateTime.UtcNow.AddDays(proposta.PrazoEntregaDias.Value)
                        : (DateTime?)null;

                    ordem.Itens.Add(new ItemOrdemCompra
                    {
                        ProdutoId = item.ProdutoId,
                        CotacaoItemId = item.Id,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = proposta.PrecoUnitario.Value,
                        PrazoEntrega = prazo,
                        Observacao = proposta.Observacao,
                    });

                    if (prazo is not null && (ordem.PrazoEntrega is null || prazo > ordem.PrazoEntrega))
                        ordem.PrazoEntrega = prazo;
                }

                contexto.OrdensCompra.Add(ordem);
                geradas.Add(ordem);
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Criacao, Modulo,
                $"Cotação #{cotacaoId} gerou {geradas.Count} "
                + (geradas.Count == 1 ? "ordem de compra." : "ordens de compra.")
                + $" Total: {geradas.Sum(o => o.ValorTotal):C2}.",
                criadoPorId,
                entidade: nameof(OrdemCompra),
                entidadeId: cotacaoId.ToString());

            return geradas;
        }

        public async Task<OrdemCompra> Atualizar(OrdemCompra ordem)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var gravada = await contexto.OrdensCompra
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == ordem.Id)
                ?? throw new InvalidOperationException("Ordem de compra não encontrada.");

            if (!gravada.Editavel)
                throw new InvalidOperationException(
                    "Pedido já enviado não se edita — o fornecedor já recebeu esta versão.");

            gravada.FornecedorId = ordem.FornecedorId;
            gravada.PrazoEntrega = ordem.PrazoEntrega;
            gravada.CondicaoPagamento = ordem.CondicaoPagamento;
            gravada.Frete = ordem.Frete;
            gravada.Observacao = ordem.Observacao;
            gravada.ModificadoEm = DateTime.UtcNow;

            var idsNaTela = ordem.Itens.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

            foreach (var removido in gravada.Itens.Where(i => !idsNaTela.Contains(i.Id)).ToList())
                contexto.ItensOrdemCompra.Remove(removido);

            foreach (var item in ordem.Itens)
            {
                var alvo = item.Id == 0 ? null : gravada.Itens.FirstOrDefault(i => i.Id == item.Id);

                if (alvo is null)
                {
                    gravada.Itens.Add(new ItemOrdemCompra
                    {
                        ProdutoId = item.ProdutoId,
                        CotacaoItemId = item.CotacaoItemId,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario,
                        PrazoEntrega = item.PrazoEntrega,
                        Observacao = item.Observacao,
                    });
                    continue;
                }

                alvo.ProdutoId = item.ProdutoId;
                alvo.Quantidade = item.Quantidade;
                alvo.PrecoUnitario = item.PrecoUnitario;
                alvo.PrazoEntrega = item.PrazoEntrega;
                alvo.Observacao = item.Observacao;
            }

            await contexto.SaveChangesAsync();

            return gravada;
        }

        /// <summary>
        /// Envia o pedido ao fornecedor. A partir daqui ele congela — e o
        /// preço de referência de cada produto é atualizado, para que a próxima
        /// solicitação seja avaliada contra o orçamento pelo preço que a
        /// empresa está pagando hoje.
        /// </summary>
        public async Task Enviar(int id, Guid autorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var ordem = await contexto.OrdensCompra
                .Include(o => o.Itens)
                .Include(o => o.Fornecedor)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new InvalidOperationException("Ordem de compra não encontrada.");

            if (ordem.Status != StatusOrdemCompra.Rascunho)
                throw new InvalidOperationException("Este pedido já foi enviado.");

            if (ordem.Itens.Count == 0)
                throw new InvalidOperationException("Um pedido sem itens não tem o que enviar.");

            ordem.Status = StatusOrdemCompra.Enviada;
            ordem.EnviadaEm = DateTime.UtcNow;
            ordem.ModificadoEm = ordem.EnviadaEm.Value;

            var produtoIds = ordem.Itens.Select(i => i.ProdutoId).Distinct().ToList();

            var produtos = await contexto.Produtos
                .Where(p => produtoIds.Contains(p.Id))
                .ToListAsync();

            foreach (var produto in produtos)
            {
                // Vários itens do mesmo produto no pedido: vale o maior preço,
                // que é o mais conservador para estimar orçamento.
                var preco = ordem.Itens
                    .Where(i => i.ProdutoId == produto.Id)
                    .Max(i => i.PrecoUnitario);

                produto.PrecoReferencia = preco;
                produto.PrecoReferenciaEm = DateTime.UtcNow;
                produto.ModificadoEm = DateTime.UtcNow;
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, Modulo,
                $"Pedido {ordem.Numero} enviado a {ordem.Fornecedor?.RazaoSocial} "
                + $"no valor de {ordem.ValorTotal:C2}.",
                autorId,
                entidade: nameof(OrdemCompra),
                entidadeId: id.ToString());
        }

        public async Task Cancelar(int id, string motivo, Guid autorId)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException(
                    "Cancelar exige motivo — sem ele o histórico não explica o que houve.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var ordem = await contexto.OrdensCompra
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new InvalidOperationException("Ordem de compra não encontrada.");

            // Cancelar o que já entrou no estoque deixaria mercadoria sem
            // documento de origem. Nesse caso o caminho é devolução, não
            // cancelamento.
            if (ordem.Itens.Any(i => i.QuantidadeRecebida > 0))
                throw new InvalidOperationException(
                    "Este pedido já teve recebimento. Cancelar deixaria mercadoria sem origem.");

            ordem.Status = StatusOrdemCompra.Cancelada;
            ordem.MotivoCancelamento = motivo;
            ordem.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Pedido {ordem.Numero} cancelado. Motivo: {motivo}",
                autorId,
                entidade: nameof(OrdemCompra),
                entidadeId: id.ToString());
        }

        /// <summary>Só rascunho se exclui: o que já foi enviado é compromisso
        /// assumido com o fornecedor.</summary>
        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var ordem = await contexto.OrdensCompra.FirstOrDefaultAsync(o => o.Id == id);

            if (ordem is null)
                return false;

            if (ordem.Status != StatusOrdemCompra.Rascunho)
                throw new InvalidOperationException("Só rascunhos podem ser excluídos.");

            contexto.OrdensCompra.Remove(ordem);
            await contexto.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Próximo número do ano, no formato OC-AAAA-0000.
        ///
        /// Dois compradores gerando pedidos no mesmo instante poderiam receber
        /// o mesmo número; o índice único da coluna recusa o segundo, que
        /// tenta de novo. Para o volume de uma empresa média isso nunca
        /// acontece, e a alternativa — uma sequência no banco — amarraria o
        /// código ao PostgreSQL.
        /// </summary>
        private static async Task<string> ProximoNumero(AppDbContext contexto)
        {
            var ano = DateTime.UtcNow.Year;
            var prefixo = $"OC-{ano}-";

            var ultimo = await contexto.OrdensCompra
                .Where(o => o.Numero.StartsWith(prefixo))
                .OrderByDescending(o => o.Numero)
                .Select(o => o.Numero)
                .FirstOrDefaultAsync();

            var sequencial = 1;

            if (ultimo is not null && int.TryParse(ultimo[prefixo.Length..], out var n))
                sequencial = n + 1;

            return $"{prefixo}{sequencial:0000}";
        }
    }
}
