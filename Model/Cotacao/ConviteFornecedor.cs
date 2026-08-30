namespace Erp.Model.Cotacao
{
    /// <summary>
    /// O convite de um fornecedor a uma cotação — e o link por onde ele responde.
    ///
    /// O fornecedor não tem login: o que identifica a sessão dele é o TOKEN, que
    /// vale só para este convite. Por isso ele mesmo declara CNPJ, razão social e
    /// responsável ao responder: o cadastro pode nem existir ainda, e mesmo
    /// quando existe é ele quem sabe se algum dado mudou.
    /// </summary>
    public class ConviteFornecedor
    {
        public int Id { get; set; }

        public int CotacaoId { get; set; }

        public Cotacao? Cotacao { get; set; }

        /// <summary>Fornecedor já cadastrado, quando o comprador escolheu um da
        /// lista. Nulo quando o convite foi para um e-mail solto.</summary>
        public int? PessoaId { get; set; }

        public Erp.Model.Pessoa.Pessoa? Pessoa { get; set; }

        /// <summary>Para onde o link é enviado.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Segredo do link, único. É a credencial do fornecedor: quem tem o
        /// token responde, quem não tem não enxerga nada.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Depois disso o link não aceita mais envio, mesmo que a
        /// cotação siga aberta.</summary>
        public DateTime ExpiraEm { get; set; }

        public StatusConvite Status { get; set; } = StatusConvite.Pendente;

        public DateTime? RespondidoEm { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // ---- Declarado pelo fornecedor na tela pública ----

        public string Cnpj { get; set; } = string.Empty;

        public string RazaoSocial { get; set; } = string.Empty;

        /// <summary>Quem respondeu, do lado do fornecedor.</summary>
        public string Responsavel { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public string CondicaoPagamento { get; set; } = string.Empty;

        public string Frete { get; set; } = string.Empty;

        public List<PropostaItem> Propostas { get; set; } = new();

        /// <summary>Nome para o comprador ver na lista: o que o fornecedor
        /// declarou, ou o cadastro, ou o e-mail — nessa ordem.</summary>
        public string Identificacao =>
            !string.IsNullOrWhiteSpace(RazaoSocial) ? RazaoSocial
            : Pessoa?.RazaoSocial ?? Email;
    }
}
