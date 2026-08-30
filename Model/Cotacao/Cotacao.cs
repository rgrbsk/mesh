namespace Erp.Model.Cotacao
{
    /// <summary>
    /// Uma rodada de cotação: os itens que se quer comprar e os fornecedores
    /// convidados a precificá-los. Cada fornecedor responde pelo seu próprio
    /// link e nunca enxerga a proposta dos outros.
    /// </summary>
    public class Cotacao
    {
        public int Id { get; set; }

        /// <summary>Como o comprador chama esta rodada.</summary>
        public string Titulo { get; set; } = string.Empty;

        public string Observacao { get; set; } = string.Empty;

        public StatusCotacao Status { get; set; } = StatusCotacao.Rascunho;

        /// <summary>Até quando o fornecedor pode responder. Depois disso o link
        /// para de aceitar envio.</summary>
        public DateTime? PrazoResposta { get; set; }

        public Guid? CriadoPorId { get; set; }

        public Erp.Model.Usuario.Usuario? CriadoPor { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public List<CotacaoItem> Itens { get; set; } = new();

        public List<ConviteFornecedor> Convites { get; set; } = new();

        /// <summary>Rascunho ainda se monta; aberta já foi para a rua.</summary>
        public bool Editavel => Status == StatusCotacao.Rascunho;

        public int Respostas => Convites.Count(c => c.Status == StatusConvite.Respondido);
    }
}
