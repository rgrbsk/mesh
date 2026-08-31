using Erp.Model.Cotacao;
using Erp.Model.Fiscal;

namespace Erp.Service.Fiscal
{
    public enum SituacaoConfronto
    {
        /// <summary>Quantidade e preço batem com o que foi cotado.</summary>
        Conferido = 0,

        /// <summary>Veio mais caro que o preço vencedor.</summary>
        PrecoAcima = 1,

        /// <summary>Veio mais barato. Não é problema, mas é divergência — e
        /// divergência não avisada vira surpresa no financeiro.</summary>
        PrecoAbaixo = 2,

        /// <summary>Quantidade diferente da comprada.</summary>
        QuantidadeDivergente = 3,

        /// <summary>Item na nota que não estava na compra.</summary>
        ForaDaCompra = 4,

        /// <summary>Item comprado que não veio nesta nota.</summary>
        NaoEntregue = 5,
    }

    /// <summary>Uma linha do mapa de confronto.</summary>
    public sealed record LinhaConfronto(
        SituacaoConfronto Situacao,
        string Descricao,
        string CodigoFornecedor,
        decimal QuantidadeNota,
        decimal? QuantidadeComprada,
        decimal ValorUnitarioNota,
        decimal? ValorUnitarioComprado,
        int? ProdutoId)
    {
        public decimal TotalNota => QuantidadeNota * ValorUnitarioNota;

        public decimal? TotalComprado => QuantidadeComprada is null || ValorUnitarioComprado is null
            ? null
            : QuantidadeComprada * ValorUnitarioComprado;

        /// <summary>Quanto a nota cobra a mais (positivo) ou a menos que o
        /// combinado. Nulo quando não há com o que comparar.</summary>
        public decimal? Diferenca => TotalComprado is null ? null : TotalNota - TotalComprado;

        public bool Divergente => Situacao != SituacaoConfronto.Conferido;
    }

    /// <summary>Resultado inteiro, com os totais que a tela mostra no rodapé.</summary>
    public sealed record ResultadoConfronto(List<LinhaConfronto> Linhas)
    {
        public decimal TotalNota => Linhas.Sum(l => l.TotalNota);

        public decimal TotalComprado => Linhas.Sum(l => l.TotalComprado ?? 0m);

        public decimal Diferenca => TotalNota - TotalComprado;

        public int Divergencias => Linhas.Count(l => l.Divergente);

        public bool Fecha => Divergencias == 0;
    }

    /// <summary>
    /// Compara a nota recebida com o que foi efetivamente comprado daquele
    /// fornecedor — quantidade e preço, item a item.
    ///
    /// Classe estática e pura: entra nota e itens comprados, sai o resultado.
    /// Nada de banco aqui, porque esta é a regra que mais merece teste e a que
    /// mais dói se estiver errada — é ela que autoriza pagar.
    /// </summary>
    public static class ConfrontoNfe
    {
        /// <summary>
        /// Tolerância de centavos no preço unitário. A NF-e traz o unitário com
        /// quatro casas e a proposta é gravada como o fornecedor digitou;
        /// arredondamento de um centavo não é divergência comercial.
        /// </summary>
        private const decimal ToleranciaPreco = 0.01m;

        /// <param name="dePara">
        /// Código do fornecedor → id do nosso produto, já normalizado em caixa
        /// alta. É a PRIMEIRA regra de casamento: o cProd da nota é o código
        /// interno de quem emitiu, e só o de-para sabe traduzi-lo.
        /// </param>
        public static ResultadoConfronto Comparar(
            NotaFiscal nota,
            IReadOnlyCollection<CotacaoItem> itensComprados,
            IReadOnlyDictionary<string, int>? dePara = null)
        {
            // O MESMO produto pode vir em várias linhas da nota (é o caso comum
            // de lote/validade diferentes). Comparar linha a linha acusaria
            // quantidade a menor em cada uma; o que vale é a soma por produto.
            var daNota = nota.Itens
                .GroupBy(i => Chave(i.CodigoFornecedor, i.Descricao))
                .Select(g => new
                {
                    Codigo = g.First().CodigoFornecedor,
                    Descricao = g.First().Descricao,
                    Quantidade = g.Sum(i => i.Quantidade),
                    // Preço unitário médio ponderado: se as linhas vierem com
                    // preços diferentes, é o que o fornecedor está cobrando de
                    // fato por unidade.
                    ValorUnitario = g.Sum(i => i.Quantidade) == 0
                        ? 0m
                        : g.Sum(i => i.ValorTotal) / g.Sum(i => i.Quantidade),
                    // Amarração já feita na tela vence; senão, consulta o
                    // de-para gravado para este fornecedor.
                    ProdutoId = g.Select(i => i.ProdutoId).FirstOrDefault(id => id is not null)
                             ?? DoDePara(dePara, g.First().CodigoFornecedor),
                })
                .ToList();

            var comprados = itensComprados
                .Select(item => new
                {
                    Item = item,
                    Codigo = item.Produto?.Codigo ?? "",
                    Descricao = item.Produto?.Descricao ?? "",
                    Preco = item.Propostas
                        .FirstOrDefault(p => p.ConviteFornecedorId == item.ConviteVencedorId)
                        ?.PrecoUnitario,
                })
                .ToList();

            var linhas = new List<LinhaConfronto>();
            var casados = new HashSet<int>();

            foreach (var linhaNota in daNota)
            {
                // Amarração por produto já feita na tela vence; depois código
                // igual; por último descrição igual. O código do fornecedor
                // quase nunca é o nosso, então na prática a amarração manual é
                // o caminho normal.
                var compra = comprados.FirstOrDefault(c =>
                                  linhaNota.ProdutoId is not null && c.Item.ProdutoId == linhaNota.ProdutoId)
                          ?? comprados.FirstOrDefault(c => Iguais(c.Codigo, linhaNota.Codigo))
                          ?? comprados.FirstOrDefault(c => Iguais(c.Descricao, linhaNota.Descricao));

                if (compra is null)
                {
                    linhas.Add(new LinhaConfronto(
                        SituacaoConfronto.ForaDaCompra, linhaNota.Descricao, linhaNota.Codigo,
                        linhaNota.Quantidade, null, linhaNota.ValorUnitario, null, linhaNota.ProdutoId));

                    continue;
                }

                casados.Add(compra.Item.Id);

                linhas.Add(new LinhaConfronto(
                    Classificar(linhaNota.Quantidade, compra.Item.Quantidade,
                                linhaNota.ValorUnitario, compra.Preco),
                    linhaNota.Descricao,
                    linhaNota.Codigo,
                    linhaNota.Quantidade,
                    compra.Item.Quantidade,
                    linhaNota.ValorUnitario,
                    compra.Preco,
                    compra.Item.ProdutoId));
            }

            // O que foi comprado e não veio nesta nota. Sem isto, entrega
            // parcial passaria como conferida — a nota bate consigo mesma.
            foreach (var pendente in comprados.Where(c => !casados.Contains(c.Item.Id)))
                linhas.Add(new LinhaConfronto(
                    SituacaoConfronto.NaoEntregue,
                    pendente.Descricao,
                    pendente.Codigo,
                    0m,
                    pendente.Item.Quantidade,
                    0m,
                    pendente.Preco,
                    pendente.Item.ProdutoId));

            return new ResultadoConfronto(linhas);
        }

        /// <summary>
        /// Quantidade tem precedência sobre preço: receber quantidade errada é
        /// problema de recebimento, e resolvê-lo muda o total de qualquer jeito.
        /// </summary>
        private static SituacaoConfronto Classificar(
            decimal quantidadeNota, decimal quantidadeComprada,
            decimal precoNota, decimal? precoComprado)
        {
            if (quantidadeNota != quantidadeComprada)
                return SituacaoConfronto.QuantidadeDivergente;

            if (precoComprado is null)
                return SituacaoConfronto.Conferido;

            var diferenca = precoNota - precoComprado.Value;

            if (Math.Abs(diferenca) <= ToleranciaPreco)
                return SituacaoConfronto.Conferido;

            return diferenca > 0 ? SituacaoConfronto.PrecoAcima : SituacaoConfronto.PrecoAbaixo;
        }

        /// <summary>Produto amarrado a este código do fornecedor, se houver.
        /// A normalização tem que ser a MESMA da gravação, senão um código com
        /// espaço sobrando nunca casa.</summary>
        private static int? DoDePara(IReadOnlyDictionary<string, int>? dePara, string codigoFornecedor)
        {
            if (dePara is null || string.IsNullOrWhiteSpace(codigoFornecedor))
                return null;

            return dePara.TryGetValue(codigoFornecedor.Trim().ToUpperInvariant(), out var produtoId)
                ? produtoId
                : null;
        }

        private static string Chave(string codigo, string descricao) =>
            string.IsNullOrWhiteSpace(codigo) ? descricao.Trim().ToUpperInvariant()
                                              : codigo.Trim().ToUpperInvariant();

        private static bool Iguais(string a, string b) =>
            !string.IsNullOrWhiteSpace(a)
            && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
