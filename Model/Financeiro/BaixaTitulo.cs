namespace Erp.Model.Financeiro
{
    /// <summary>
    /// Um pagamento efetuado contra um título. Fica em tabela própria, e não
    /// como campos no título, porque pagamento parcial é comum: o título pode
    /// receber várias baixas, e cada uma precisa de data, valor e forma
    /// próprios para a conciliação bancária fazer sentido.
    /// </summary>
    public class BaixaTitulo
    {
        public int Id { get; set; }

        public int TituloPagarId { get; set; }

        public TituloPagar? TituloPagar { get; set; }

        public DateTime Data { get; set; } = DateTime.UtcNow;

        public decimal Valor { get; set; }

        /// <summary>Texto livre: PIX, boleto, transferência, dinheiro. Um
        /// conjunto fechado engessaria sem ganho — não há regra que dependa
        /// desta informação.</summary>
        public string FormaPagamento { get; set; } = string.Empty;

        public string Observacao { get; set; } = string.Empty;

        public Guid? RegistradaPorId { get; set; }

        public string RegistradaPorNome { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
