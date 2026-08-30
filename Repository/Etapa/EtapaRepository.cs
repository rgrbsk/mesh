using Erp.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Etapa
{
    using Etapa = Erp.Model.Etapa.Etapa;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class EtapaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;

        public EtapaRepository(IDbContextFactory<AppDbContext> fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<List<Etapa>> Buscar(Expression<Func<Etapa, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Etapas.AsNoTracking().AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderBy(e => e.Ordem).ThenBy(e => e.Id).ToListAsync();
        }

        /// <summary>As colunas do quadro, na ordem em que aparecem.</summary>
        public Task<List<Etapa>> BuscarAtivas() => Buscar(e => e.Ativo);

        public async Task<Etapa> Criar(Etapa etapa)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            // Nova etapa entra no fim do quadro: inserir no meio é uma decisão
            // consciente, feita depois pelas setas de reordenar.
            if (etapa.Ordem == 0)
                etapa.Ordem = await contexto.Etapas.AnyAsync()
                    ? await contexto.Etapas.MaxAsync(e => e.Ordem) + 1
                    : 1;

            etapa.CriadoEm = DateTime.UtcNow;
            etapa.ModificadoEm = etapa.CriadoEm;

            contexto.Etapas.Add(etapa);
            await contexto.SaveChangesAsync();

            return etapa;
        }

        public async Task<Etapa> Atualizar(Etapa etapa)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            etapa.ModificadoEm = DateTime.UtcNow;

            contexto.Etapas.Update(etapa);
            await contexto.SaveChangesAsync();

            return etapa;
        }

        /// <summary>
        /// Troca a etapa de posição com a vizinha. Mexer nas duas de uma vez
        /// evita o buraco que sobraria se só uma mudasse de número.
        /// </summary>
        public async Task Mover(int id, bool paraCima)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var etapas = await contexto.Etapas.OrderBy(e => e.Ordem).ThenBy(e => e.Id).ToListAsync();

            var indice = etapas.FindIndex(e => e.Id == id);
            var destino = paraCima ? indice - 1 : indice + 1;

            if (indice < 0 || destino < 0 || destino >= etapas.Count)
                return;

            (etapas[indice].Ordem, etapas[destino].Ordem) = (etapas[destino].Ordem, etapas[indice].Ordem);

            await contexto.SaveChangesAsync();
        }

        /// <summary>
        /// Etapa com solicitação parada nela não se apaga: o quadro perderia os
        /// cartões. Quem quer tirar do fluxo desativa.
        /// </summary>
        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            if (await contexto.Solicitacoes.AnyAsync(s => s.EtapaId == id))
                throw new InvalidOperationException(
                    "Há solicitações nesta etapa. Desative-a em vez de excluir.");

            var etapa = await contexto.Etapas.FirstOrDefaultAsync(e => e.Id == id);
            if (etapa is null)
                return false;

            contexto.Etapas.Remove(etapa);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
