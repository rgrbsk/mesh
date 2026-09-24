namespace Erp.Model.Solicitacao
{
    /// <summary>
    /// Uma linha do pedido. Carrega o centro de custo porque é ele que define
    /// QUEM aprova — sem centro de custo o item não tem para onde ir.
    /// </summary>
    public class ItemSolicitacao
    {
        public int Id { get; set; }

        public int SolicitacaoId { get; set; }

        public SolicitacaoCompra? Solicitacao { get; set; }

        public int ProdutoId { get; set; }

        public Erp.Model.Produto.Produto? Produto { get; set; }

        public decimal Quantidade { get; set; }

        public int CentroCustoId { get; set; }

        public Erp.Model.CentroCusto.CentroCusto? CentroCusto { get; set; }

        /// <summary>Data em que o material precisa estar disponível.</summary>
        public DateTime? PrazoDesejado { get; set; }

        public string Justificativa { get; set; } = string.Empty;

        public StatusItem Status { get; set; } = StatusItem.Pendente;

        /// <summary>Obrigatório ao recusar ou devolver — recusa sem motivo não
        /// diz ao solicitante o que corrigir.</summary>
        public string? MotivoDecisao { get; set; }

        public Guid? DecididoPorId { get; set; }

        public Erp.Model.Usuario.Usuario? DecididoPor { get; set; }

        public DateTime? DecididoEm { get; set; }

        // ---- Cadeia de aprovação ----

        /// <summary>
        /// Degrau em que o item está. Começa em 1 e sobe a cada aprovação que
        /// não encerra a alçada. Item em nível 2 continua Pendente: a decisão
        /// do primeiro aprovador foi favorável, mas não bastou.
        /// </summary>
        public int NivelAtual { get; set; } = 1;

        /// <summary>
        /// Valor do item no momento do envio, calculado pelo preço de
        /// referência do produto. É por ele que a alçada é resolvida e que o
        /// impacto no orçamento é estimado.
        ///
        /// Fica gravado em vez de recalculado: mudar o preço de referência do
        /// produto não pode reabrir a cadeia de um item que já está em decisão.
        /// </summary>
        public decimal ValorEstimado { get; set; }

        public List<Erp.Model.Aprovacao.AprovacaoItem> Aprovacoes { get; set; } = new();

        public decimal PrecoEstimadoUnitario => Quantidade == 0
            ? 0m
            : ValorEstimado / Quantidade;
    }
}
