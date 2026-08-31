using Erp.Data;
using Erp.Model.Cotacao;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace Erp.Repository.Cotacao
{
    using Cotacao = Erp.Model.Cotacao.Cotacao;

    /// <summary>
    /// Um contexto por operação, vindo da fábrica — ver PessoaRepository para o
    /// porquê.
    ///
    /// Os métodos estão em dois grupos, e a divisão é de segurança, não de
    /// organização: os de cima servem a tela do comprador, autenticada; os
    /// marcados como PÚBLICOS respondem a quem tem só um token na URL, e por
    /// isso nunca recebem id — quem passa id escolhe o que quer ler.
    /// </summary>
    public class CotacaoRepository
    {
        private const string Modulo = "Cotações";

        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Repository.Log.LogRepository _logs;
        private readonly Erp.Repository.Notificacao.NotificacaoRepository _avisos;
        private readonly Erp.Service.Email.EmailService _emails;
        private readonly Erp.Repository.Empresa.EmpresaRepository _empresas;

        public CotacaoRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Repository.Log.LogRepository logs,
            Erp.Repository.Notificacao.NotificacaoRepository avisos,
            Erp.Service.Email.EmailService emails,
            Erp.Repository.Empresa.EmpresaRepository empresas)
        {
            _fabrica = fabrica;
            _logs = logs;
            _avisos = avisos;
            _emails = emails;
            _empresas = empresas;
        }

        /// <summary>
        /// Identifica quem está comprando, para o e-mail não chegar anônimo.
        /// A empresa vem do cadastro; o contato é quem criou a rodada.
        /// </summary>
        public async Task<Erp.Service.Email.EmailService.Comprador> Comprador(Guid? criadoPorId)
        {
            var empresa = (await _empresas.BuscarEmpresas()).FirstOrDefault();

            await using var contexto = await _fabrica.CreateDbContextAsync();

            var usuario = criadoPorId is { } id
                ? await contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
                : null;

            return new Erp.Service.Email.EmailService.Comprador(
                empresa?.Nome ?? "",
                empresa?.CNPJ ?? "",
                usuario is null ? "" : $"{usuario.Nome} {usuario.Sobrenome}".Trim(),
                usuario?.Email ?? "");
        }

        public async Task<List<Cotacao>> Buscar(Expression<Func<Cotacao, bool>>? filtro = null)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Cotacoes
                .AsNoTracking()
                .Include(c => c.Itens).ThenInclude(i => i.Produto)
                .Include(c => c.Convites).ThenInclude(f => f.Pessoa)
                .AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query.OrderByDescending(c => c.Id).ToListAsync();
        }

        public async Task<Cotacao?> ObterPorId(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Cotacoes
                .AsNoTracking()
                .Include(c => c.Itens).ThenInclude(i => i.Produto)
                .Include(c => c.Convites).ThenInclude(f => f.Pessoa)
                .Include(c => c.Convites).ThenInclude(f => f.Propostas)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cotacao> Criar(Cotacao cotacao)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            cotacao.CriadoEm = DateTime.UtcNow;
            cotacao.ModificadoEm = cotacao.CriadoEm;

            foreach (var convite in cotacao.Convites)
                PrepararConvite(convite, cotacao.PrazoResposta);

            LimparNavegacoes(cotacao);

            contexto.Cotacoes.Add(cotacao);
            await contexto.SaveChangesAsync();

            return cotacao;
        }

        /// <summary>
        /// Itens e convites da tela são a verdade: o que sumiu é apagado, o que
        /// veio sem Id é inserido. Convite já respondido NÃO é removido nem tem
        /// o token trocado — isso apagaria a proposta de alguém.
        /// </summary>
        public async Task<Cotacao> Atualizar(Cotacao cotacao)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var gravada = await contexto.Cotacoes
                .Include(c => c.Itens)
                .Include(c => c.Convites)
                .FirstOrDefaultAsync(c => c.Id == cotacao.Id)
                ?? throw new InvalidOperationException("Cotação não encontrada.");

            if (!gravada.Editavel)
                throw new InvalidOperationException(
                    "Cotação já aberta não se edita: os fornecedores estão respondendo o que foi enviado.");

            gravada.Titulo = cotacao.Titulo;
            gravada.Observacao = cotacao.Observacao;
            gravada.PrazoResposta = cotacao.PrazoResposta;
            gravada.ModificadoEm = DateTime.UtcNow;

            SincronizarItens(contexto, gravada, cotacao);
            SincronizarConvites(contexto, gravada, cotacao);

            await contexto.SaveChangesAsync();

            return gravada;
        }

        /// <summary>
        /// Publica a rodada: daqui em diante os links funcionam e os itens não
        /// mudam mais. Mudar o que se pede depois de o fornecedor abrir o link
        /// tornaria as propostas incomparáveis.
        /// </summary>
        public async Task Abrir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var cotacao = await contexto.Cotacoes
                .Include(c => c.Itens)
                .Include(c => c.Convites)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Cotação não encontrada.");

            if (cotacao.Itens.Count == 0)
                throw new InvalidOperationException("Não há o que cotar: adicione ao menos um item.");

            if (cotacao.Convites.Count == 0)
                throw new InvalidOperationException("Adicione ao menos um fornecedor.");

            cotacao.Status = StatusCotacao.Aberta;
            cotacao.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, Modulo,
                $"Cotação #{id} \"{cotacao.Titulo}\" aberta para {cotacao.Convites.Count} "
                + (cotacao.Convites.Count == 1 ? "fornecedor." : "fornecedores."),
                cotacao.CriadoPorId,
                entidade: nameof(Cotacao),
                entidadeId: id.ToString());
        }

        public async Task Encerrar(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var cotacao = await contexto.Cotacoes.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Cotação não encontrada.");

            cotacao.Status = StatusCotacao.Encerrada;
            cotacao.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Alteracao, Modulo,
                $"Cotação #{id} encerrada — os links pararam de aceitar resposta.",
                cotacao.CriadoPorId,
                entidade: nameof(Cotacao),
                entidadeId: id.ToString());
        }

        /// <summary>
        /// Destrava um convite já respondido. É ação do comprador, de propósito:
        /// o fornecedor não reabre a própria proposta.
        /// </summary>
        public async Task Reabrir(int conviteId, DateTime novaExpiracao)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var convite = await contexto.Convites.FirstOrDefaultAsync(c => c.Id == conviteId)
                ?? throw new InvalidOperationException("Convite não encontrado.");

            convite.Status = StatusConvite.Pendente;
            convite.ExpiraEm = novaExpiracao;

            await contexto.SaveChangesAsync();
        }

        public async Task<bool> Excluir(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var cotacao = await contexto.Cotacoes.FirstOrDefaultAsync(c => c.Id == id);
            if (cotacao is null)
                return false;

            if (cotacao.Status != StatusCotacao.Rascunho)
                throw new InvalidOperationException("Só rascunhos podem ser excluídos.");

            contexto.Cotacoes.Remove(cotacao);
            await contexto.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// A rodada com tudo que o mapa comparativo precisa: itens, convites e
        /// as propostas de todos. Aqui o cruzamento é o ponto — é a tela do
        /// COMPRADOR, autenticada, ao contrário da do fornecedor.
        /// </summary>
        public async Task<Cotacao?> ObterParaMapa(int id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Cotacoes
                .AsNoTracking()
                .Include(c => c.Itens).ThenInclude(i => i.Produto)
                .Include(c => c.Itens).ThenInclude(i => i.Propostas)
                .Include(c => c.Convites).ThenInclude(f => f.Pessoa)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Registra o vencedor de um item. O motivo é obrigatório quando o
        /// escolhido NÃO é o menor preço — e quem decide se é o menor é esta
        /// função, comparando as propostas gravadas, não a tela.
        /// </summary>
        public async Task EscolherVencedor(int cotacaoItemId, int conviteId, string? motivo, Guid escolhidoPorId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var item = await contexto.CotacaoItens
                .Include(i => i.Propostas)
                .FirstOrDefaultAsync(i => i.Id == cotacaoItemId)
                ?? throw new InvalidOperationException("Item da cotação não encontrado.");

            var escolhida = item.Propostas.FirstOrDefault(p => p.ConviteFornecedorId == conviteId)
                ?? throw new InvalidOperationException("Este fornecedor não cotou este item.");

            if (escolhida.PrecoUnitario is null)
                throw new InvalidOperationException("Este fornecedor não precificou este item.");

            var menorPreco = item.Propostas
                .Where(p => p.PrecoUnitario is not null)
                .Min(p => p.PrecoUnitario);

            if (escolhida.PrecoUnitario > menorPreco && string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException(
                    "Escolher quem não tem o menor preço exige um motivo.");

            item.ConviteVencedorId = conviteId;
            item.MotivoEscolha = motivo;
            item.EscolhidoPorId = escolhidoPorId;
            item.EscolhidoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            // O log guarda o preço escolhido e o menor da rodada: é o par que
            // permite auditar a decisão sem recalcular nada depois.
            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Escolha, Modulo,
                $"Vencedor definido no item #{cotacaoItemId} por {escolhida.PrecoUnitario:C2}"
                + (escolhida.PrecoUnitario > menorPreco
                    ? $" — não era o menor preço ({menorPreco:C2}). Motivo: {motivo}"
                    : " (menor preço)."),
                escolhidoPorId,
                entidade: nameof(CotacaoItem),
                entidadeId: cotacaoItemId.ToString());
        }

        /// <summary>
        /// Avisa os vencedores e reabre o link deles na segunda fase, para
        /// enviarem a NF-e.
        ///
        /// Só quem ganhou algum item entra: quem perdeu continua travado no
        /// estado Respondido, e o link dele não vira canal de envio de nota.
        /// </summary>
        /// <summary>Quantos foram avisados e quantos e-mails saíram de fato. Os
        /// dois números diferem quando o SMTP não está configurado ou recusa —
        /// e a tela precisa dizer isso, senão o comprador acha que avisou.</summary>
        public sealed record ResultadoAnuncio(List<ConviteFornecedor> Anunciados, int EmailsEnviados);

        /// <param name="urlBase">
        /// Origem da aplicação, para montar o link do fornecedor. Vem da tela
        /// porque é o navegador que sabe por qual endereço o sistema é acessado
        /// — o servidor não tem como adivinhar.
        /// </param>
        public async Task<ResultadoAnuncio> AnunciarVencedores(
            int cotacaoId, DateTime prazoParaNota, Guid anunciadoPorId, string urlBase)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var cotacao = await contexto.Cotacoes
                .Include(c => c.Itens)
                .Include(c => c.Convites).ThenInclude(f => f.Pessoa)
                .FirstOrDefaultAsync(c => c.Id == cotacaoId)
                ?? throw new InvalidOperationException("Cotação não encontrada.");

            var vencedores = cotacao.Itens
                .Where(i => i.ConviteVencedorId is not null)
                .Select(i => i.ConviteVencedorId!.Value)
                .Distinct()
                .ToHashSet();

            if (vencedores.Count == 0)
                throw new InvalidOperationException(
                    "Nenhum vencedor escolhido ainda. Defina os vencedores no mapa comparativo.");

            var anunciados = new List<ConviteFornecedor>();

            foreach (var convite in cotacao.Convites.Where(c => vencedores.Contains(c.Id)))
            {
                // Nota já enviada não volta para "aguardando nota": reanunciar
                // uma rodada não pode desfazer o que o fornecedor já mandou.
                if (convite.Status == StatusConvite.NotaEnviada)
                    continue;

                convite.Status = StatusConvite.Vencedor;
                convite.ExpiraEm = prazoParaNota;

                anunciados.Add(convite);
            }

            // Encerra a rodada: com vencedor definido, aceitar proposta nova
            // tornaria a decisão já tomada incomparável.
            cotacao.Status = StatusCotacao.Encerrada;
            cotacao.ModificadoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Envio, Modulo,
                $"Cotação #{cotacaoId}: {anunciados.Count} "
                + (anunciados.Count == 1 ? "fornecedor avisado" : "fornecedores avisados")
                + $" da vitória. Prazo para a NF-e: {prazoParaNota.ToLocalTime():dd/MM/yyyy}.",
                anunciadoPorId,
                entidade: nameof(Cotacao),
                entidadeId: cotacaoId.ToString());

            // O e-mail sai DEPOIS do SaveChanges: se a gravação falhasse, o
            // fornecedor teria recebido aviso de uma vitória que não existe.
            var enviados = 0;
            var comprador = await Comprador(cotacao.CriadoPorId);

            foreach (var convite in anunciados.Where(c => !string.IsNullOrWhiteSpace(c.Email)))
            {
                var link = $"{urlBase.TrimEnd('/')}/cotacao/{convite.Token}";

                if (await _emails.EnviarAvisoDeVitoria(
                        convite.Email, cotacaoId, cotacao.Titulo, link, prazoParaNota, comprador))
                    enviados++;
            }

            // Falha de envio não desfaz o anúncio — o link já está liberado e o
            // comprador pode mandar o endereço manualmente. Mas fica registrado.
            if (enviados < anunciados.Count)
                await _logs.Registrar(
                    Erp.Model.Log.TipoAcao.Erro, Modulo,
                    $"Cotação #{cotacaoId}: {anunciados.Count - enviados} de {anunciados.Count} "
                    + "avisos de vitória NÃO foram enviados por e-mail"
                    + (_emails.Configurado ? " (o servidor recusou)." : " (SMTP não configurado)."),
                    anunciadoPorId,
                    entidade: nameof(Cotacao),
                    entidadeId: cotacaoId.ToString());

            return new ResultadoAnuncio(anunciados, enviados);
        }

        /// <summary>Desfaz a escolha de um item, para refazer a decisão.</summary>
        public async Task LimparVencedor(int cotacaoItemId)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var item = await contexto.CotacaoItens.FirstOrDefaultAsync(i => i.Id == cotacaoItemId);
            if (item is null)
                return;

            item.ConviteVencedorId = null;
            item.MotivoEscolha = null;
            item.EscolhidoPorId = null;
            item.EscolhidoEm = null;

            await contexto.SaveChangesAsync();
        }

        /// <summary>
        /// Itens já aprovados que ainda não entraram em nenhuma cotação. É o que
        /// alimenta o "importar itens aprovados": cotar duas vezes o mesmo item
        /// aprovado geraria compra dobrada.
        /// </summary>
        public async Task<List<Erp.Model.Solicitacao.ItemSolicitacao>> ItensAprovadosDisponiveis()
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var jaCotados = await contexto.CotacaoItens
                .Where(i => i.ItemSolicitacaoId != null)
                .Select(i => i.ItemSolicitacaoId!.Value)
                .ToListAsync();

            return await contexto.ItensSolicitacao
                .AsNoTracking()
                .Include(i => i.Produto)
                .Include(i => i.CentroCusto)
                .Include(i => i.Solicitacao)
                .Where(i => i.Status == Erp.Model.Solicitacao.StatusItem.Aprovado
                         && !jaCotados.Contains(i.Id))
                .OrderBy(i => i.SolicitacaoId)
                .ToListAsync();
        }

        // ------------------------------------------------------------------
        // PÚBLICOS — atendem a tela do fornecedor, que só tem o token.
        // ------------------------------------------------------------------

        /// <summary>
        /// O convite de um token, com a cotação e os itens a precificar.
        ///
        /// Carrega DELIBERADAMENTE só as propostas deste convite: incluir os
        /// outros convites da rodada entregaria ao fornecedor o preço do
        /// concorrente. Token inexistente devolve nulo — a tela não diz se o
        /// token é inválido ou de outra rodada.
        /// </summary>
        public async Task<ConviteFornecedor?> ObterPorToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Convites
                .AsNoTracking()
                .Include(c => c.Propostas)
                .Include(c => c.Cotacao!)
                    .ThenInclude(co => co.Itens)
                    .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(c => c.Token == token);
        }

        /// <summary>
        /// Grava a resposta do fornecedor. A chave continua sendo o TOKEN, não o
        /// id do convite: assim uma tela adulterada não consegue responder pelo
        /// vizinho. Recusa fora do prazo, com a rodada encerrada ou já
        /// respondida — a validação vale aqui, não só no botão.
        /// </summary>
        public async Task ResponderPorToken(
            string token,
            ConviteFornecedor dadosDeclarados,
            IEnumerable<PropostaItem> propostas)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var convite = await contexto.Convites
                .Include(c => c.Cotacao!).ThenInclude(co => co.Itens)
                .Include(c => c.Propostas)
                .FirstOrDefaultAsync(c => c.Token == token)
                ?? throw new InvalidOperationException("Link inválido.");

            if (convite.Status == StatusConvite.Respondido)
                throw new InvalidOperationException("Esta proposta já foi enviada.");

            if (convite.Status == StatusConvite.Cancelado)
                throw new InvalidOperationException("Este convite foi cancelado.");

            if (convite.Cotacao?.Status != StatusCotacao.Aberta)
                throw new InvalidOperationException("Esta cotação não está aberta para respostas.");

            if (convite.ExpiraEm < DateTime.UtcNow)
                throw new InvalidOperationException("O prazo para responder venceu.");

            convite.Cnpj = dadosDeclarados.Cnpj;
            convite.RazaoSocial = dadosDeclarados.RazaoSocial;
            convite.Responsavel = dadosDeclarados.Responsavel;
            convite.Telefone = dadosDeclarados.Telefone;
            convite.CondicaoPagamento = dadosDeclarados.CondicaoPagamento;
            convite.Frete = dadosDeclarados.Frete;

            // Só itens DESTA cotação entram. Sem esse filtro, um id forjado no
            // POST gravaria preço em item de outra rodada.
            var itensValidos = convite.Cotacao.Itens.Select(i => i.Id).ToHashSet();

            foreach (var enviada in propostas.Where(p => itensValidos.Contains(p.CotacaoItemId)))
            {
                var existente = convite.Propostas.FirstOrDefault(p => p.CotacaoItemId == enviada.CotacaoItemId);

                if (existente is null)
                {
                    convite.Propostas.Add(new PropostaItem
                    {
                        CotacaoItemId = enviada.CotacaoItemId,
                        PrecoUnitario = enviada.PrecoUnitario,
                        PrazoEntregaDias = enviada.PrazoEntregaDias,
                        Observacao = enviada.Observacao,
                    });
                    continue;
                }

                existente.PrecoUnitario = enviada.PrecoUnitario;
                existente.PrazoEntregaDias = enviada.PrazoEntregaDias;
                existente.Observacao = enviada.Observacao;
            }

            // Trava ao enviar: reabrir exige ação do comprador.
            convite.Status = StatusConvite.Respondido;
            convite.RespondidoEm = DateTime.UtcNow;

            await contexto.SaveChangesAsync();

            // Sem usuário: veio de fora do login. É quem o fornecedor DECLAROU
            // ser — por isso o nome vai na descrição, e não no campo de usuário.
            await _logs.Registrar(
                Erp.Model.Log.TipoAcao.Resposta, Modulo,
                $"Proposta recebida na cotação #{convite.CotacaoId} de "
                + $"{convite.RazaoSocial} (CNPJ {convite.Cnpj}), respondida por {convite.Responsavel}.",
                entidade: nameof(ConviteFornecedor),
                entidadeId: convite.Id.ToString());

            // Avisa quem montou a rodada: é ele que decide quando fechar e
            // comparar, e a resposta chega sem ele estar na tela.
            if (convite.Cotacao?.CriadoPorId is { } comprador)
                await _avisos.Criar(
                    comprador,
                    "Proposta recebida",
                    $"{convite.RazaoSocial} respondeu a cotação #{convite.CotacaoId}.",
                    "inbox",
                    "/home/cotacoes");
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// 32 bytes de aleatoriedade criptográfica em base64url. Guid seria
        /// menor e previsível demais para uma URL que é a própria credencial.
        /// </summary>
        public static string GerarToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static void PrepararConvite(ConviteFornecedor convite, DateTime? prazo)
        {
            if (string.IsNullOrWhiteSpace(convite.Token))
                convite.Token = GerarToken();

            if (convite.ExpiraEm == default)
                convite.ExpiraEm = prazo ?? DateTime.UtcNow.AddDays(7);

            convite.Pessoa = null;
            convite.Cotacao = null;
        }

        private static void SincronizarItens(AppDbContext contexto, Cotacao gravada, Cotacao daTela)
        {
            var idsNaTela = daTela.Itens.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

            foreach (var removido in gravada.Itens.Where(i => !idsNaTela.Contains(i.Id)).ToList())
                contexto.CotacaoItens.Remove(removido);

            foreach (var item in daTela.Itens)
            {
                var alvo = item.Id == 0 ? null : gravada.Itens.FirstOrDefault(i => i.Id == item.Id);

                if (alvo is null)
                {
                    gravada.Itens.Add(new CotacaoItem
                    {
                        ProdutoId = item.ProdutoId,
                        Quantidade = item.Quantidade,
                        ItemSolicitacaoId = item.ItemSolicitacaoId,
                    });
                    continue;
                }

                alvo.ProdutoId = item.ProdutoId;
                alvo.Quantidade = item.Quantidade;
                alvo.ItemSolicitacaoId = item.ItemSolicitacaoId;
            }
        }

        private static void SincronizarConvites(AppDbContext contexto, Cotacao gravada, Cotacao daTela)
        {
            var idsNaTela = daTela.Convites.Where(c => c.Id != 0).Select(c => c.Id).ToHashSet();

            foreach (var removido in gravada.Convites
                                            .Where(c => !idsNaTela.Contains(c.Id)
                                                     && c.Status != StatusConvite.Respondido)
                                            .ToList())
                contexto.Convites.Remove(removido);

            foreach (var convite in daTela.Convites)
            {
                var alvo = convite.Id == 0 ? null : gravada.Convites.FirstOrDefault(c => c.Id == convite.Id);

                if (alvo is null)
                {
                    var novo = new ConviteFornecedor
                    {
                        PessoaId = convite.PessoaId,
                        Email = convite.Email,
                    };

                    PrepararConvite(novo, daTela.PrazoResposta);
                    gravada.Convites.Add(novo);
                    continue;
                }

                if (alvo.Status == StatusConvite.Respondido)
                    continue;

                alvo.PessoaId = convite.PessoaId;
                alvo.Email = convite.Email;
                alvo.ExpiraEm = daTela.PrazoResposta ?? alvo.ExpiraEm;
            }
        }

        /// <summary>As navegações vêm preenchidas da tela; mandá-las ao EF na
        /// inserção faria ele tentar inserir produto e pessoa de novo.</summary>
        private static void LimparNavegacoes(Cotacao cotacao)
        {
            cotacao.CriadoPor = null;

            foreach (var item in cotacao.Itens)
            {
                item.Produto = null;
                item.Cotacao = null;
                item.ItemSolicitacao = null;
            }
        }
    }
}
