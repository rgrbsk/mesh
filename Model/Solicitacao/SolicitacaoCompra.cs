namespace Erp.Model.Solicitacao
{
    /// <summary>
    /// Pedido interno de compra: um cabeçalho e N itens. O que dá destino a cada
    /// item é o centro de custo dele — por isso centro de custo mora no item, e
    /// não aqui.
    /// </summary>
    public class SolicitacaoCompra
    {
        public int Id { get; set; }

        public Guid SolicitanteId { get; set; }

        public Erp.Model.Usuario.Usuario? Solicitante { get; set; }

        public StatusSolicitacao Status { get; set; } = StatusSolicitacao.Rascunho;

        /// <summary>
        /// Em que coluna do quadro ela está, antes da aprovação. Nulo significa
        /// "na primeira etapa" — assim solicitação criada antes de existirem
        /// etapas não some do Kanban.
        /// </summary>
        public int? EtapaId { get; set; }

        public Erp.Model.Etapa.Etapa? Etapa { get; set; }

        /// <summary>Contexto do pedido inteiro. A justificativa de cada item vai
        /// no item.</summary>
        public string Observacao { get; set; } = string.Empty;

        /// <summary>Quando saiu do rascunho. Nulo enquanto não foi enviada.</summary>
        public DateTime? EnviadaEm { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public List<ItemSolicitacao> Itens { get; set; } = new();

        /// <summary>Rascunho é o único estado que ainda se edita.</summary>
        public bool Editavel => Status is StatusSolicitacao.Rascunho or StatusSolicitacao.Devolvida;

        public decimal QuantidadeTotal => Itens.Sum(i => i.Quantidade);

        /// <summary>O prazo do pedido é o do item mais urgente: é a data em que
        /// alguém começa a ficar sem material.</summary>
        public DateTime? PrazoMaisCurto => Itens
            .Where(i => i.PrazoDesejado is not null)
            .Min(i => i.PrazoDesejado);
    }
}
