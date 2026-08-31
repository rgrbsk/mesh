namespace Erp.Model.Fiscal
{
    /// <summary>
    /// Uma linha &lt;det&gt; da NF-e, como veio no arquivo. Nada aqui é
    /// convertido nem corrigido: é o que o fornecedor declarou, e é contra isto
    /// que o confronto compara.
    /// </summary>
    public class NotaFiscalItem
    {
        public int Id { get; set; }

        public int NotaFiscalId { get; set; }

        public NotaFiscal? NotaFiscal { get; set; }

        /// <summary>Número da linha na nota (nItem). O mesmo produto pode
        /// aparecer em várias.</summary>
        public int Numero { get; set; }

        /// <summary>Código do produto NO FORNECEDOR (cProd). Raramente é igual
        /// ao nosso — é por isso que existe a amarração manual.</summary>
        public string CodigoFornecedor { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Ncm { get; set; } = string.Empty;

        public string Cfop { get; set; } = string.Empty;

        public string Unidade { get; set; } = string.Empty;

        public decimal Quantidade { get; set; }

        public decimal ValorUnitario { get; set; }

        public decimal ValorTotal { get; set; }

        /// <summary>Produto do nosso catálogo a que esta linha corresponde.
        /// Nulo enquanto ninguém amarrou.</summary>
        public int? ProdutoId { get; set; }

        public Erp.Model.Produto.Produto? Produto { get; set; }
    }
}
