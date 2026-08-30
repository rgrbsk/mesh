namespace Erp.Model.Cotacao
{
    /// <summary>
    /// Um produto a ser precificado. Guarda de onde veio (o item da solicitação
    /// aprovada) para o mapa comparativo saber a qual pedido a compra atende.
    /// </summary>
    public class CotacaoItem
    {
        public int Id { get; set; }

        public int CotacaoId { get; set; }

        public Cotacao? Cotacao { get; set; }

        public int ProdutoId { get; set; }

        public Erp.Model.Produto.Produto? Produto { get; set; }

        public decimal Quantidade { get; set; }

        /// <summary>Nulo quando o comprador cotou um item avulso, fora de uma
        /// solicitação.</summary>
        public int? ItemSolicitacaoId { get; set; }

        public Erp.Model.Solicitacao.ItemSolicitacao? ItemSolicitacao { get; set; }

        public List<PropostaItem> Propostas { get; set; } = new();

        // ---- Escolha do vencedor, feita no mapa comparativo ----

        /// <summary>Convite que ganhou ESTE item. A escolha é por item: o mesmo
        /// pedido pode terminar dividido entre dois fornecedores.</summary>
        public int? ConviteVencedorId { get; set; }

        public ConviteFornecedor? ConviteVencedor { get; set; }

        /// <summary>Obrigatório quando o escolhido não é o menor preço. É o que
        /// justifica a decisão para quem auditar depois.</summary>
        public string? MotivoEscolha { get; set; }

        public Guid? EscolhidoPorId { get; set; }

        public Erp.Model.Usuario.Usuario? EscolhidoPor { get; set; }

        public DateTime? EscolhidoEm { get; set; }
    }
}
