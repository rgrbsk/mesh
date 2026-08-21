using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Empresa
{
    using Empresa = Erp.Model.Empresa.Empresa;

    public class EmpresaRepository
    {
        private readonly AppDbContext _context;

        public EmpresaRepository(AppDbContext context)
        {
            this._context = context;
        }

        public Task<List<Empresa>> BuscarEmpresas() =>
            _context.Empresas
                .AsNoTracking()
                .OrderBy(e => e.Nome)
                .ToListAsync();
    }
}
