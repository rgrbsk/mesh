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
        /// as solicitações do centro ficam sem destino.
        ///
        /// Continua valendo como o degrau único da cadeia quando o centro não
        /// tem alçada cadastrada — é o que mantém compatível o que já existia
        /// antes de haver alçada.</summary>
        public Guid? ResponsavelId { get; set; }

        public Usuario.Usuario? Responsavel { get; set; }

        /// <summary>
        /// Recusar a aprovação que estoura o orçamento do mês, em vez de apenas
        /// avisar. Fica desligado por padrão: numa organização que ainda não
        /// cadastrou verba, travar tudo seria pior que não controlar.
        /// </summary>
        public bool BloqueiaAcimaOrcamento { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;
    }
}
