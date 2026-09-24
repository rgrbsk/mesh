namespace Erp.Model.Compra
{
    /// <summary>
    /// O pedido formal enviado a UM fornecedor. É a peça central do
    /// three-way match: a nota recebida e o recebimento físico são ambos
    /// conferidos contra ela.
    ///
    /// Uma cotação com vencedores divididos gera uma ordem POR FORNECEDOR —
    /// misturar fornecedores num pedido só tornaria impossível enviá-lo.
    /// </summary>
    public class OrdemCompra
    {
        public int Id { get; set; }

        /// <summary>
        /// Número legível, no formato OC-AAAA-0000. É o que o fornecedor cita
        /// no e-mail e no documento fiscal, então não pode ser o id interno.
        /// </summary>
        public string Numero { get; set; } = string.Empty;

        public int FornecedorId { get; set; }

        public Erp.Model.Pessoa.Pessoa? Fornecedor { get; set; }

        /// <summary>Rodada que originou o pedido. Nula quando o comprador
        /// emitiu uma ordem avulsa, sem cotação.</summary>
        public int? CotacaoId { get; set; }

        public Erp.Model.Cotacao.Cotacao? Cotacao { get; set; }

        public StatusOrdemCompra Status { get; set; } = StatusOrdemCompra.Rascunho;

        public DateTime EmitidaEm { get; set; } = DateTime.UtcNow;

        /// <summary>Quando saiu para o fornecedor. Nulo enquanto rascunho.</summary>
        public DateTime? EnviadaEm { get; set; }

        public DateTime? PrazoEntrega { get; set; }

        /// <summary>Copiados da proposta vencedora, para o pedido registrar o
        /// que foi acordado e não depender da cotação continuar existindo.</summary>
        public string CondicaoPagamento { get; set; } = string.Empty;

        public string Frete { get; set; } = string.Empty;

        public string Observacao { get; set; } = string.Empty;

        /// <summary>Preenchido ao cancelar. Cancelamento sem motivo não diz
        /// nada a quem consultar o histórico depois.</summary>
        public string? MotivoCancelamento { get; set; }

        public Guid? CriadoPorId { get; set; }

        public Erp.Model.Usuario.Usuario? CriadoPor { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public List<ItemOrdemCompra> Itens { get; set; } = new();

        /// <summary>Rascunho é o único estado que ainda se edita.</summary>
        public bool Editavel => Status == StatusOrdemCompra.Rascunho;

        /// <summary>Aceita recebimento enquanto houver saldo e não estiver
        /// cancelada ou parada em rascunho.</summary>
        public bool AceitaRecebimento =>
            Status is StatusOrdemCompra.Enviada or StatusOrdemCompra.ParcialmenteRecebida;

        public decimal ValorTotal => Itens.Sum(i => i.Total);

        public decimal ValorRecebido => Itens.Sum(i => i.QuantidadeRecebida * i.PrecoUnitario);

        public bool TotalmenteAtendida => Itens.Count > 0 && Itens.All(i => i.Atendido);

        public bool TemRecebimentoParcial => Itens.Any(i => i.QuantidadeRecebida > 0) && !TotalmenteAtendida;
    }
}
