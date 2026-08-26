using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Cidades
{
    using Cidade = Erp.Model.Cidades.Cidade;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class CidadeRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public CidadeRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            this._fabrica = fabrica;
        }

        public async Task<List<Cidade>> Buscar()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Cidades
                .AsNoTracking()
                .OrderBy(c => c.Descricao)
                .ToListAsync();
        }
    }
}
