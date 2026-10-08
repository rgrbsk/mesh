using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Empresa
{
    using Empresa = Erp.Model.Empresa.Empresa;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class EmpresaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        private readonly Erp.Data.Tenancy.TenantAtual _tenant;
        private readonly Erp.Data.Tenancy.ConexoesTenant _conexoes;

        public EmpresaRepository(
            Erp.Data.Tenancy.FabricaCentral fabrica,
            Erp.Data.Tenancy.TenantAtual tenant,
            Erp.Data.Tenancy.ConexoesTenant conexoes)
        {
            this._fabrica = fabrica;
            _tenant = tenant;
            _conexoes = conexoes;
        }

        public async Task<List<Empresa>> BuscarEmpresas()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Empresas.AsNoTracking();

            // Cada tenant enxerga a própria empresa. A página pública da
            // cotação não tem login, mas sabe o banco pelo link.
            if (await _tenant.EmpresaId() is { } empresa)
                query = query.Where(e => e.Id == empresa);
            else if (await _tenant.Banco() is { } banco)
            {
                var central = _conexoes.EhCentral(banco);
                query = query.Where(e => central ? e.Banco == "" || e.Banco == _conexoes.BancoCentral : e.Banco == banco);
            }

            return await query.OrderBy(e => e.CriadoEm).ToListAsync();
        }
    }
}
