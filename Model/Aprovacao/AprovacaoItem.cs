namespace Erp.Model.Aprovacao
{
    /// <summary>
    /// O registro de UM degrau percorrido por um item de solicitação.
    ///
    /// Existe porque a situação final do item ("Aprovado") não conta quem
    /// aprovou em cada nível. Num item que passou por gerente e diretor, é esta
    /// tabela que responde quem decidiu o quê, quando e com qual alçada — e é
    /// dela que sai o rastro de aprovação exigido por qualquer auditoria.
    /// </summary>
    public class AprovacaoItem
    {
        public int Id { get; set; }

        public int ItemSolicitacaoId { get; set; }

        public Erp.Model.Solicitacao.ItemSolicitacao? ItemSolicitacao { get; set; }

        /// <summary>Degrau da cadeia a que este registro corresponde.</summary>
        public int Nivel { get; set; }

        public Guid AprovadorId { get; set; }

        public Erp.Model.Usuario.Usuario? Aprovador { get; set; }

        /// <summary>Instantâneo do nome, pelo mesmo motivo do registro de
        /// histórico: conta excluída não pode apagar quem aprovou.</summary>
        public string AprovadorNome { get; set; } = string.Empty;

        /// <summary>Preenchido quando quem decidiu foi um substituto. Nulo
        /// numa decisão do próprio titular.</summary>
        public Guid? EmNomeDeId { get; set; }

        public string? EmNomeDeNome { get; set; }

        public Erp.Model.Solicitacao.StatusItem Decisao { get; set; }

        public string? Motivo { get; set; }

        /// <summary>Teto do degrau no momento da decisão. Gravado junto porque
        /// a alçada pode ser alterada depois, e a decisão foi tomada sob a
        /// regra que valia naquele dia.</summary>
        public decimal LimiteNoMomento { get; set; }

        /// <summary>Valor do item quando a decisão foi tomada.</summary>
        public decimal ValorNoMomento { get; set; }

        public DateTime DecididoEm { get; set; } = DateTime.UtcNow;

        public bool PorSubstituto => EmNomeDeId is not null;
    }
}
