namespace Erp.Model
{
    /// <summary>Marca uma entidade que pertence a um tenant (Empresa).</summary>
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
