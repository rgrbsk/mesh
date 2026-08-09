namespace Erp.Service.Tenancy
{
    /// <summary>Fornece o tenant (Empresa) do contexto atual.</summary>
    public interface ITenantProvider
    {
        /// <summary>Tenant atual. Lança se não resolvido — use em queries/inserts.</summary>
        Guid TenantId { get; }

        /// <summary>
        /// Tenta obter o tenant sem lançar. Usado onde a ausência é válida
        /// (ex.: migrations/design-time, DDL fora do escopo de request).
        /// </summary>
        bool TryGetTenantId(out Guid tenantId);
    }
}
