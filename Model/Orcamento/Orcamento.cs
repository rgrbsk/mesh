namespace Erp.Model.Orcamento
{
    /// <summary>
    /// Verba de um centro de custo em uma competência (ano e mês).
    ///
    /// A granularidade mensal é deliberada: orçamento anual sem recorte de mês
    /// só acusa o estouro em dezembro, quando já não há o que fazer. Quem
    /// trabalha com verba anual cadastra o mesmo valor nos doze meses ou usa o
    /// rateio automático da tela.
    /// </summary>
    public class Orcamento
    {
        public int Id { get; set; }

        public int CentroCustoId { get; set; }

        public Erp.Model.CentroCusto.CentroCusto? CentroCusto { get; set; }

        public int Ano { get; set; }

        /// <summary>1 a 12.</summary>
        public int Mes { get; set; }

        public decimal Valor { get; set; }

        public string Observacao { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public string Competencia => $"{Mes:00}/{Ano}";
    }
}
