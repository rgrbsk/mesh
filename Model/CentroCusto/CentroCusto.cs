using Erp.Model.Usuario;

namespace Erp.Model.CentroCusto
{
    /// <summary>
    /// Unidade de rateio da despesa — e, no fluxo de compras, quem decide:
    /// o aprovador da solicitação sai daqui, não do valor.
    ///
    /// Hierárquico: um centro pode ficar sob outro (Diretoria → Produção →
    /// Manutenção). A árvore serve para rateio e leitura consolidada; a
    /// aprovação continua sendo do responsável do próprio centro.
    /// </summary>
    public class CentroCusto
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        /// <summary>Centro superior. Nulo é raiz.</summary>
        public int? PaiId { get; set; }

        public CentroCusto? Pai { get; set; }

        public ICollection<CentroCusto> Filhos { get; set; } = new List<CentroCusto>();

        /// <summary>Quem aprova as solicitações deste centro. Sem responsável,
        /// as solicitações do centro ficam sem destino.</summary>
        public Guid? ResponsavelId { get; set; }

        public Usuario.Usuario? Responsavel { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;
    }
}
