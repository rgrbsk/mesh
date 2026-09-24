namespace Erp.Model.Aprovacao
{
    /// <summary>
    /// Transfere temporariamente a competência de aprovar de um titular para um
    /// substituto — férias, afastamento, viagem.
    ///
    /// A delegação NÃO reescreve a alçada: ela é resolvida no momento do
    /// roteamento. Assim, quando o período termina, tudo volta ao titular sem
    /// que nada precise ser desfeito, e o histórico continua mostrando que o
    /// substituto decidiu em nome de quem.
    /// </summary>
    public class DelegacaoAprovacao
    {
        public int Id { get; set; }

        public Guid TitularId { get; set; }

        public Erp.Model.Usuario.Usuario? Titular { get; set; }

        public Guid SubstitutoId { get; set; }

        public Erp.Model.Usuario.Usuario? Substituto { get; set; }

        public DateTime Inicio { get; set; }

        public DateTime Fim { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public bool VigenteEm(DateTime momento) =>
            Ativo && momento >= Inicio && momento <= Fim;
    }
}
