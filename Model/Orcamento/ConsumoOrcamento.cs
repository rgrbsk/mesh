namespace Erp.Model.Orcamento
{
    /// <summary>
    /// Fotografia do orçamento de um centro numa competência. Não é entidade
    /// persistida: é calculada sob demanda, porque congelar o consumo faria a
    /// tela mentir assim que uma ordem de compra fosse cancelada.
    ///
    /// São três números, e a distinção entre eles importa:
    /// PREVISTO é intenção (aprovado, ainda sem preço firme, avaliado pelo
    /// preço de referência do produto); COMPROMETIDO é obrigação assumida
    /// (ordem de compra emitida, a preço real); REALIZADO é o que já entrou
    /// com documento fiscal.
    /// </summary>
    public sealed record ConsumoOrcamento(
        int CentroCustoId,
        string CentroCustoNome,
        int Ano,
        int Mes,
        decimal Orcado,
        decimal Previsto,
        decimal Comprometido,
        decimal Realizado)
    {
        /// <summary>O que já saiu do bolso ou está prometido. É este o número
        /// que se compara com a verba.</summary>
        public decimal Consumido => Comprometido + Previsto;

        public decimal Saldo => Orcado - Consumido;

        public bool Estourado => Orcado > 0 && Consumido > Orcado;

        /// <summary>Percentual consumido, limitado a 100 para a barra não
        /// transbordar visualmente — o estouro é sinalizado pela cor.</summary>
        public int Percentual => Orcado <= 0
            ? 0
            : (int)Math.Min(100m, Math.Round(Consumido / Orcado * 100m));

        public int PercentualReal => Orcado <= 0
            ? 0
            : (int)Math.Round(Consumido / Orcado * 100m);

        public bool SemOrcamento => Orcado <= 0;
    }
}
