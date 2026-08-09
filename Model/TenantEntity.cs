namespace Erp.Model
{
    /// <summary>
    /// Base para entidades tenant-scoped: PK <see cref="Id"/> (uuid v7) + <see cref="TenantId"/>.
    /// </summary>
    public abstract class TenantEntity : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public Guid TenantId { get; set; }
    }
}
