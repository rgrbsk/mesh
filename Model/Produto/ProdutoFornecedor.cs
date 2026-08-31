namespace Erp.Model.Produto
{
    /// <summary>
    /// De-para entre o código do produto NO FORNECEDOR e o produto do nosso
    /// catálogo.
    ///
    /// Existe porque o <c>cProd</c> da NF-e é o código interno de quem emitiu:
    /// o "B17025056" da Plotag não tem relação nenhuma com o nosso código. Sem
    /// esta tabela, todo recebimento exigiria amarrar item a item de novo, e o
    /// confronto acusaria "fora da compra" no que na verdade é o produto certo
    /// com nome alheio.
    ///
    /// A chave é o PAR (fornecedor, código): o mesmo código pode significar
    /// coisas diferentes em fornecedores diferentes.
    /// </summary>
    public class ProdutoFornecedor
    {
        public int Id { get; set; }

        public int FornecedorId { get; set; }

        public Erp.Model.Pessoa.Pessoa? Fornecedor { get; set; }

        /// <summary>O cProd como vem no XML dele.</summary>
        public string CodigoFornecedor { get; set; } = string.Empty;

        public int ProdutoId { get; set; }

        public Produto? Produto { get; set; }

        /// <summary>
        /// Descrição como o fornecedor chama. Guardada só para quem confere
        /// reconhecer a linha na tela — a amarração é pelo código.
        /// </summary>
        public string DescricaoFornecedor { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;
    }
}
