using Erp.Data;
using Erp.Model.Pessoa;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Pessoa
{
    using Pessoa = Erp.Model.Pessoa.Pessoa;

    /// <summary>
    /// Acesso à tabela de pessoas — clientes, fornecedores, colaboradores e
    /// demais tipos. Registrado automaticamente no DI pelo sufixo "Repository"
    /// (ver RegistroServicos).
    ///
    /// Um contexto por operação, vindo da fábrica: no Blazor Server o escopo
    /// dura o circuito inteiro, e componentes que carregam dados ao mesmo tempo
    /// (a lista e o diálogo, por exemplo) colidiriam no mesmo DbContext.
    /// </summary>
    public class PessoaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public PessoaRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            this._fabrica = fabrica;
        }

        /// <summary>Lista para a grade. <paramref name="tipo"/> nulo traz todos
        /// os tipos; <paramref name="busca"/> filtra por razão social, fantasia
        /// ou documento.</summary>
        public async Task<List<Pessoa>> Buscar(TipoPessoa? tipo = null, string? busca = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Pessoas
                .AsNoTracking()
                .Include(p => p.Cidade)
                .AsQueryable();

            if (tipo is not null)
                query = query.Where(p => p.Tipo == tipo);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = $"%{busca.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.RazaoSocial, termo) ||
                    EF.Functions.ILike(p.NomeFantasia ?? "", termo) ||
                    EF.Functions.ILike(p.CNPJ, termo));
            }

            return await query.OrderBy(p => p.RazaoSocial).ToListAsync();
        }

        /// <summary>Lista aplicando o filtro montado no BbFilterBuilder. A
        /// expressão vai para o banco (Where do EF), não para memória.</summary>
        public async Task<List<Pessoa>> Buscar(Expression<Func<Pessoa, bool>>? filtro)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Pessoas
                .AsNoTracking()
                .Include(p => p.Cidade)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderBy(p => p.RazaoSocial).ToListAsync();
        }

        public async Task<Pessoa?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Pessoas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Pessoa> Criar(Pessoa pessoa)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            pessoa.CriadoEm = DateTime.UtcNow;
            pessoa.ModificadoEm = pessoa.CriadoEm;

            contexto.Pessoas.Add(pessoa);
            await contexto.SaveChangesAsync();

            return pessoa;
        }

        public async Task<Pessoa> Atualizar(Pessoa pessoa)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            pessoa.ModificadoEm = DateTime.UtcNow;

            // A tela edita uma cópia desanexada; por isso Update e não só Save.
            contexto.Pessoas.Update(pessoa);
            await contexto.SaveChangesAsync();

            return pessoa;
        }

        /// <summary>Remove de vez. Retorna false se o registro já não existia —
        /// dois usuários excluindo a mesma linha não deve virar exceção.</summary>
        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var pessoa = await contexto.Pessoas.FirstOrDefaultAsync(p => p.Id == id);
            if (pessoa is null)
                return false;

            contexto.Pessoas.Remove(pessoa);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
