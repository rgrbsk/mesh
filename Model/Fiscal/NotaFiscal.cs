namespace Erp.Model.Fiscal
{
    /// <summary>
    /// Uma NF-e recebida do fornecedor. Guarda os campos que o confronto usa e o
    /// XML inteiro — o arquivo original é o documento fiscal, e qualquer coisa
    /// que a gente extraia dele é interpretação nossa.
    /// </summary>
    public class NotaFiscal
    {
        public int Id { get; set; }

        /// <summary>Chave de acesso, 44 dígitos. É a identidade da nota no país
        /// inteiro: índice único, para o mesmo arquivo não entrar duas vezes.</summary>
        public string Chave { get; set; } = string.Empty;

        public string Numero { get; set; } = string.Empty;

        public string Serie { get; set; } = string.Empty;

        public DateTime Emissao { get; set; }

        public string EmitenteCnpj { get; set; } = string.Empty;

        public string EmitenteNome { get; set; } = string.Empty;

        public string DestinatarioCnpj { get; set; } = string.Empty;

        public decimal ValorProdutos { get; set; }

        public decimal ValorFrete { get; set; }

        public decimal ValorTotal { get; set; }

        /// <summary>
        /// Ambiente declarado na nota: 1 produção, 2 homologação. Nota de
        /// homologação NÃO tem valor fiscal — importar uma sem avisar deixaria
        /// entrar no estoque uma compra que não existe.
        /// </summary>
        public int Ambiente { get; set; }

        /// <summary>Protocolo de autorização da SEFAZ. Vazio quando o arquivo é
        /// só a NFe assinada, sem o retorno.</summary>
        public string Protocolo { get; set; } = string.Empty;

        /// <summary>Fornecedor do nosso cadastro, casado pelo CNPJ do emitente.
        /// Nulo quando não há Pessoa com aquele CNPJ.</summary>
        public int? FornecedorId { get; set; }

        public Erp.Model.Pessoa.Pessoa? Fornecedor { get; set; }

        /// <summary>Cotação a que esta nota se refere, escolhida na importação.</summary>
        public int? CotacaoId { get; set; }

        public Erp.Model.Cotacao.Cotacao? Cotacao { get; set; }

        /// <summary>
        /// Ordem de compra contra a qual esta nota é conferida. É a terceira
        /// ponta do three-way match — sem ela, o confronto compara a nota com a
        /// cotação, que registra o que se pretendia comprar, e não o que foi
        /// efetivamente pedido.
        /// </summary>
        public int? OrdemCompraId { get; set; }

        public Erp.Model.Compra.OrdemCompra? OrdemCompra { get; set; }

        /// <summary>O arquivo como veio. Sem ele, uma divergência descoberta
        /// meses depois não teria como ser conferida contra a origem.</summary>
        public string Xml { get; set; } = string.Empty;

        public DateTime ImportadaEm { get; set; } = DateTime.UtcNow;

        public Guid? ImportadaPorId { get; set; }

        /// <summary>
        /// Marcada quando o confronto fechou e alguém liberou a nota para o
        /// financeiro. É a condição para gerar título a pagar: nota com
        /// divergência aberta não vira obrigação de pagamento.
        /// </summary>
        public bool LiberadaParaPagamento { get; set; }

        public DateTime? LiberadaEm { get; set; }

        public Guid? LiberadaPorId { get; set; }

        /// <summary>Justificativa de quem liberou apesar de divergência. Vazio
        /// quando o confronto fechou sem ressalva.</summary>
        public string? MotivoLiberacao { get; set; }

        public List<NotaFiscalItem> Itens { get; set; } = new();

        public bool EhHomologacao => Ambiente == 2;
    }
}
