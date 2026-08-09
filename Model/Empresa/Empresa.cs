namespace Erp.Model.Empresa
{
    /// <summary>
    /// Raiz do tenant. NÃO implementa <see cref="ITenantEntity"/> — ela É o tenant;
    /// as demais entidades referenciam esta via <c>TenantId</c>.
    /// </summary>
    public class Empresa
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Nome { get; set; } = string.Empty;

        public string CNPJ { get; set; } = string.Empty;

        public string IE { get; set; } = string.Empty;

        public string Cor { get; set; } = "#000000";

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
