using Erp.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Produto
{
    using Produto = Erp.Model.Produto.Produto;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class ProdutoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public ProdutoRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            this._fabrica = fabrica;
        }

        /// <summary>Lista aplicando o filtro montado no BbFilterBuilder — a
        /// expressão vai para o Where do EF, não para memória.</summary>
        public async Task<List<Produto>> Buscar(Expression<Func<Produto, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Produtos
                .AsNoTracking()
                .Include(p => p.FornecedorPadrao)
                .Include(p => p.CentroCustoPadrao)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderBy(p => p.Descricao).ToListAsync();
        }

        /// <summary>Produtos com saldo no ponto de pedido ou abaixo — a base do
        /// alerta de reposição. A comparação é entre colunas, então roda no
        /// banco.</summary>
        public async Task<List<Produto>> BuscarParaRepor()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Produtos
                .AsNoTracking()
                .Include(p => p.FornecedorPadrao)
                .Include(p => p.CentroCustoPadrao)
                .Where(p => p.Ativo && p.SaldoAtual <= p.PontoPedido)
                .OrderBy(p => p.Descricao)
                .ToListAsync();
        }

        public async Task<Produto?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Produto> Criar(Produto produto)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            produto.CriadoEm = DateTime.UtcNow;
            produto.ModificadoEm = produto.CriadoEm;

            contexto.Produtos.Add(produto);
            await contexto.SaveChangesAsync();

            return produto;
        }

        public async Task<Produto> Atualizar(Produto produto)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            produto.ModificadoEm = DateTime.UtcNow;

            contexto.Produtos.Update(produto);
            await contexto.SaveChangesAsync();

            return produto;
        }

        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var produto = await contexto.Produtos.FirstOrDefaultAsync(p => p.Id == id);
            if (produto is null)
                return false;

            contexto.Produtos.Remove(produto);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
