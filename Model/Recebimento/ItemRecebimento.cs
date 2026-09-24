namespace Erp.Model.Recebimento
{
    /// <summary>
    /// Quanto de UM item do pedido entrou NESTE recebimento. A soma das linhas
    /// de todos os recebimentos é o que alimenta a quantidade recebida da ordem
    /// de compra.
    /// </summary>
    public class ItemRecebimento
    {
        public int Id { get; set; }

        public int RecebimentoId { get; set; }

        public Recebimento? Recebimento { get; set; }

        public int ItemOrdemCompraId { get; set; }

        public Erp.Model.Compra.ItemOrdemCompra? ItemOrdemCompra { get; set; }

        /// <summary>O que entrou agora, não o acumulado.</summary>
        public decimal Quantidade { get; set; }

        /// <summary>
        /// Marca a mercadoria que chegou com problema. Ela entra no saldo do
        /// mesmo jeito — negar a entrada esconderia que ela está no depósito —
        /// mas fica sinalizada para quem for pagar.
        /// </summary>
        public bool ComAvaria { get; set; }

        public string Observacao { get; set; } = string.Empty;
    }
}
