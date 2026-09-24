namespace Erp.Model.Financeiro
{
    /// <summary>
    /// Obrigação de pagamento gerada a partir de um documento fiscal recebido.
    ///
    /// Os títulos nascem do CONFRONTO, não da nota crua: gerar contas a pagar
    /// de uma nota que ainda tem divergência é exatamente o erro que o
    /// three-way match existe para impedir.
    /// </summary>
    public class TituloPagar
    {
        public int Id { get; set; }

        /// <summary>Número legível, no formato TP-AAAA-0000/pp, onde pp é a
        /// parcela.</summary>
        public string Numero { get; set; } = string.Empty;

        public int FornecedorId { get; set; }

        public Erp.Model.Pessoa.Pessoa? Fornecedor { get; set; }

        /// <summary>Documento que originou o título. Nulo em título lançado à
        /// mão, sem nota.</summary>
        public int? NotaFiscalId { get; set; }

        public Erp.Model.Fiscal.NotaFiscal? NotaFiscal { get; set; }

        public int? OrdemCompraId { get; set; }

        public Erp.Model.Compra.OrdemCompra? OrdemCompra { get; set; }

        public DateTime Emissao { get; set; } = DateTime.UtcNow;

        public DateTime Vencimento { get; set; }

        public decimal Valor { get; set; }

        /// <summary>Soma das baixas. Gravado porque toda listagem mostra o
        /// saldo, e somar as baixas por linha custaria uma consulta por título.</summary>
        public decimal ValorPago { get; set; }

        public int Parcela { get; set; } = 1;

        public int TotalParcelas { get; set; } = 1;

        public StatusTitulo Status { get; set; } = StatusTitulo.Aberto;

        public string Observacao { get; set; } = string.Empty;

        public string? MotivoCancelamento { get; set; }

        public Guid? CriadoPorId { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public List<BaixaTitulo> Baixas { get; set; } = new();

        public decimal Saldo => Math.Max(0m, Valor - ValorPago);

        /// <summary>Vencido é o que passou da data E ainda tem saldo. Título
        /// pago fora do prazo não é problema em aberto.</summary>
        public bool Vencido => Status is StatusTitulo.Aberto or StatusTitulo.ParcialmentePago
                            && Vencimento.Date < DateTime.UtcNow.Date;

        public int DiasParaVencer => (Vencimento.Date - DateTime.UtcNow.Date).Days;

        public string Identificacao => TotalParcelas > 1
            ? $"{Numero} ({Parcela}/{TotalParcelas})"
            : Numero;
    }
}
