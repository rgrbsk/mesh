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

        public EmpresaRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            this._fabrica = fabrica;
        }

        public async Task<List<Empresa>> BuscarEmpresas()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Empresas
                .AsNoTracking()
                .OrderBy(e => e.Nome)
                .ToListAsync();
        }
    }
}
