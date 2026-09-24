using Erp.Data;
using Erp.Model.Compra;
using Erp.Model.Recebimento;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Recebimento
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// O recebimento é a segunda ponta do three-way match. Registrá-lo separado
    /// da nota é o que permite ao sistema saber que se está cobrando mercadoria
    /// que não chegou.
    /// </summary>
    public class RecebimentoRepository
    {
        private const string Modulo = "Recebimento";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;

        public RecebimentoRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs)
        {
            _fabrica = fabrica;
            _logs = logs;
        }

        public async Task<List<Model.Recebimento.Recebimento>> Buscar(int? ordemCompraId = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Recebimentos
                .AsNoTracking()
                .Include(r => r.OrdemCompra).ThenInclude(o => o!.Fornecedor)
                .Include(r => r.Itens).ThenInclude(i => i.ItemOrdemCompra).ThenInclude(i => i!.Produto)
                .AsQueryable();

            if (ordemCompraId is not null)
                query = query.Where(r => r.OrdemCompraId == ordemCompraId);

            return await query.OrderByDescending(r => r.Id).ToListAsync();
        }

        public async Task<Model.Recebimento.Recebimento?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Recebimentos
                .AsNoTracking()
                .Include(r => r.OrdemCompra).ThenInclude(o => o!.Fornecedor)
                .Include(r => r.RecebidoPor)
                .Include(r => r.Itens).ThenInclude(i => i.ItemOrdemCompra).ThenInclude(i => i!.Produto)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Dá entrada na mercadoria que chegou.
        ///
        /// Aceita entrega parcial — é o caso comum — e recusa receber mais do
        /// que foi pedido: excesso não é entrada, é divergência a resolver com
        /// o fornecedor antes de entrar no estoque.
        /// </summary>
        public async Task<Model.Recebimento.Recebimento> Registrar(
            Model.Recebimento.Recebimento recebimento, Guid autorId, string autorNome)
        {
            if (recebimento.Itens.Count == 0)
                throw new InvalidOperationException("Um recebimento sem itens não registra nada.");

            if (recebimento.Itens.All(i => i.Quantidade <= 0))
                throw new InvalidOperationException("Informe a quantidade recebida de ao menos um item.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var ordem = await contexto.OrdensCompra
                .Include(o => o.Itens).ThenInclude(i => i.Produto)
                .Include(o => o.Fornecedor)
                .FirstOrDefaultAsync(o => o.Id == recebimento.OrdemCompraId)
                ?? throw new InvalidOperationException("Ordem de compra não encontrada.");

            if (!ordem.AceitaRecebimento)
                throw new InvalidOperationException(
                    ordem.Status == StatusOrdemCompra.Rascunho
                        ? "Este pedido ainda não foi enviado ao fornecedor."
                        : "Este pedido não está aberto para recebimento.");

            var novo = new Model.Recebimento.Recebimento
            {
                Numero = await ProximoNumero(contexto),
                OrdemCompraId = ordem.Id,
                RecebidoEm = DateTime.UtcNow,
                RecebidoPorId = autorId,
                RecebidoPorNome = autorNome,
                Observacao = recebimento.Observacao,
            };

            foreach (var linha in recebimento.Itens.Where(i => i.Quantidade > 0))
            {
                var item = ordem.Itens.FirstOrDefault(i => i.Id == linha.ItemOrdemCompraId)
                    ?? throw new InvalidOperationException("Item informado não pertence a este pedido.");

                if (linha.Quantidade > item.Pendente)
                    throw new InvalidOperationException(
                        $"\"{item.Produto?.Descricao}\": tentativa de receber {linha.Quantidade:N2} "
                        + $"com apenas {item.Pendente:N2} pendente no pedido.");

                item.QuantidadeRecebida += linha.Quantidade;

                novo.Itens.Add(new ItemRecebimento
                {
                    ItemOrdemCompraId = item.Id,
                    Quantidade = linha.Quantidade,
                    ComAvaria = linha.ComAvaria,
                    Observacao = linha.Observacao,
                });
            }

            // O estado do pedido é consequência do que entrou, nunca digitado.
            ordem.Status = ordem.TotalmenteAtendida
                ? StatusOrdemCompra.Recebida
                : StatusOrdemCompra.ParcialmenteRecebida;

            ordem.ModificadoEm = DateTime.UtcNow;

            contexto.Recebimentos.Add(novo);
            await contexto.SaveChangesAsync();

            var resumo = ordem.TotalmenteAtendida
                ? "Pedido totalmente atendido."
                : $"Pedido parcialmente atendido — {ordem.Itens.Count(i => !i.Atendido)} item(ns) com saldo.";

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Criacao, Modulo,
                $"Recebimento {novo.Numero} do pedido {ordem.Numero} "
                + $"({ordem.Fornecedor?.RazaoSocial}). {resumo}"
                + (novo.Itens.Any(i => i.ComAvaria) ? " Há item com avaria." : ""),
                autorId,
                entidade: nameof(Model.Recebimento.Recebimento),
                entidadeId: novo.Id.ToString());

            return novo;
        }

        /// <summary>
        /// Desfaz um recebimento inteiro — conferência lançada no pedido
        /// errado, ou mercadoria devolvida antes de entrar.
        ///
        /// Devolve o saldo ao pedido e reabre o estado dele. O registro não é
        /// apagado: some do saldo, permanece no histórico.
        /// </summary>
        public async Task Estornar(int id, string motivo, Guid autorId)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Estornar exige motivo.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var recebimento = await contexto.Recebimentos
                .Include(r => r.Itens)
                .Include(r => r.OrdemCompra).ThenInclude(o => o!.Itens)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Recebimento não encontrado.");

            var ordem = recebimento.OrdemCompra
                ?? throw new InvalidOperationException("Pedido do recebimento não encontrado.");

            var temNota = await contexto.NotasFiscais
                .AnyAsync(n => n.OrdemCompraId == ordem.Id && n.LiberadaParaPagamento);

            if (temNota)
                throw new InvalidOperationException(
                    "Este pedido já tem nota liberada para pagamento. Estornar deixaria "
                    + "o título sem lastro de entrada.");

            foreach (var linha in recebimento.Itens)
            {
                var item = ordem.Itens.FirstOrDefault(i => i.Id == linha.ItemOrdemCompraId);

                if (item is null)
                    continue;

                item.QuantidadeRecebida = Math.Max(0m, item.QuantidadeRecebida - linha.Quantidade);
            }

            ordem.Status = ordem.Itens.Any(i => i.QuantidadeRecebida > 0)
                ? StatusOrdemCompra.ParcialmenteRecebida
                : StatusOrdemCompra.Enviada;

            ordem.ModificadoEm = DateTime.UtcNow;

            contexto.Recebimentos.Remove(recebimento);
            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Exclusao, Modulo,
                $"Recebimento {recebimento.Numero} do pedido {ordem.Numero} estornado. Motivo: {motivo}",
                autorId,
                entidade: nameof(Model.Recebimento.Recebimento),
                entidadeId: id.ToString());
        }

        /// <summary>Itens de pedido que tiveram alguma entrada com avaria — o
        /// confronto usa para sinalizar a linha a quem for liberar o pagamento.</summary>
        public async Task<HashSet<int>> ItensComAvaria(int ordemCompraId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var ids = await contexto.ItensRecebimento
                .AsNoTracking()
                .Where(i => i.ComAvaria && i.Recebimento!.OrdemCompraId == ordemCompraId)
                .Select(i => i.ItemOrdemCompraId)
                .Distinct()
                .ToListAsync();

            return ids.ToHashSet();
        }

        private static async Task<string> ProximoNumero(AppDbContext contexto)
        {
            var ano = DateTime.UtcNow.Year;
            var prefixo = $"REC-{ano}-";

            var ultimo = await contexto.Recebimentos
                .Where(r => r.Numero.StartsWith(prefixo))
                .OrderByDescending(r => r.Numero)
                .Select(r => r.Numero)
                .FirstOrDefaultAsync();

            var sequencial = 1;

            if (ultimo is not null && int.TryParse(ultimo[prefixo.Length..], out var n))
                sequencial = n + 1;

            return $"{prefixo}{sequencial:0000}";
        }
    }
}
