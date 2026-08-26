namespace Erp.Model.Produto
{
    /// <summary>
    /// Item comprável ou estocável. Os três números do fim são o que dispara
    /// todo o fluxo de compras.
    /// </summary>
    public class Produto
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        /// <summary>UN, CX, KG, M… texto livre por enquanto.</summary>
        public string Unidade { get; set; } = "UN";

        public decimal SaldoAtual { get; set; }

        /// <summary>Piso que não deveria ser cruzado. Serve de referência e de
        /// alerta crítico — não é ele que dispara a compra.</summary>
        public decimal EstoqueMinimo { get; set; }

        /// <summary>Nível que dispara a necessidade de compra. Fica ACIMA do
        /// mínimo: mínimo + consumo médio diário × prazo de entrega, pra
        /// mercadoria chegar antes de o saldo encostar no piso.</summary>
        public decimal PontoPedido { get; set; }

        /// <summary>Prazo típico de entrega, em dias. Entra no cálculo sugerido
        /// do ponto de pedido.</summary>
        public int PrazoEntregaDias { get; set; }

        public decimal ConsumoMedioDiario { get; set; }

        /// <summary>Fornecedor habitual (Pessoa com Tipo = Fornecedor). Vira
        /// sugestão na hora de montar a cotação.</summary>
        public int? FornecedorPadraoId { get; set; }

        public Erp.Model.Pessoa.Pessoa? FornecedorPadrao { get; set; }

        public int? CentroCustoPadraoId { get; set; }

        public Erp.Model.CentroCusto.CentroCusto? CentroCustoPadrao { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        /// <summary>Ponto de pedido sugerido pela fórmula, pra tela oferecer o
        /// número em vez de o usuário adivinhar.</summary>
        public decimal PontoPedidoSugerido =>
            EstoqueMinimo + (ConsumoMedioDiario * PrazoEntregaDias);

        /// <summary>Saldo abaixo do ponto de pedido: é hora de comprar.</summary>
        public bool PrecisaRepor => SaldoAtual <= PontoPedido;
    }
}
