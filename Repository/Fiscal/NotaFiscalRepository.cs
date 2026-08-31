using Erp.Data;
using Erp.Model.Cotacao;
using Erp.Repository.Notificacao;
using Erp.Model.Fiscal;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Erp.Repository.Fiscal
{
    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    /// </summary>
    public class NotaFiscalRepository
    {
        private const string Modulo = "Notas fiscais";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly NotificacaoRepository _avisos;

        public NotaFiscalRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            NotificacaoRepository avisos)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
        }

        public async Task<List<NotaFiscal>> Buscar(Expression<Func<NotaFiscal, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.NotasFiscais
                .AsNoTracking()
                .Include(n => n.Fornecedor)
                .Include(n => n.Cotacao)
                .Include(n => n.Itens)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderByDescending(n => n.ImportadaEm).ToListAsync();
        }

        public async Task<NotaFiscal?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.NotasFiscais
                .AsNoTracking()
                .Include(n => n.Fornecedor)
                .Include(n => n.Cotacao)
                .Include(n => n.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        /// <summary>A chave já está no banco? É o que impede o mesmo arquivo de
        /// entrar duas vezes e dobrar uma compra.</summary>
        public async Task<NotaFiscal?> PorChave(string chave)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.NotasFiscais
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Chave == chave);
        }

        /// <summary>
        /// Fornecedor do cadastro com este CNPJ. A comparação é por dígitos: o
        /// cadastro costuma ter máscara e a NF-e nunca tem.
        /// </summary>
        public async Task<Erp.Model.Pessoa.Pessoa?> FornecedorPorCnpj(string cnpjSoDigitos)
        {
            if (string.IsNullOrWhiteSpace(cnpjSoDigitos))
                return null;

            await using var contexto = await _fabrica.CreateDbContextAsync();

            // Traz os candidatos e compara em memória: normalizar máscara dentro
            // do SQL exigiria função específica do Postgres, e a tabela de
            // pessoas é pequena o bastante para isso não pesar.
            var pessoas = await contexto.Pessoas
                .AsNoTracking()
                .Where(p => p.CNPJ != null && p.CNPJ != "")
                .Select(p => new { p.Id, p.CNPJ })
                .ToListAsync();

            var achada = pessoas.FirstOrDefault(p =>
                Erp.Service.Fiscal.LeitorNfe.SoDigitos(p.CNPJ) == cnpjSoDigitos);

            return achada is null
                ? null
                : await contexto.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == achada.Id);
        }

        /// <summary>
        /// Itens que este fornecedor ganhou nesta cotação, com as propostas —
        /// é o lado "comprado" do confronto.
        /// </summary>
        public async Task<List<CotacaoItem>> ItensGanhosPeloFornecedor(int cotacaoId, int fornecedorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.CotacaoItens
                .AsNoTracking()
                .Include(i => i.Produto)
                .Include(i => i.Propostas)
                .Include(i => i.ConviteVencedor)
                .Where(i => i.CotacaoId == cotacaoId
                         && i.ConviteVencedorId != null
                         && i.ConviteVencedor!.PessoaId == fornecedorId)
                .ToListAsync();
        }

        /// <summary>Cotações em que este fornecedor tem item vencedor — as
        /// candidatas a receber esta nota.</summary>
        // Nome completo de propósito: dentro de Erp.Repository.Fiscal, "Cotacao"
        // resolve para o NAMESPACE Erp.Repository.Cotacao, não para o tipo.
        public async Task<List<Erp.Model.Cotacao.Cotacao>> CotacoesComVitoriaDo(int fornecedorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Cotacoes
                .AsNoTracking()
                .Where(c => c.Itens.Any(i => i.ConviteVencedorId != null
                                          && i.ConviteVencedor!.PessoaId == fornecedorId))
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<NotaFiscal> Importar(NotaFiscal nota)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            if (await contexto.NotasFiscais.AnyAsync(n => n.Chave == nota.Chave))
                throw new InvalidOperationException(
                    $"A nota {nota.Numero} (chave {nota.Chave}) já foi importada.");

            nota.ImportadaEm = DateTime.UtcNow;
            nota.Fornecedor = null;
            nota.Cotacao = null;

            foreach (var item in nota.Itens)
            {
                item.NotaFiscal = null;
                item.Produto = null;
            }

            contexto.NotasFiscais.Add(nota);
            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Criacao, Modulo,
                $"NF-e {nota.Numero} de {nota.EmitenteNome} importada — "
                + $"{nota.Itens.Count} {(nota.Itens.Count == 1 ? "item" : "itens")}, "
                + $"total {nota.ValorTotal:C2}."
                + (nota.EhHomologacao ? " ATENÇÃO: nota de homologação, sem valor fiscal." : ""),
                nota.ImportadaPorId,
                entidade: nameof(NotaFiscal),
                entidadeId: nota.Id.ToString());

            return nota;
        }

        // ------------------------------------------------------------------
        // PÚBLICO — atende a tela do fornecedor, que só tem o token.
        // ------------------------------------------------------------------

        /// <summary>
        /// Recebe a NF-e enviada pelo próprio fornecedor, pelo link dele.
        ///
        /// Chaveado pelo TOKEN, nunca por id: é o que impede uma tela adulterada
        /// de anexar nota em nome de outro. E o CNPJ do EMITENTE tem que bater
        /// com o do fornecedor convidado — sem essa conferência, quem tivesse um
        /// link válido subiria a nota de qualquer empresa.
        /// </summary>
        public async Task<NotaFiscal> ImportarDoFornecedor(string token, NotaFiscal nota)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var convite = await contexto.Convites
                .Include(c => c.Pessoa)
                .Include(c => c.Cotacao)
                .FirstOrDefaultAsync(c => c.Token == token)
                ?? throw new InvalidOperationException("Link inválido.");

            if (convite.Status == StatusConvite.NotaEnviada)
                throw new InvalidOperationException("A nota desta compra já foi enviada.");

            if (convite.Status != StatusConvite.Vencedor)
                throw new InvalidOperationException(
                    "Este link não está liberado para envio de nota fiscal.");

            if (convite.ExpiraEm < DateTime.UtcNow)
                throw new InvalidOperationException("O prazo para enviar a nota venceu.");

            // O CNPJ que vale é o declarado por ele na proposta; o do cadastro
            // serve de reserva quando a proposta veio sem CNPJ.
            var esperado = Erp.Service.Fiscal.LeitorNfe.SoDigitos(
                string.IsNullOrWhiteSpace(convite.Cnpj) ? convite.Pessoa?.CNPJ : convite.Cnpj);

            if (!string.IsNullOrWhiteSpace(esperado) && esperado != nota.EmitenteCnpj)
                throw new InvalidOperationException(
                    "O CNPJ que emitiu esta nota não é o da empresa convidada para esta cotação.");

            if (await contexto.NotasFiscais.AnyAsync(n => n.Chave == nota.Chave))
                throw new InvalidOperationException("Esta nota já foi enviada.");

            nota.FornecedorId = convite.PessoaId;
            nota.CotacaoId = convite.CotacaoId;
            nota.ImportadaEm = DateTime.UtcNow;
            // Sem usuário: veio de fora do login, pelo link do fornecedor.
            nota.ImportadaPorId = null;
            nota.Fornecedor = null;
            nota.Cotacao = null;

            foreach (var item in nota.Itens)
            {
                item.NotaFiscal = null;
                item.Produto = null;
            }

            contexto.NotasFiscais.Add(nota);

            convite.Status = StatusConvite.NotaEnviada;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Resposta, Modulo,
                $"NF-e {nota.Numero} enviada pelo fornecedor {nota.EmitenteNome} "
                + $"(CNPJ {nota.EmitenteCnpj}) na cotação #{convite.CotacaoId}, "
                + $"total {nota.ValorTotal:C2}."
                + (nota.EhHomologacao ? " ATENÇÃO: nota de homologação, sem valor fiscal." : ""),
                entidade: nameof(NotaFiscal),
                entidadeId: nota.Id.ToString());

            // Avisa quem montou a rodada: a nota chegou sem ninguém de dentro
            // estar na tela.
            if (convite.Cotacao?.CriadoPorId is { } comprador)
                await _avisos.Criar(
                    comprador,
                    "Nota fiscal recebida",
                    $"{nota.EmitenteNome} enviou a NF-e {nota.Numero} da cotação #{convite.CotacaoId}.",
                    "file-check-2",
                    "/home/notas");

            return nota;
        }

        /// <summary>
        /// Refaz o confronto de uma nota já gravada, contra a compra e o de-para
        /// atuais.
        ///
        /// É recalculado, não guardado: se alguém amarrar um código no de-para
        /// depois, ou corrigir o vencedor, a conferência tem que refletir isso.
        /// Congelar o resultado deixaria a tela mentindo sobre o estado atual.
        /// </summary>
        public async Task<Erp.Service.Fiscal.ResultadoConfronto?> Confrontar(int notaId)
        {
            var nota = await ObterPorId(notaId);

            if (nota?.CotacaoId is not { } cotacaoId || nota.FornecedorId is not { } fornecedorId)
                return null;

            var comprados = await ItensGanhosPeloFornecedor(cotacaoId, fornecedorId);

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var dePara = (await contexto.ProdutosFornecedor
                    .AsNoTracking()
                    .Where(d => d.FornecedorId == fornecedorId)
                    .Select(d => new { d.CodigoFornecedor, d.ProdutoId })
                    .ToListAsync())
                .GroupBy(d => d.CodigoFornecedor.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().ProdutoId);

            return Erp.Service.Fiscal.ConfrontoNfe.Comparar(nota, comprados, dePara);
        }

        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var nota = await contexto.NotasFiscais.FirstOrDefaultAsync(n => n.Id == id);
            if (nota is null)
                return false;

            contexto.NotasFiscais.Remove(nota);
            await contexto.SaveChangesAsync();

            return true;
        }
    }
}
