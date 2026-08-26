using Erp.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.CentroCusto
{
    using CentroCusto = Erp.Model.CentroCusto.CentroCusto;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class CentroCustoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public CentroCustoRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            this._fabrica = fabrica;
        }

        /// <summary>Lista aplicando o filtro montado no BbFilterBuilder — a
        /// expressão vai para o Where do EF, não para memória.</summary>
        public async Task<List<CentroCusto>> Buscar(Expression<Func<CentroCusto, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.CentrosCusto
                .AsNoTracking()
                .Include(c => c.Responsavel)
                .Include(c => c.Pai)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderBy(c => c.Codigo).ToListAsync();
        }

        /// <summary>
        /// Ids do centro e de toda a subárvore abaixo dele. Serve para o
        /// seletor de pai não oferecer opções que criariam ciclo — um centro
        /// não pode ficar sob um descendente seu, nem sob si mesmo.
        /// </summary>
        public async Task<HashSet<int>> IdsDaSubarvore(int raizId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            // Uma leitura só: a árvore de centros de custo é pequena, e
            // percorrer em memória evita N consultas por nível.
            var pares = await contexto.CentrosCusto
                .AsNoTracking()
                .Select(c => new { c.Id, c.PaiId })
                .ToListAsync();

            var porPai = pares
                .Where(p => p.PaiId is not null)
                .GroupBy(p => p.PaiId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var resultado = new HashSet<int> { raizId };
            var fila = new Queue<int>();
            fila.Enqueue(raizId);

            while (fila.Count > 0)
            {
                var atual = fila.Dequeue();
                if (!porPai.TryGetValue(atual, out var filhos))
                    continue;

                foreach (var filho in filhos)
                    if (resultado.Add(filho))
                        fila.Enqueue(filho);
            }

            return resultado;
        }

        /// <summary>Só os ativos, para os seletores das outras telas.</summary>
        public async Task<List<CentroCusto>> BuscarAtivos()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.CentrosCusto
                .AsNoTracking()
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        public async Task<CentroCusto?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.CentrosCusto
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CentroCusto> Criar(CentroCusto centro)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            centro.CriadoEm = DateTime.UtcNow;
            centro.ModificadoEm = centro.CriadoEm;

            contexto.CentrosCusto.Add(centro);
            await contexto.SaveChangesAsync();

            return centro;
        }

        public async Task<CentroCusto> Atualizar(CentroCusto centro)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            centro.ModificadoEm = DateTime.UtcNow;

            contexto.CentrosCusto.Update(centro);
            await contexto.SaveChangesAsync();

            return centro;
        }

        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var centro = await contexto.CentrosCusto.FirstOrDefaultAsync(c => c.Id == id);
            if (centro is null)
                return false;

            contexto.CentrosCusto.Remove(centro);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
