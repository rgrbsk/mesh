namespace Erp.Model.Aprovacao
{
    /// <summary>
    /// Um degrau da cadeia de aprovação de um centro de custo.
    ///
    /// A regra é por VALOR: o degrau aprova sozinho até o seu limite; acima
    /// dele, a decisão favorável não encerra o item — ela o promove ao degrau
    /// seguinte. É isso que na prática se chama alçada.
    ///
    /// Centro sem nenhuma alçada cadastrada continua funcionando como antes:
    /// o responsável do centro decide, sem limite. Isso mantém as solicitações
    /// existentes válidas e torna o recurso opcional.
    /// </summary>
    public class AlcadaAprovacao
    {
        public int Id { get; set; }

        public int CentroCustoId { get; set; }

        public Erp.Model.CentroCusto.CentroCusto? CentroCusto { get; set; }

        /// <summary>Posição na cadeia, a partir de 1. Dois degraus não podem
        /// dividir a mesma ordem no mesmo centro.</summary>
        public int Ordem { get; set; }

        public Guid AprovadorId { get; set; }

        public Erp.Model.Usuario.Usuario? Aprovador { get; set; }

        /// <summary>
        /// Teto que este degrau aprova sozinho. Zero significa SEM LIMITE, e é
        /// o que deve encerrar a cadeia: uma cadeia em que nenhum degrau é
        /// ilimitado deixaria itens caros sem ninguém para aprovar.
        /// </summary>
        public decimal LimiteValor { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public bool Ilimitado => LimiteValor <= 0m;

        /// <summary>Este degrau encerra a decisão para o valor informado?</summary>
        public bool Encerra(decimal valor) => Ilimitado || valor <= LimiteValor;
    }
}
