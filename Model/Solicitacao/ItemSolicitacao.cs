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
    }
}
