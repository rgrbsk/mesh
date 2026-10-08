using Microsoft.EntityFrameworkCore;

namespace Erp.Data.Tenancy
{
    /// <summary>
    /// A fábrica que os repositórios de negócio recebem: abre o contexto no
    /// banco do tenant de quem está usando. Sem tenant (dono da aplicação,
    /// requisição anônima), cai no banco central.
    /// </summary>
    public sealed class FabricaTenant : IDbContextFactory<AppDbContext>
    {
        private readonly ConexoesTenant _conexoes;
        private readonly TenantAtual _tenant;

        public FabricaTenant(ConexoesTenant conexoes, TenantAtual tenant)
        {
            _conexoes = conexoes;
            _tenant = tenant;
        }

        public AppDbContext CreateDbContext() =>
            _conexoes.Criar(_tenant.Banco().GetAwaiter().GetResult());

        public async Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            _conexoes.Criar(await _tenant.Banco());
    }
}
