namespace Erp.Model.Cotacao
{
    /// <summary>O preço que UM fornecedor deu para UM item.</summary>
    public class PropostaItem
    {
        public int Id { get; set; }

        public int ConviteFornecedorId { get; set; }

        public ConviteFornecedor? ConviteFornecedor { get; set; }

        public int CotacaoItemId { get; set; }

        public CotacaoItem? CotacaoItem { get; set; }

        /// <summary>Nulo significa "não cotado": o fornecedor não trabalha com
        /// este item. Diferente de zero, que seria brinde.</summary>
        public decimal? PrecoUnitario { get; set; }

        public int? PrazoEntregaDias { get; set; }

        public string Observacao { get; set; } = string.Empty;

        public decimal? Total => PrecoUnitario is null || CotacaoItem is null
            ? null
            : PrecoUnitario * CotacaoItem.Quantidade;
    }
}
