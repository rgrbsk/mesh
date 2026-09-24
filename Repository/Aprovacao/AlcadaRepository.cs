using Erp.Data;
using Erp.Model.Aprovacao;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Aprovacao
{
    /// <summary>Um degrau já resolvido: com a delegação aplicada e o nome
    /// pronto para a tela.</summary>
    public sealed record DegrauResolvido(
        int Nivel,
        decimal Limite,
        bool Ilimitado,
        Guid TitularId,
        string TitularNome,
        Guid AprovadorId,
        string AprovadorNome)
    {
        /// <summary>Quem decide não é o titular: há delegação em vigor.</summary>
        public bool PorSubstituto => AprovadorId != TitularId;

        public bool Encerra(decimal valor) => Ilimitado || valor <= Limite;
    }

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// Concentra a cadeia de aprovação: quem são os degraus de um centro, até
    /// que valor cada um decide sozinho e quem responde por eles hoje, já
    /// considerando as delegações em vigor.
    /// </summary>
    public class AlcadaRepository
    {
        private const string Modulo = "Alçadas";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;

        public AlcadaRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs)
        {
            _fabrica = fabrica;
            _logs = logs;
        }

        // ------------------------------------------------------------------
        // Cadastro da cadeia
        // ------------------------------------------------------------------

        public async Task<List<AlcadaAprovacao>> Buscar(int? centroCustoId = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Alcadas
                .AsNoTracking()
                .Include(a => a.CentroCusto)
                .Include(a => a.Aprovador)
                .AsQueryable();

            if (centroCustoId is not null)
                query = query.Where(a => a.CentroCustoId == centroCustoId);

            return await query
                .OrderBy(a => a.CentroCusto!.Codigo)
                .ThenBy(a => a.Ordem)
                .ToListAsync();
        }

        /// <summary>
        /// Substitui a cadeia INTEIRA de um centro pelo que veio da tela. É
        /// tudo-ou-nada de propósito: uma cadeia meio salva teria buracos de
        /// nível, e item promovido a um degrau inexistente ficaria sem
        /// aprovador — parado para sempre.
        /// </summary>
        public async Task SalvarCadeia(int centroCustoId, List<AlcadaAprovacao> degraus, Guid autorId)
        {
            Validar(degraus);

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var atuais = await contexto.Alcadas
                .Where(a => a.CentroCustoId == centroCustoId)
                .ToListAsync();

            contexto.Alcadas.RemoveRange(atuais);

            var ordem = 1;
            foreach (var degrau in degraus.OrderBy(d => d.Ordem))
            {
                contexto.Alcadas.Add(new AlcadaAprovacao
                {
                    CentroCustoId = centroCustoId,
                    Ordem = ordem++,
                    AprovadorId = degrau.AprovadorId,
                    LimiteValor = degrau.LimiteValor,
                    Ativo = degrau.Ativo,
                    CriadoEm = DateTime.UtcNow,
                    ModificadoEm = DateTime.UtcNow,
                });
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Cadeia de aprovação do centro #{centroCustoId} redefinida com {degraus.Count} "
                + (degraus.Count == 1 ? "degrau." : "degraus."),
                autorId,
                entidade: nameof(AlcadaAprovacao),
                entidadeId: centroCustoId.ToString());
        }

        /// <summary>
        /// Duas regras que, quebradas, deixam itens sem aprovador: degrau
        /// repetido na mesma posição e cadeia sem nenhum degrau ilimitado no
        /// fim — que é o que fecha o teto para valores altos.
        /// </summary>
        private static void Validar(List<AlcadaAprovacao> degraus)
        {
            if (degraus.Count == 0)
                return;   // cadeia vazia é permitida: cai no responsável do centro

            if (degraus.Select(d => d.Ordem).Distinct().Count() != degraus.Count)
                throw new InvalidOperationException("Dois degraus não podem ocupar a mesma posição.");

            if (degraus.Any(d => d.AprovadorId == Guid.Empty))
                throw new InvalidOperationException("Todo degrau precisa de um aprovador.");

            var ultimo = degraus.OrderBy(d => d.Ordem).Last();

            if (!ultimo.Ilimitado)
                throw new InvalidOperationException(
                    "O último degrau precisa ser ilimitado — sem ele, item acima do teto "
                    + "ficaria sem ninguém para aprovar.");

            // Limite que não cresce a cada degrau inverte a lógica: o item
            // subiria para alguém que decide menos que o anterior.
            var anteriores = degraus.OrderBy(d => d.Ordem).ToList();
            for (var i = 1; i < anteriores.Count; i++)
            {
                if (anteriores[i].Ilimitado)
                    continue;

                if (anteriores[i].LimiteValor <= anteriores[i - 1].LimiteValor)
                    throw new InvalidOperationException(
                        $"O limite do degrau {anteriores[i].Ordem} precisa ser maior que o do degrau anterior.");
            }
        }

        // ------------------------------------------------------------------
        // Resolução
        // ------------------------------------------------------------------

        /// <summary>
        /// A cadeia de um centro, já com as delegações do momento aplicadas.
        ///
        /// Centro SEM alçada cadastrada devolve um degrau único e ilimitado com
        /// o responsável do centro — é o comportamento que existia antes de
        /// haver alçada, e é o que mantém válidas as solicitações antigas.
        /// </summary>
        public async Task<List<DegrauResolvido>> ResolverCadeia(int centroCustoId, DateTime? momento = null)
        {
            var quando = momento ?? DateTime.UtcNow;

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var degraus = await contexto.Alcadas
                .AsNoTracking()
                .Include(a => a.Aprovador)
                .Where(a => a.CentroCustoId == centroCustoId && a.Ativo)
                .OrderBy(a => a.Ordem)
                .ToListAsync();

            if (degraus.Count == 0)
            {
                var centro = await contexto.CentrosCusto
                    .AsNoTracking()
                    .Include(c => c.Responsavel)
                    .FirstOrDefaultAsync(c => c.Id == centroCustoId);

                if (centro?.ResponsavelId is null)
                    return new List<DegrauResolvido>();

                var (subId, subNome) = await Substituto(contexto, centro.ResponsavelId.Value, quando);

                return new List<DegrauResolvido>
                {
                    new(1, 0m, true,
                        centro.ResponsavelId.Value, NomeDe(centro.Responsavel),
                        subId ?? centro.ResponsavelId.Value, subNome ?? NomeDe(centro.Responsavel)),
                };
            }

            var resolvidos = new List<DegrauResolvido>();

            foreach (var degrau in degraus)
            {
                var (subId, subNome) = await Substituto(contexto, degrau.AprovadorId, quando);

                resolvidos.Add(new DegrauResolvido(
                    degrau.Ordem,
                    degrau.LimiteValor,
                    degrau.Ilimitado,
                    degrau.AprovadorId,
                    NomeDe(degrau.Aprovador),
                    subId ?? degrau.AprovadorId,
                    subNome ?? NomeDe(degrau.Aprovador)));
            }

            return resolvidos;
        }

        /// <summary>
        /// O degrau em que um item de determinado valor está agora. Nulo quando
        /// o nível pedido não existe na cadeia — caso em que o item já passou
        /// por todos e não há mais o que decidir.
        /// </summary>
        public async Task<DegrauResolvido?> DegrauDe(int centroCustoId, int nivel, DateTime? momento = null)
        {
            var cadeia = await ResolverCadeia(centroCustoId, momento);

            return cadeia.FirstOrDefault(d => d.Nivel == nivel);
        }

        /// <summary>
        /// Centros em que esta pessoa decide em ALGUM degrau — direto ou como
        /// substituta. É o filtro grosso da fila de aprovações: reduz a consulta
        /// a poucos centros, e o degrau exato é conferido depois, em memória,
        /// sobre um conjunto já pequeno.
        /// </summary>
        public async Task<List<int>> CentrosOndeDecide(Guid usuarioId, DateTime? momento = null)
        {
            var quando = momento ?? DateTime.UtcNow;

            await using var contexto = await _fabrica.CreateDbContextAsync();

            // Quem ela substitui hoje: as delegações em que é a substituta.
            var titulares = await contexto.Delegacoes
                .AsNoTracking()
                .Where(d => d.SubstitutoId == usuarioId && d.Ativo
                         && quando >= d.Inicio && quando <= d.Fim)
                .Select(d => d.TitularId)
                .ToListAsync();

            titulares.Add(usuarioId);

            var porAlcada = await contexto.Alcadas
                .AsNoTracking()
                .Where(a => a.Ativo && titulares.Contains(a.AprovadorId))
                .Select(a => a.CentroCustoId)
                .ToListAsync();

            // Centros sem alçada cadastrada continuam roteando pelo responsável.
            var comAlcada = await contexto.Alcadas
                .AsNoTracking()
                .Select(a => a.CentroCustoId)
                .Distinct()
                .ToListAsync();

            var porResponsavel = await contexto.CentrosCusto
                .AsNoTracking()
                .Where(c => c.ResponsavelId != null
                         && titulares.Contains(c.ResponsavelId.Value)
                         && !comAlcada.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            return porAlcada.Concat(porResponsavel).Distinct().ToList();
        }

        // ------------------------------------------------------------------
        // Delegações
        // ------------------------------------------------------------------

        public async Task<List<DelegacaoAprovacao>> BuscarDelegacoes(bool somenteVigentes = false)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Delegacoes
                .AsNoTracking()
                .Include(d => d.Titular)
                .Include(d => d.Substituto)
                .AsQueryable();

            if (somenteVigentes)
            {
                var agora = DateTime.UtcNow;
                query = query.Where(d => d.Ativo && agora >= d.Inicio && agora <= d.Fim);
            }

            return await query.OrderByDescending(d => d.Inicio).ToListAsync();
        }

        public async Task<DelegacaoAprovacao> SalvarDelegacao(DelegacaoAprovacao delegacao, Guid autorId)
        {
            if (delegacao.TitularId == delegacao.SubstitutoId)
                throw new InvalidOperationException("O substituto tem que ser outra pessoa.");

            if (delegacao.Fim < delegacao.Inicio)
                throw new InvalidOperationException("O fim do período não pode ser anterior ao início.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            // Duas delegações do mesmo titular no mesmo período deixariam o
            // roteamento ambíguo — não há como escolher entre dois substitutos.
            var conflito = await contexto.Delegacoes.AnyAsync(d =>
                d.Id != delegacao.Id && d.Ativo && d.TitularId == delegacao.TitularId
                && delegacao.Inicio <= d.Fim && delegacao.Fim >= d.Inicio);

            if (conflito)
                throw new InvalidOperationException(
                    "Já existe delegação deste titular que cobre parte deste período.");

            delegacao.Titular = null;
            delegacao.Substituto = null;

            if (delegacao.Id == 0)
            {
                delegacao.CriadoEm = DateTime.UtcNow;
                contexto.Delegacoes.Add(delegacao);
            }
            else
            {
                contexto.Delegacoes.Update(delegacao);
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Delegação de aprovação registrada de {delegacao.Inicio:dd/MM/yyyy} a {delegacao.Fim:dd/MM/yyyy}.",
                autorId,
                entidade: nameof(DelegacaoAprovacao),
                entidadeId: delegacao.Id.ToString());

            return delegacao;
        }

        public async Task EncerrarDelegacao(int id, Guid autorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var delegacao = await contexto.Delegacoes.FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new InvalidOperationException("Delegação não encontrada.");

            delegacao.Ativo = false;
            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Delegação #{id} encerrada.", autorId,
                entidade: nameof(DelegacaoAprovacao), entidadeId: id.ToString());
        }

        // ------------------------------------------------------------------

        private static async Task<(Guid? Id, string? Nome)> Substituto(
            AppDbContext contexto, Guid titularId, DateTime quando)
        {
            var delegacao = await contexto.Delegacoes
                .AsNoTracking()
                .Include(d => d.Substituto)
                .FirstOrDefaultAsync(d => d.TitularId == titularId && d.Ativo
                                       && quando >= d.Inicio && quando <= d.Fim);

            return delegacao is null
                ? (null, null)
                : (delegacao.SubstitutoId, NomeDe(delegacao.Substituto));
        }

        private static string NomeDe(Erp.Model.Usuario.Usuario? usuario) =>
            usuario is null ? "" : $"{usuario.Nome} {usuario.Sobrenome}".Trim();
    }
}
