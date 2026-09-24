using Erp.Model.Acesso;
using Erp.Model.Usuario;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Erp.Data
{
    /// <summary>
    /// Contexto único da aplicação (single database — uma connection string,
    /// sem particionamento por tenant).
    /// </summary>
    public class AppDbContext : IdentityDbContext<Usuario, Papel, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Erp.Model.Empresa.Empresa> Empresas => Set<Erp.Model.Empresa.Empresa>();

        public DbSet<Erp.Model.Usuario.Usuario> Usuarios => Set<Erp.Model.Usuario.Usuario>();

        public DbSet<Erp.Model.Acesso.Papel> Papeis => Set<Erp.Model.Acesso.Papel>();

        public DbSet<Erp.Model.Cidades.Cidade> Cidades => Set<Erp.Model.Cidades.Cidade>();

        public DbSet<Erp.Model.Pessoa.Pessoa> Pessoas => Set<Erp.Model.Pessoa.Pessoa>();

        public DbSet<Erp.Model.CentroCusto.CentroCusto> CentrosCusto => Set<Erp.Model.CentroCusto.CentroCusto>();

        public DbSet<Erp.Model.Produto.Produto> Produtos => Set<Erp.Model.Produto.Produto>();

        public DbSet<Erp.Model.Solicitacao.SolicitacaoCompra> Solicitacoes => Set<Erp.Model.Solicitacao.SolicitacaoCompra>();

        public DbSet<Erp.Model.Solicitacao.ItemSolicitacao> ItensSolicitacao => Set<Erp.Model.Solicitacao.ItemSolicitacao>();

        public DbSet<Erp.Model.Etapa.Etapa> Etapas => Set<Erp.Model.Etapa.Etapa>();

        public DbSet<Erp.Model.Cotacao.Cotacao> Cotacoes => Set<Erp.Model.Cotacao.Cotacao>();

        public DbSet<Erp.Model.Cotacao.CotacaoItem> CotacaoItens => Set<Erp.Model.Cotacao.CotacaoItem>();

        public DbSet<Erp.Model.Cotacao.ConviteFornecedor> Convites => Set<Erp.Model.Cotacao.ConviteFornecedor>();

        public DbSet<Erp.Model.Cotacao.PropostaItem> Propostas => Set<Erp.Model.Cotacao.PropostaItem>();

        public DbSet<Erp.Model.Produto.ProdutoFornecedor> ProdutosFornecedor => Set<Erp.Model.Produto.ProdutoFornecedor>();

        public DbSet<Erp.Model.Fiscal.NotaFiscal> NotasFiscais => Set<Erp.Model.Fiscal.NotaFiscal>();

        public DbSet<Erp.Model.Fiscal.NotaFiscalItem> NotaFiscalItens => Set<Erp.Model.Fiscal.NotaFiscalItem>();

        public DbSet<Erp.Model.Log.RegistroLog> Logs => Set<Erp.Model.Log.RegistroLog>();

        public DbSet<Erp.Model.Log.LogSistema> LogsSistema => Set<Erp.Model.Log.LogSistema>();

        public DbSet<Erp.Model.Notificacao.Notificacao> Notificacoes => Set<Erp.Model.Notificacao.Notificacao>();

        public DbSet<Erp.Model.Compra.OrdemCompra> OrdensCompra => Set<Erp.Model.Compra.OrdemCompra>();

        public DbSet<Erp.Model.Compra.ItemOrdemCompra> ItensOrdemCompra => Set<Erp.Model.Compra.ItemOrdemCompra>();

        public DbSet<Erp.Model.Recebimento.Recebimento> Recebimentos => Set<Erp.Model.Recebimento.Recebimento>();

        public DbSet<Erp.Model.Recebimento.ItemRecebimento> ItensRecebimento => Set<Erp.Model.Recebimento.ItemRecebimento>();

        public DbSet<Erp.Model.Orcamento.Orcamento> Orcamentos => Set<Erp.Model.Orcamento.Orcamento>();

        public DbSet<Erp.Model.Aprovacao.AlcadaAprovacao> Alcadas => Set<Erp.Model.Aprovacao.AlcadaAprovacao>();

        public DbSet<Erp.Model.Aprovacao.DelegacaoAprovacao> Delegacoes => Set<Erp.Model.Aprovacao.DelegacaoAprovacao>();

        public DbSet<Erp.Model.Aprovacao.AprovacaoItem> AprovacoesItem => Set<Erp.Model.Aprovacao.AprovacaoItem>();

        public DbSet<Erp.Model.Financeiro.TituloPagar> TitulosPagar => Set<Erp.Model.Financeiro.TituloPagar>();

        public DbSet<Erp.Model.Financeiro.BaixaTitulo> BaixasTitulo => Set<Erp.Model.Financeiro.BaixaTitulo>();



        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Cidade é referência: apagar uma cidade não pode levar junto os
            // cadastros que apontam pra ela (o default do EF aqui era Cascade).
            mb.Entity<Erp.Model.Pessoa.Pessoa>()
              .HasOne(p => p.Cidade)
              .WithMany()
              .HasForeignKey(p => p.CidadeId)
              .OnDelete(DeleteBehavior.Restrict);

            // Código é identificador de negócio: duplicar cria dois cadastros
            // pro mesmo item, caro de desfazer depois que há movimento.
            mb.Entity<Erp.Model.Produto.Produto>()
              .HasIndex(p => p.Codigo)
              .IsUnique();

            mb.Entity<Erp.Model.CentroCusto.CentroCusto>()
              .HasIndex(c => c.Codigo)
              .IsUnique();

            // Auto-relacionamento da árvore. Restrict de propósito: apagar um
            // centro que tem filhos tem que falhar, não levar a subárvore junto.
            mb.Entity<Erp.Model.CentroCusto.CentroCusto>()
              .HasOne(c => c.Pai)
              .WithMany(c => c.Filhos)
              .HasForeignKey(c => c.PaiId)
              .OnDelete(DeleteBehavior.Restrict);

            // Item não existe sem cabeçalho: apagar a solicitação leva os itens.
            // Já produto e centro de custo são referência — Restrict, senão
            // excluir um centro apagaria itens de solicitações antigas.
            mb.Entity<Erp.Model.Solicitacao.ItemSolicitacao>()
              .HasOne(i => i.Solicitacao)
              .WithMany(s => s.Itens)
              .HasForeignKey(i => i.SolicitacaoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Erp.Model.Solicitacao.ItemSolicitacao>()
              .HasOne(i => i.Produto)
              .WithMany()
              .HasForeignKey(i => i.ProdutoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Solicitacao.ItemSolicitacao>()
              .HasOne(i => i.CentroCusto)
              .WithMany()
              .HasForeignKey(i => i.CentroCustoId)
              .OnDelete(DeleteBehavior.Restrict);

            // Etapa é referência: desativar/apagar uma etapa não pode levar
            // junto as solicitações que estavam nela.
            mb.Entity<Erp.Model.Solicitacao.SolicitacaoCompra>()
              .HasOne(s => s.Etapa)
              .WithMany()
              .HasForeignKey(s => s.EtapaId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Solicitacao.SolicitacaoCompra>()
              .HasOne(s => s.Solicitante)
              .WithMany()
              .HasForeignKey(s => s.SolicitanteId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Solicitacao.ItemSolicitacao>()
              .HasOne(i => i.DecididoPor)
              .WithMany()
              .HasForeignKey(i => i.DecididoPorId)
              .OnDelete(DeleteBehavior.Restrict);

            // O token é a credencial do fornecedor: a busca da tela pública é
            // POR ELE, e duas linhas com o mesmo token dariam acesso cruzado.
            mb.Entity<Erp.Model.Cotacao.ConviteFornecedor>()
              .HasIndex(c => c.Token)
              .IsUnique();

            // Itens e convites não existem sem a cotação; as propostas não
            // existem sem o convite. Apagar a rodada leva tudo junto.
            mb.Entity<Erp.Model.Cotacao.CotacaoItem>()
              .HasOne(i => i.Cotacao)
              .WithMany(c => c.Itens)
              .HasForeignKey(i => i.CotacaoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Erp.Model.Cotacao.ConviteFornecedor>()
              .HasOne(c => c.Cotacao)
              .WithMany(c => c.Convites)
              .HasForeignKey(c => c.CotacaoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Erp.Model.Cotacao.PropostaItem>()
              .HasOne(p => p.ConviteFornecedor)
              .WithMany(c => c.Propostas)
              .HasForeignKey(p => p.ConviteFornecedorId)
              .OnDelete(DeleteBehavior.Cascade);

            // Já produto, pessoa e item de solicitação são referência.
            mb.Entity<Erp.Model.Cotacao.PropostaItem>()
              .HasOne(p => p.CotacaoItem)
              .WithMany(i => i.Propostas)
              .HasForeignKey(p => p.CotacaoItemId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Erp.Model.Cotacao.CotacaoItem>()
              .HasOne(i => i.Produto)
              .WithMany()
              .HasForeignKey(i => i.ProdutoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Cotacao.CotacaoItem>()
              .HasOne(i => i.ItemSolicitacao)
              .WithMany()
              .HasForeignKey(i => i.ItemSolicitacaoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Cotacao.ConviteFornecedor>()
              .HasOne(c => c.Pessoa)
              .WithMany()
              .HasForeignKey(c => c.PessoaId)
              .OnDelete(DeleteBehavior.Restrict);

            // O vencedor é referência ao convite, e apagar um convite não pode
            // levar junto o item cotado — por isso Restrict, não Cascade.
            mb.Entity<Erp.Model.Cotacao.CotacaoItem>()
              .HasOne(i => i.ConviteVencedor)
              .WithMany()
              .HasForeignKey(i => i.ConviteVencedorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Cotacao.CotacaoItem>()
              .HasOne(i => i.EscolhidoPor)
              .WithMany()
              .HasForeignKey(i => i.EscolhidoPorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Cotacao.Cotacao>()
              .HasOne(c => c.CriadoPor)
              .WithMany()
              .HasForeignKey(c => c.CriadoPorId)
              .OnDelete(DeleteBehavior.Restrict);

            // Propriedades calculadas não viram coluna.
            mb.Entity<Erp.Model.Produto.Produto>().Ignore(p => p.PontoPedidoSugerido);
            mb.Entity<Erp.Model.Produto.Produto>().Ignore(p => p.PrecisaRepor);
            mb.Entity<Erp.Model.Solicitacao.SolicitacaoCompra>().Ignore(s => s.Editavel);
            mb.Entity<Erp.Model.Solicitacao.SolicitacaoCompra>().Ignore(s => s.QuantidadeTotal);
            mb.Entity<Erp.Model.Solicitacao.SolicitacaoCompra>().Ignore(s => s.PrazoMaisCurto);
            mb.Entity<Erp.Model.Cotacao.Cotacao>().Ignore(c => c.Editavel);
            mb.Entity<Erp.Model.Cotacao.Cotacao>().Ignore(c => c.Respostas);
            mb.Entity<Erp.Model.Cotacao.PropostaItem>().Ignore(p => p.Total);
            mb.Entity<Erp.Model.Cotacao.ConviteFornecedor>().Ignore(c => c.Identificacao);
            // Um código por fornecedor: o mesmo cProd não pode apontar para dois
            // produtos nossos, senão o confronto não saberia qual usar.
            mb.Entity<Erp.Model.Produto.ProdutoFornecedor>()
              .HasIndex(d => new { d.FornecedorId, d.CodigoFornecedor })
              .IsUnique();

            mb.Entity<Erp.Model.Produto.ProdutoFornecedor>()
              .HasOne(d => d.Fornecedor)
              .WithMany()
              .HasForeignKey(d => d.FornecedorId)
              .OnDelete(DeleteBehavior.Cascade);

            // Produto é referência: apagar um produto não pode ser bloqueado
            // por um de-para, mas também não pode apagar o histórico — aqui
            // Cascade é o certo porque a linha SÓ existe para ligar os dois.
            mb.Entity<Erp.Model.Produto.ProdutoFornecedor>()
              .HasOne(d => d.Produto)
              .WithMany()
              .HasForeignKey(d => d.ProdutoId)
              .OnDelete(DeleteBehavior.Cascade);

            // Chave de acesso é a identidade nacional da nota: única, para o
            // mesmo arquivo não entrar duas vezes e dobrar uma compra.
            mb.Entity<Erp.Model.Fiscal.NotaFiscal>()
              .HasIndex(n => n.Chave)
              .IsUnique();

            mb.Entity<Erp.Model.Fiscal.NotaFiscalItem>()
              .HasOne(i => i.NotaFiscal)
              .WithMany(n => n.Itens)
              .HasForeignKey(i => i.NotaFiscalId)
              .OnDelete(DeleteBehavior.Cascade);

            // Fornecedor, cotação e produto são referência: apagar qualquer um
            // deles não pode levar junto o documento fiscal.
            mb.Entity<Erp.Model.Fiscal.NotaFiscal>()
              .HasOne(n => n.Fornecedor)
              .WithMany()
              .HasForeignKey(n => n.FornecedorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Fiscal.NotaFiscal>()
              .HasOne(n => n.Cotacao)
              .WithMany()
              .HasForeignKey(n => n.CotacaoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Fiscal.NotaFiscalItem>()
              .HasOne(i => i.Produto)
              .WithMany()
              .HasForeignKey(i => i.ProdutoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Erp.Model.Fiscal.NotaFiscal>().Ignore(n => n.EhHomologacao);
            mb.Entity<Erp.Model.Log.RegistroLog>().Ignore(l => l.TemStackTrace);
            mb.Entity<Erp.Model.Log.LogSistema>().Ignore(l => l.TemStackTrace);

            mb.Entity<Erp.Model.Log.LogSistema>().HasIndex(l => l.Quando);

            // O sino consulta "não lidas deste usuário" a cada carga de página:
            // sem este índice é varredura de tabela em algo que roda o tempo todo.
            mb.Entity<Erp.Model.Notificacao.Notificacao>()
              .HasIndex(n => new { n.DestinatarioId, n.Lida });

            // Notificação é dirigida: sem o dono, ela não tem sentido — mas
            // apagar o usuário não pode falhar por causa de avisos antigos.
            mb.Entity<Erp.Model.Notificacao.Notificacao>()
              .HasOne(n => n.Destinatario)
              .WithMany()
              .HasForeignKey(n => n.DestinatarioId)
              .OnDelete(DeleteBehavior.Cascade);

            // Toda consulta da tela de logs ordena e filtra por data.
            mb.Entity<Erp.Model.Log.RegistroLog>().HasIndex(l => l.Quando);

            // Mapeamento dos módulos de ordem de compra, recebimento, orçamento,
            // alçada e contas a pagar. Fica ANTES do conversor de UTC abaixo para
            // que as entidades novas também passem por ele.
            MapeamentoNovosModulos.Configurar(mb);

            // Automatically treats Unspecified DateTimes as UTC when saving or reading
            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                            v => v
                        ));
                    }
                }
            }

        }
    }
}
