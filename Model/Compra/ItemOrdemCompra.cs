namespace Erp.Model.Compra
{
    /// <summary>
    /// Uma linha do pedido. Guarda de onde veio (o item da cotação vencedor)
    /// porque é essa amarração que liga o pedido à solicitação que o originou —
    /// e, por ela, ao centro de custo que responde pela despesa.
    /// </summary>
    public class ItemOrdemCompra
    {
        public int Id { get; set; }

        public int OrdemCompraId { get; set; }

        public OrdemCompra? OrdemCompra { get; set; }

        public int ProdutoId { get; set; }

        public Erp.Model.Produto.Produto? Produto { get; set; }

        /// <summary>Item da cotação que originou esta linha. Nulo quando o
        /// comprador acrescentou um item à mão, fora de cotação.</summary>
        public int? CotacaoItemId { get; set; }

        public Erp.Model.Cotacao.CotacaoItem? CotacaoItem { get; set; }

        public decimal Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }

        /// <summary>
        /// Quanto já entrou, somando todos os recebimentos. Fica gravado em vez
        /// de ser somado na leitura: é consultado em toda listagem de pendências
        /// e recalcular a soma a cada linha custaria uma consulta por item.
        /// </summary>
        public decimal QuantidadeRecebida { get; set; }

        public DateTime? PrazoEntrega { get; set; }

        public string Observacao { get; set; } = string.Empty;

        public decimal Total => Quantidade * PrecoUnitario;

        /// <summary>O que ainda falta entrar. Nunca negativo: receber a mais é
        /// divergência, não saldo ao contrário.</summary>
        public decimal Pendente => Math.Max(0m, Quantidade - QuantidadeRecebida);

        public bool Atendido => QuantidadeRecebida >= Quantidade;
    }
}
