using Erp.Data;
using Erp.Model.Financeiro;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Financeiro
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// Fecha o ciclo: o que foi pedido, recebido e conferido vira obrigação de
    /// pagamento. A porta de entrada é a LIBERAÇÃO da nota — título gerado a
    /// partir de nota com divergência aberta é exatamente o que o confronto
    /// existe para impedir.
    /// </summary>
    public class TituloPagarRepository
    {
        private const string Modulo = "Contas a pagar";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly Erp.Repository.Notificacao.NotificacaoRepository _avisos;

        public TituloPagarRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            Erp.Repository.Notificacao.NotificacaoRepository avisos)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
        }

        public async Task<List<TituloPagar>> Buscar(Expression<Func<TituloPagar, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.TitulosPagar
                .AsNoTracking()
                .Include(t => t.Fornecedor)
                .Include(t => t.NotaFiscal)
                .Include(t => t.OrdemCompra)
                .Include(t => t.Baixas)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query
                .OrderBy(t => t.Status == StatusTitulo.Pago || t.Status == StatusTitulo.Cancelado)
                .ThenBy(t => t.Vencimento)
                .ToListAsync();
        }

        public async Task<TituloPagar?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.TitulosPagar
                .AsNoTracking()
                .Include(t => t.Fornecedor)
                .Include(t => t.NotaFiscal)
                .Include(t => t.OrdemCompra)
                .Include(t => t.Baixas)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // ------------------------------------------------------------------
        // Liberação da nota
        // ------------------------------------------------------------------

        /// <summary>
        /// Marca a nota como apta a gerar título.
        ///
        /// Quando o confronto não fecha, a liberação continua possível — há
        /// divergências que o comprador aceita, como preço abaixo do acordado —
        /// mas exige justificativa. É a diferença entre decidir e ignorar.
        /// </summary>
        public async Task LiberarNota(int notaFiscalId, bool confrontoFecha, string? motivo, Guid autorId)
        {
            if (!confrontoFecha && string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException(
                    "O confronto desta nota tem divergência. Liberar assim exige justificativa.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var nota = await contexto.NotasFiscais.FirstOrDefaultAsync(n => n.Id == notaFiscalId)
                ?? throw new InvalidOperationException("Nota fiscal não encontrada.");

            if (nota.EhHomologacao)
                throw new InvalidOperationException(
                    "Nota emitida em homologação não tem validade fiscal e não pode gerar pagamento.");

            if (nota.LiberadaParaPagamento)
                throw new InvalidOperationException("Esta nota já foi liberada.");

            nota.LiberadaParaPagamento = true;
            nota.LiberadaEm = DateTime.UtcNow;
            nota.LiberadaPorId = autorId;
            nota.MotivoLiberacao = motivo;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Aprovacao, Modulo,
                $"Nota {nota.Numero} liberada para pagamento."
                + (confrontoFecha ? " Confronto sem divergência." : $" Com ressalva: {motivo}"),
                autorId,
                entidade: nameof(Erp.Model.Fiscal.NotaFiscal),
                entidadeId: notaFiscalId.ToString());
        }

        // ------------------------------------------------------------------
        // Geração
        // ------------------------------------------------------------------

        /// <summary>
        /// Gera os títulos de uma nota liberada, parcelados nos vencimentos
        /// informados.
        ///
        /// O rateio distribui o valor em partes iguais e joga a sobra de
        /// centavos na PRIMEIRA parcela — nunca na última, que costuma ser a
        /// conferida contra o extrato e onde uma diferença de centavo chama
        /// mais atenção.
        /// </summary>
        public async Task<List<TituloPagar>> GerarDeNota(
            int notaFiscalId, IReadOnlyList<DateTime> vencimentos, Guid autorId)
        {
            if (vencimentos.Count == 0)
                throw new InvalidOperationException("Informe ao menos um vencimento.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var nota = await contexto.NotasFiscais
                .Include(n => n.Fornecedor)
                .FirstOrDefaultAsync(n => n.Id == notaFiscalId)
                ?? throw new InvalidOperationException("Nota fiscal não encontrada.");

            if (!nota.LiberadaParaPagamento)
                throw new InvalidOperationException(
                    "Esta nota ainda não foi liberada para pagamento.");

            if (nota.FornecedorId is null)
                throw new InvalidOperationException(
                    "A nota não está amarrada a um fornecedor do cadastro.");

            var jaExiste = await contexto.TitulosPagar
                .AnyAsync(t => t.NotaFiscalId == notaFiscalId && t.Status != StatusTitulo.Cancelado);

            if (jaExiste)
                throw new InvalidOperationException(
                    "Esta nota já gerou título. Cancele o anterior antes de gerar outro.");

            var numeroBase = await ProximoNumero(contexto);
            var total = nota.ValorTotal;
            var parcela = Math.Round(total / vencimentos.Count, 2, MidpointRounding.AwayFromZero);
            var sobra = total - (parcela * vencimentos.Count);

            var titulos = new List<TituloPagar>();

            for (var i = 0; i < vencimentos.Count; i++)
            {
                var valor = parcela + (i == 0 ? sobra : 0m);

                var titulo = new TituloPagar
                {
                    Numero = numeroBase,
                    FornecedorId = nota.FornecedorId.Value,
                    NotaFiscalId = nota.Id,
                    OrdemCompraId = nota.OrdemCompraId,
                    Emissao = nota.Emissao,
                    Vencimento = vencimentos[i],
                    Valor = valor,
                    Parcela = i + 1,
                    TotalParcelas = vencimentos.Count,
                    Status = StatusTitulo.Aberto,
                    CriadoPorId = autorId,
                };

                contexto.TitulosPagar.Add(titulo);
                titulos.Add(titulo);
            }

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Criacao, Modulo,
                $"Nota {nota.Numero} de {nota.Fornecedor?.RazaoSocial} gerou {titulos.Count} "
                + (titulos.Count == 1 ? "título" : "títulos")
                + $" somando {total:C2}.",
                autorId,
                entidade: nameof(TituloPagar),
                entidadeId: numeroBase);

            return titulos;
        }

        // ------------------------------------------------------------------
        // Baixa
        // ------------------------------------------------------------------

        /// <summary>
        /// Registra um pagamento. Aceita baixa parcial — é o caso de acordo
        /// com o fornecedor — e recusa pagar mais que o saldo, que seria
        /// crédito a receber, não pagamento.
        /// </summary>
        public async Task RegistrarBaixa(
            int tituloId, BaixaTitulo baixa, Guid autorId, string autorNome)
        {
            if (baixa.Valor <= 0m)
                throw new InvalidOperationException("O valor da baixa tem que ser maior que zero.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var titulo = await contexto.TitulosPagar
                .Include(t => t.Baixas)
                .Include(t => t.Fornecedor)
                .FirstOrDefaultAsync(t => t.Id == tituloId)
                ?? throw new InvalidOperationException("Título não encontrado.");

            if (titulo.Status == StatusTitulo.Cancelado)
                throw new InvalidOperationException("Título cancelado não recebe baixa.");

            if (titulo.Status == StatusTitulo.Pago)
                throw new InvalidOperationException("Este título já está quitado.");

            if (baixa.Valor > titulo.Saldo)
                throw new InvalidOperationException(
                    $"A baixa de {baixa.Valor:C2} passa do saldo de {titulo.Saldo:C2}.");

            titulo.Baixas.Add(new BaixaTitulo
            {
                Data = baixa.Data,
                Valor = baixa.Valor,
                FormaPagamento = baixa.FormaPagamento,
                Observacao = baixa.Observacao,
                RegistradaPorId = autorId,
                RegistradaPorNome = autorNome,
            });

            titulo.ValorPago += baixa.Valor;
            titulo.ModificadoEm = DateTime.UtcNow;

            // O estado é consequência do saldo, nunca digitado.
            titulo.Status = titulo.ValorPago >= titulo.Valor
                ? StatusTitulo.Pago
                : StatusTitulo.ParcialmentePago;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Baixa de {baixa.Valor:C2} no título {titulo.Identificacao} "
                + $"({titulo.Fornecedor?.RazaoSocial}). "
                + (titulo.Status == StatusTitulo.Pago ? "Título quitado." : $"Saldo: {titulo.Saldo:C2}."),
                autorId,
                entidade: nameof(TituloPagar),
                entidadeId: tituloId.ToString());
        }

        public async Task Cancelar(int id, string motivo, Guid autorId)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Cancelar exige motivo.");

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var titulo = await contexto.TitulosPagar
                .Include(t => t.Baixas)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Título não encontrado.");

            if (titulo.Baixas.Count > 0)
                throw new InvalidOperationException(
                    "Este título já recebeu baixa. Cancelar apagaria um pagamento registrado.");

            titulo.Status = StatusTitulo.Cancelado;
            titulo.MotivoCancelamento = motivo;
            titulo.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Título {titulo.Identificacao} cancelado. Motivo: {motivo}",
                autorId,
                entidade: nameof(TituloPagar),
                entidadeId: id.ToString());
        }

        /// <summary>Totais que o painel e os cartões da tela mostram.</summary>
        public async Task<(decimal Aberto, decimal Vencido, decimal AVencer7, int Quantidade)> Resumo()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var hoje = DateTime.UtcNow.Date;
            var limite = hoje.AddDays(7);

            var abertos = await contexto.TitulosPagar
                .AsNoTracking()
                .Where(t => t.Status == StatusTitulo.Aberto || t.Status == StatusTitulo.ParcialmentePago)
                .Select(t => new { t.Valor, t.ValorPago, t.Vencimento })
                .ToListAsync();

            var saldo = abertos.Select(t => new { Saldo = t.Valor - t.ValorPago, t.Vencimento }).ToList();

            return (
                saldo.Sum(t => t.Saldo),
                saldo.Where(t => t.Vencimento.Date < hoje).Sum(t => t.Saldo),
                saldo.Where(t => t.Vencimento.Date >= hoje && t.Vencimento.Date <= limite).Sum(t => t.Saldo),
                saldo.Count);
        }

        private static async Task<string> ProximoNumero(AppDbContext contexto)
        {
            var ano = DateTime.UtcNow.Year;
            var prefixo = $"TP-{ano}-";

            var ultimo = await contexto.TitulosPagar
                .Where(t => t.Numero.StartsWith(prefixo))
                .OrderByDescending(t => t.Numero)
                .Select(t => t.Numero)
                .FirstOrDefaultAsync();

            var sequencial = 1;

            if (ultimo is not null && int.TryParse(ultimo[prefixo.Length..], out var n))
                sequencial = n + 1;

            return $"{prefixo}{sequencial:0000}";
        }
    }
}
