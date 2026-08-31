using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Produto
{
    using ProdutoFornecedor = Erp.Model.Produto.ProdutoFornecedor;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class ProdutoFornecedorRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public ProdutoFornecedorRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<List<ProdutoFornecedor>> Buscar(int? fornecedorId = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.ProdutosFornecedor
                .AsNoTracking()
                .Include(d => d.Fornecedor)
                .Include(d => d.Produto)
                .AsQueryable();

            if (fornecedorId is { } id)
                query = query.Where(d => d.FornecedorId == id);

            return await query
                .OrderBy(d => d.Fornecedor!.RazaoSocial)
                .ThenBy(d => d.CodigoFornecedor)
                .ToListAsync();
        }

        /// <summary>
        /// O de-para de um fornecedor num dicionário pronto para consulta:
        /// código dele (maiúsculo, sem espaço) → nosso produto. É o formato que
        /// o confronto usa.
        /// </summary>
        public async Task<Dictionary<string, int>> Mapa(int fornecedorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var linhas = await contexto.ProdutosFornecedor
                .AsNoTracking()
                .Where(d => d.FornecedorId == fornecedorId)
                .Select(d => new { d.CodigoFornecedor, d.ProdutoId })
                .ToListAsync();

            // A normalização acontece aqui e no confronto do mesmo jeito: código
            // gravado com espaço ou em caixa diferente tem que casar mesmo assim.
            return linhas
                .GroupBy(l => l.CodigoFornecedor.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().ProdutoId);
        }

        /// <summary>
        /// Grava a amarração. Se o par (fornecedor, código) já existir, aponta
        /// para o novo produto em vez de duplicar — a última decisão de quem
        /// confere é a que vale.
        /// </summary>
        public async Task<ProdutoFornecedor> Salvar(
            int fornecedorId, string codigoFornecedor, int produtoId, string descricaoFornecedor = "")
        {
            var codigo = codigoFornecedor.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
                throw new InvalidOperationException("Informe o código do fornecedor.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var existente = await contexto.ProdutosFornecedor
                .FirstOrDefaultAsync(d => d.FornecedorId == fornecedorId
                                       && d.CodigoFornecedor.ToUpper() == codigo.ToUpper());

            if (existente is not null)
            {
                existente.ProdutoId = produtoId;
                existente.DescricaoFornecedor = descricaoFornecedor;
                existente.ModificadoEm = DateTime.UtcNow;

                await contexto.SaveChangesAsync();

                return existente;
            }

            var novo = new ProdutoFornecedor
            {
                FornecedorId = fornecedorId,
                CodigoFornecedor = codigo,
                ProdutoId = produtoId,
                DescricaoFornecedor = descricaoFornecedor,
            };

            contexto.ProdutosFornecedor.Add(novo);
            await contexto.SaveChangesAsync();

            return novo;
        }

        /// <summary>Grava várias de uma vez — é o que acontece ao importar uma
        /// nota em que a pessoa amarrou vários itens de uma sentada.</summary>
        public async Task SalvarVarias(
            int fornecedorId, IEnumerable<(string Codigo, int ProdutoId, string Descricao)> amarracoes)
        {
            foreach (var (codigo, produtoId, descricao) in amarracoes)
                await Salvar(fornecedorId, codigo, produtoId, descricao);
        }

        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var linha = await contexto.ProdutosFornecedor.FirstOrDefaultAsync(d => d.Id == id);
            if (linha is null)
                return false;

            contexto.ProdutosFornecedor.Remove(linha);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
