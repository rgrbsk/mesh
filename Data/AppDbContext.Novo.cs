// Mapeamento das entidades de ordem de compra, recebimento, orçamento, alçada e
// contas a pagar. Separado do AppDbContext para não misturar com o que já
// estava funcionando — o contexto só chama Configurar(mb) no fim do
// OnModelCreating.

using Erp.Model.Aprovacao;
using Erp.Model.Compra;
using Erp.Model.Financeiro;
using Erp.Model.Recebimento;
using Microsoft.EntityFrameworkCore;

namespace Erp.Data
{
    public static class MapeamentoNovosModulos
    {
        /// <summary>
        /// Chame no FIM do OnModelCreating, logo antes do laço que converte
        /// DateTime para UTC:
        ///
        ///     MapeamentoNovosModulos.Configurar(mb);
        ///
        /// O critério das exclusões segue o que já estava no contexto: cascata
        /// só quando o registro dependente não tem existência própria, e
        /// restrição quando o registro referenciado é dado de referência.
        /// </summary>
        public static void Configurar(ModelBuilder mb)
        {
            // ================= ORDEM DE COMPRA =================

            // O número é o que o fornecedor cita no e-mail e na nota. Dois
            // pedidos com o mesmo número tornariam a conferência ambígua.
            mb.Entity<OrdemCompra>()
              .HasIndex(o => o.Numero)
              .IsUnique();

            mb.Entity<OrdemCompra>()
              .HasOne(o => o.Fornecedor)
              .WithMany()
              .HasForeignKey(o => o.FornecedorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<OrdemCompra>()
              .HasOne(o => o.Cotacao)
              .WithMany()
              .HasForeignKey(o => o.CotacaoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<OrdemCompra>()
              .HasOne(o => o.CriadoPor)
              .WithMany()
              .HasForeignKey(o => o.CriadoPorId)
              .OnDelete(DeleteBehavior.Restrict);

            // Item não existe sem o pedido: apagar o rascunho leva os itens.
            mb.Entity<ItemOrdemCompra>()
              .HasOne(i => i.OrdemCompra)
              .WithMany(o => o.Itens)
              .HasForeignKey(i => i.OrdemCompraId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<ItemOrdemCompra>()
              .HasOne(i => i.Produto)
              .WithMany()
              .HasForeignKey(i => i.ProdutoId)
              .OnDelete(DeleteBehavior.Restrict);

            // O item de cotação é a origem do pedido — apagar a rodada não pode
            // levar junto o pedido que dela nasceu.
            mb.Entity<ItemOrdemCompra>()
              .HasOne(i => i.CotacaoItem)
              .WithMany()
              .HasForeignKey(i => i.CotacaoItemId)
              .OnDelete(DeleteBehavior.Restrict);

            // ================= RECEBIMENTO =================

            mb.Entity<Recebimento>()
              .HasIndex(r => r.Numero)
              .IsUnique();

            // Restrict, e não Cascade: pedido com entrada registrada não deve
            // ser apagável de jeito nenhum — a mercadoria está no depósito.
            mb.Entity<Recebimento>()
              .HasOne(r => r.OrdemCompra)
              .WithMany()
              .HasForeignKey(r => r.OrdemCompraId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Recebimento>()
              .HasOne(r => r.RecebidoPor)
              .WithMany()
              .HasForeignKey(r => r.RecebidoPorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<ItemRecebimento>()
              .HasOne(i => i.Recebimento)
              .WithMany(r => r.Itens)
              .HasForeignKey(i => i.RecebimentoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<ItemRecebimento>()
              .HasOne(i => i.ItemOrdemCompra)
              .WithMany()
              .HasForeignKey(i => i.ItemOrdemCompraId)
              .OnDelete(DeleteBehavior.Restrict);

            // A consulta do confronto filtra por pedido e por avaria.
            mb.Entity<ItemRecebimento>()
              .HasIndex(i => new { i.ItemOrdemCompraId, i.ComAvaria });

            // ================= ORÇAMENTO =================

            // Uma verba por centro e competência: duas linhas para o mesmo mês
            // fariam o consumo comparar com um teto indefinido.
            mb.Entity<Erp.Model.Orcamento.Orcamento>()
              .HasIndex(o => new { o.CentroCustoId, o.Ano, o.Mes })
              .IsUnique();

            mb.Entity<Erp.Model.Orcamento.Orcamento>()
              .HasOne(o => o.CentroCusto)
              .WithMany()
              .HasForeignKey(o => o.CentroCustoId)
              .OnDelete(DeleteBehavior.Cascade);

            // ================= ALÇADA =================

            // Dois degraus na mesma posição deixariam o roteamento ambíguo.
            mb.Entity<AlcadaAprovacao>()
              .HasIndex(a => new { a.CentroCustoId, a.Ordem })
              .IsUnique();

            mb.Entity<AlcadaAprovacao>()
              .HasOne(a => a.CentroCusto)
              .WithMany()
              .HasForeignKey(a => a.CentroCustoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<AlcadaAprovacao>()
              .HasOne(a => a.Aprovador)
              .WithMany()
              .HasForeignKey(a => a.AprovadorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<DelegacaoAprovacao>()
              .HasOne(d => d.Titular)
              .WithMany()
              .HasForeignKey(d => d.TitularId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<DelegacaoAprovacao>()
              .HasOne(d => d.Substituto)
              .WithMany()
              .HasForeignKey(d => d.SubstitutoId)
              .OnDelete(DeleteBehavior.Restrict);

            // O roteamento pergunta "quem eu substituo agora" a cada carga da fila.
            mb.Entity<DelegacaoAprovacao>()
              .HasIndex(d => new { d.SubstitutoId, d.Ativo });

            mb.Entity<DelegacaoAprovacao>()
              .HasIndex(d => new { d.TitularId, d.Ativo });

            // O passo da cadeia morre com o item que o originou.
            mb.Entity<AprovacaoItem>()
              .HasOne(a => a.ItemSolicitacao)
              .WithMany(i => i.Aprovacoes)
              .HasForeignKey(a => a.ItemSolicitacaoId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<AprovacaoItem>()
              .HasOne(a => a.Aprovador)
              .WithMany()
              .HasForeignKey(a => a.AprovadorId)
              .OnDelete(DeleteBehavior.Restrict);

            // ================= CONTAS A PAGAR =================

            // Número mais parcela identificam o título. O número sozinho não
            // serve: um documento parcelado repete o número em cada parcela.
            mb.Entity<TituloPagar>()
              .HasIndex(t => new { t.Numero, t.Parcela })
              .IsUnique();

            mb.Entity<TituloPagar>()
              .HasOne(t => t.Fornecedor)
              .WithMany()
              .HasForeignKey(t => t.FornecedorId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<TituloPagar>()
              .HasOne(t => t.NotaFiscal)
              .WithMany()
              .HasForeignKey(t => t.NotaFiscalId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<TituloPagar>()
              .HasOne(t => t.OrdemCompra)
              .WithMany()
              .HasForeignKey(t => t.OrdemCompraId)
              .OnDelete(DeleteBehavior.Restrict);

            // Toda listagem do módulo ordena por vencimento.
            mb.Entity<TituloPagar>()
              .HasIndex(t => new { t.Status, t.Vencimento });

            mb.Entity<BaixaTitulo>()
              .HasOne(b => b.TituloPagar)
              .WithMany(t => t.Baixas)
              .HasForeignKey(b => b.TituloPagarId)
              .OnDelete(DeleteBehavior.Cascade);

            // ================= NOTA FISCAL: TERCEIRA PONTA =================

            mb.Entity<Erp.Model.Fiscal.NotaFiscal>()
              .HasOne(n => n.OrdemCompra)
              .WithMany()
              .HasForeignKey(n => n.OrdemCompraId)
              .OnDelete(DeleteBehavior.Restrict);

            // ================= CALCULADAS =================
            // Propriedades derivadas não viram coluna.

            mb.Entity<OrdemCompra>().Ignore(o => o.Editavel);
            mb.Entity<OrdemCompra>().Ignore(o => o.AceitaRecebimento);
            mb.Entity<OrdemCompra>().Ignore(o => o.ValorTotal);
            mb.Entity<OrdemCompra>().Ignore(o => o.ValorRecebido);
            mb.Entity<OrdemCompra>().Ignore(o => o.TotalmenteAtendida);
            mb.Entity<OrdemCompra>().Ignore(o => o.TemRecebimentoParcial);

            mb.Entity<ItemOrdemCompra>().Ignore(i => i.Total);
            mb.Entity<ItemOrdemCompra>().Ignore(i => i.Pendente);
            mb.Entity<ItemOrdemCompra>().Ignore(i => i.Atendido);

            mb.Entity<Recebimento>().Ignore(r => r.QuantidadeTotal);
            mb.Entity<Recebimento>().Ignore(r => r.TemRessalva);

            mb.Entity<Erp.Model.Orcamento.Orcamento>().Ignore(o => o.Competencia);

            mb.Entity<AlcadaAprovacao>().Ignore(a => a.Ilimitado);
            mb.Entity<AprovacaoItem>().Ignore(a => a.PorSubstituto);

            mb.Entity<TituloPagar>().Ignore(t => t.Saldo);
            mb.Entity<TituloPagar>().Ignore(t => t.Vencido);
            mb.Entity<TituloPagar>().Ignore(t => t.DiasParaVencer);
            mb.Entity<TituloPagar>().Ignore(t => t.Identificacao);

            mb.Entity<Erp.Model.Solicitacao.ItemSolicitacao>().Ignore(i => i.PrecoEstimadoUnitario);
        }
    }
}
