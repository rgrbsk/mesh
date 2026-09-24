namespace Erp.Model.Recebimento
{
    /// <summary>
    /// A conferência física da mercadoria, registrada no momento em que ela
    /// chega — separada do documento fiscal de propósito.
    ///
    /// Tratar a nota como se fosse o recebimento esconde dois casos comuns: a
    /// mercadoria que chega antes da nota e a entrega parcial, em que parte do
    /// pedido vem numa remessa e parte em outra. Por isso o recebimento é um
    /// documento próprio, e uma ordem de compra pode ter vários.
    /// </summary>
    public class Recebimento
    {
        public int Id { get; set; }

        /// <summary>Número legível, no formato REC-AAAA-0000.</summary>
        public string Numero { get; set; } = string.Empty;

        public int OrdemCompraId { get; set; }

        public Erp.Model.Compra.OrdemCompra? OrdemCompra { get; set; }

        public DateTime RecebidoEm { get; set; } = DateTime.UtcNow;

        public Guid? RecebidoPorId { get; set; }

        public Erp.Model.Usuario.Usuario? RecebidoPor { get; set; }

        /// <summary>Instantâneo do nome de quem conferiu — o histórico do
        /// recebimento não pode virar anônimo se a conta for excluída.</summary>
        public string RecebidoPorNome { get; set; } = string.Empty;

        /// <summary>Nota de quem conferiu: avaria, volume faltando, divergência
        /// de embalagem. É o registro que sustenta uma reclamação depois.</summary>
        public string Observacao { get; set; } = string.Empty;

        public List<ItemRecebimento> Itens { get; set; } = new();

        public decimal QuantidadeTotal => Itens.Sum(i => i.Quantidade);

        public bool TemRessalva => Itens.Any(i => i.ComAvaria)
                                || !string.IsNullOrWhiteSpace(Observacao);
    }
}
