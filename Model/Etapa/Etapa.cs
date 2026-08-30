namespace Erp.Model.Etapa
{
    /// <summary>
    /// Uma coluna do fluxo ANTES da aprovação. É o que cada empresa organiza do
    /// seu jeito — triagem, conferência de orçamento, o que for. A aprovação em
    /// si não é etapa cadastrável: ela é o fim da esteira e tem regra própria
    /// (responsável do centro de custo do item).
    /// </summary>
    public class Etapa
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        /// <summary>Posição no quadro, da esquerda para a direita.</summary>
        public int Ordem { get; set; }

        /// <summary>Cor do traço da coluna no Kanban.</summary>
        public string Cor { get; set; } = "#89CFF0";

        /// <summary>Etapa inativa some do quadro, mas continua existindo para as
        /// solicitações que passaram por ela.</summary>
        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;
    }
}
