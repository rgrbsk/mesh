using Microsoft.EntityFrameworkCore;

namespace Erp.Data.Tenancy
{
    /// <summary>Fábrica fixa no banco central — para quem lida com login,
    /// papéis, tenants e logs de sistema, que não pertencem a tenant nenhum.</summary>
    public sealed class FabricaCentral : IDbContextFactory<AppDbContext>
    {
        private readonly ConexoesTenant _conexoes;

        public FabricaCentral(ConexoesTenant conexoes) => _conexoes = conexoes;

        public AppDbContext CreateDbContext() => _conexoes.Central();

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_conexoes.Central());
    }
}
