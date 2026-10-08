namespace Erp.Model.Empresa
{
    public class Empresa
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Nome { get; set; } = string.Empty;

        public string CNPJ { get; set; } = string.Empty;

        public string IE { get; set; } = string.Empty;

        public string Cor { get; set; } = "#000000";

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        /// <summary>Tenant suspenso não entra: o login dos seus usuários é
        /// recusado e as sessões abertas caem na próxima revalidação.</summary>
        public bool Ativa { get; set; } = true;

        public DateTime? SuspensaEm { get; set; }

        /// <summary>Banco de dados do tenant. Vazio = banco central (o tenant
        /// que existia antes da separação por banco).</summary>
        public string Banco { get; set; } = string.Empty;
    }
}
