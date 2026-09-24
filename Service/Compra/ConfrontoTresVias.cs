using Erp.Model.Compra;
using Erp.Model.Fiscal;

namespace Erp.Service.Compra
{
    /// <summary>
    /// O que cada linha do confronto revelou. Comparado ao confronto de duas
    /// vias, que só sabia opor a nota ao que foi comprado, aqui existem
    /// situações que dependem do recebimento físico — e são justamente elas
    /// que o three-way match existe para pegar.
    /// </summary>
    public enum SituacaoTresVias
    {
        /// <summary>Pedido, recebido e cobrado batem.</summary>
        Conferido = 0,

        /// <summary>A nota cobra mais unidades do que foram pedidas.</summary>
        QuantidadeAcimaDoPedido = 1,

        /// <summary>A nota cobra menos unidades do que foram pedidas — entrega
        /// parcial faturada.</summary>
        QuantidadeAbaixoDoPedido = 2,

        /// <summary>
        /// A nota cobra mais do que entrou fisicamente. É a divergência mais
        /// cara de todas e a única que o confronto de duas vias não enxerga:
        /// a nota bate com o pedido, mas a mercadoria não chegou inteira.
        /// </summary>
        CobradoAcimaDoRecebido = 3,

        /// <summary>Recebido, porém ainda não cobrado nesta nota.</summary>
        RecebidoNaoCobrado = 4,

        /// <summary>Consta na nota, mas nenhum recebimento confirmou a entrada.</summary>
        SemRecebimento = 5,

        PrecoAcima = 6,

        PrecoAbaixo = 7,

        /// <summary>Item na nota que não está no pedido.</summary>
        ForaDoPedido = 8,

        /// <summary>Item do pedido que não veio nesta nota nem foi recebido.</summary>
        NaoEntregue = 9,
    }

    /// <summary>Uma linha do mapa de confronto, com as três pontas lado a lado.</summary>
    public sealed record LinhaTresVias(
        SituacaoTresVias Situacao,
        string Descricao,
        string CodigoFornecedor,
        decimal QuantidadeNota,
        decimal? QuantidadePedida,
        decimal? QuantidadeRecebida,
        decimal ValorUnitarioNota,
        decimal? ValorUnitarioPedido,
        int? ProdutoId,
        bool ComAvaria)
    {
        public decimal TotalNota => QuantidadeNota * ValorUnitarioNota;

        public decimal? TotalPedido => QuantidadePedida is null || ValorUnitarioPedido is null
            ? null
            : QuantidadePedida * ValorUnitarioPedido;

        /// <summary>Quanto a nota cobra a mais (positivo) ou a menos que o
        /// pedido. Nulo quando não há pedido com o que comparar.</summary>
        public decimal? Diferenca => TotalPedido is null ? null : TotalNota - TotalPedido;

        public bool Divergente => Situacao != SituacaoTresVias.Conferido;

        /// <summary>
        /// Divergência que impede o pagamento, por oposição à que só merece
        /// registro. Receber menos do que se paga é bloqueante; receber mais
        /// barato, não.
        /// </summary>
        public bool Bloqueante => Situacao is SituacaoTresVias.CobradoAcimaDoRecebido
                                           or SituacaoTresVias.QuantidadeAcimaDoPedido
                                           or SituacaoTresVias.PrecoAcima
                                           or SituacaoTresVias.ForaDoPedido
                                           or SituacaoTresVias.SemRecebimento;
    }

    /// <summary>Resultado inteiro, com os totais que a tela mostra no rodapé.</summary>
    public sealed record ResultadoTresVias(List<LinhaTresVias> Linhas)
    {
        public decimal TotalNota => Linhas.Sum(l => l.TotalNota);

        public decimal TotalPedido => Linhas.Sum(l => l.TotalPedido ?? 0m);

        public decimal TotalRecebido => Linhas.Sum(l =>
            (l.QuantidadeRecebida ?? 0m) * (l.ValorUnitarioPedido ?? 0m));

        public decimal Diferenca => TotalNota - TotalPedido;

        public int Divergencias => Linhas.Count(l => l.Divergente);

        public int Bloqueios => Linhas.Count(l => l.Bloqueante);

        public bool Fecha => Divergencias == 0;

        /// <summary>Pode virar título a pagar sem justificativa? É o que separa
        /// a liberação automática da liberação com ressalva.</summary>
        public bool LiberaSemRessalva => Bloqueios == 0;
    }

    /// <summary>
    /// Confronta as TRÊS pontas da compra: o que foi pedido (ordem de compra),
    /// o que entrou (recebimentos) e o que está sendo cobrado (nota fiscal).
    ///
    /// Classe estática e pura, pelo mesmo motivo do confronto de duas vias que
    /// a antecedeu: é a regra que mais merece teste e a que mais dói se estiver
    /// errada — é ela que autoriza pagar.
    /// </summary>
    public static class ConfrontoTresVias
    {
        /// <summary>
        /// Tolerância de centavos no preço unitário. A NF-e traz o unitário com
        /// quatro casas e o pedido guarda o preço como foi acordado;
        /// arredondamento de um centavo não é divergência comercial.
        /// </summary>
        private const decimal ToleranciaPreco = 0.01m;

        /// <param name="dePara">
        /// Código do fornecedor → id do nosso produto, já normalizado em caixa
        /// alta. É a PRIMEIRA regra de casamento: o cProd da nota é o código
        /// interno de quem emitiu, e só o de-para sabe traduzi-lo.
        /// </param>
        /// <param name="avariados">
        /// Ids de item de pedido que tiveram alguma entrada marcada com avaria.
        /// Não muda a classificação — a mercadoria entrou —, mas acompanha a
        /// linha para quem for liberar o pagamento.
        /// </param>
        public static ResultadoTresVias Comparar(
            NotaFiscal nota,
            IReadOnlyCollection<ItemOrdemCompra> itensPedido,
            IReadOnlyDictionary<string, int>? dePara = null,
            IReadOnlySet<int>? avariados = null)
        {
            // O MESMO produto pode vir em várias linhas da nota (lote ou
            // validade diferentes). Comparar linha a linha acusaria quantidade
            // a menor em cada uma; o que vale é a soma por produto.
            var daNota = nota.Itens
                .GroupBy(i => Chave(i.CodigoFornecedor, i.Descricao))
                .Select(g => new
                {
                    Codigo = g.First().CodigoFornecedor,
                    Descricao = g.First().Descricao,
                    Quantidade = g.Sum(i => i.Quantidade),
                    // Unitário médio ponderado: se as linhas vierem com preços
                    // diferentes, é o que o fornecedor cobra de fato por unidade.
                    ValorUnitario = g.Sum(i => i.Quantidade) == 0
                        ? 0m
                        : g.Sum(i => i.ValorTotal) / g.Sum(i => i.Quantidade),
                    ProdutoId = g.Select(i => i.ProdutoId).FirstOrDefault(id => id is not null)
                             ?? DoDePara(dePara, g.First().CodigoFornecedor),
                })
                .ToList();

            var pedidos = itensPedido
                .Select(item => new
                {
                    Item = item,
                    Codigo = item.Produto?.Codigo ?? "",
                    Descricao = item.Produto?.Descricao ?? "",
                })
                .ToList();

            var linhas = new List<LinhaTresVias>();
            var casados = new HashSet<int>();

            foreach (var linhaNota in daNota)
            {
                // Amarração por produto feita na tela vence; depois código
                // igual; por último descrição igual. Na prática o de-para é o
                // caminho normal, porque o código da nota é o do emitente.
                var pedido = pedidos.FirstOrDefault(p =>
                                  linhaNota.ProdutoId is not null && p.Item.ProdutoId == linhaNota.ProdutoId)
                          ?? pedidos.FirstOrDefault(p => Iguais(p.Codigo, linhaNota.Codigo))
                          ?? pedidos.FirstOrDefault(p => Iguais(p.Descricao, linhaNota.Descricao));

                if (pedido is null)
                {
                    linhas.Add(new LinhaTresVias(
                        SituacaoTresVias.ForaDoPedido, linhaNota.Descricao, linhaNota.Codigo,
                        linhaNota.Quantidade, null, null,
                        linhaNota.ValorUnitario, null, linhaNota.ProdutoId, false));

                    continue;
                }

                casados.Add(pedido.Item.Id);

                var avaria = avariados?.Contains(pedido.Item.Id) ?? false;

                linhas.Add(new LinhaTresVias(
                    Classificar(
                        linhaNota.Quantidade,
                        pedido.Item.Quantidade,
                        pedido.Item.QuantidadeRecebida,
                        linhaNota.ValorUnitario,
                        pedido.Item.PrecoUnitario),
                    linhaNota.Descricao,
                    linhaNota.Codigo,
                    linhaNota.Quantidade,
                    pedido.Item.Quantidade,
                    pedido.Item.QuantidadeRecebida,
                    linhaNota.ValorUnitario,
                    pedido.Item.PrecoUnitario,
                    pedido.Item.ProdutoId,
                    avaria));
            }

            // O que foi pedido e não veio nesta nota. Sem isto, entrega parcial
            // passaria como conferida — a nota bate consigo mesma.
            foreach (var pendente in pedidos.Where(p => !casados.Contains(p.Item.Id)))
            {
                // Recebido e não cobrado é diferente de simplesmente não
                // entregue: no primeiro caso a mercadoria está no depósito
                // aguardando fatura, no segundo ela não chegou.
                var situacao = pendente.Item.QuantidadeRecebida > 0
                    ? SituacaoTresVias.RecebidoNaoCobrado
                    : SituacaoTresVias.NaoEntregue;

                linhas.Add(new LinhaTresVias(
                    situacao,
                    pendente.Descricao,
                    pendente.Codigo,
                    0m,
                    pendente.Item.Quantidade,
                    pendente.Item.QuantidadeRecebida,
                    0m,
                    pendente.Item.PrecoUnitario,
                    pendente.Item.ProdutoId,
                    avariados?.Contains(pendente.Item.Id) ?? false));
            }

            return new ResultadoTresVias(linhas);
        }

        /// <summary>
        /// A ordem das verificações é a ordem da gravidade.
        ///
        /// Cobrar mais do que entrou vem primeiro porque é o único caso em que
        /// se paga por mercadoria que não está no depósito. Depois vêm as
        /// divergências contra o pedido, e só então o preço: quantidade errada
        /// muda o total de qualquer jeito, e apontar as duas coisas ao mesmo
        /// tempo só obscureceria o diagnóstico.
        /// </summary>
        private static SituacaoTresVias Classificar(
            decimal quantidadeNota,
            decimal quantidadePedida,
            decimal quantidadeRecebida,
            decimal precoNota,
            decimal precoPedido)
        {
            if (quantidadeNota > quantidadeRecebida)
                return quantidadeRecebida == 0m
                    ? SituacaoTresVias.SemRecebimento
                    : SituacaoTresVias.CobradoAcimaDoRecebido;

            if (quantidadeNota > quantidadePedida)
                return SituacaoTresVias.QuantidadeAcimaDoPedido;

            if (quantidadeNota < quantidadePedida)
                return SituacaoTresVias.QuantidadeAbaixoDoPedido;

            var diferenca = precoNota - precoPedido;

            if (Math.Abs(diferenca) <= ToleranciaPreco)
                return SituacaoTresVias.Conferido;

            return diferenca > 0 ? SituacaoTresVias.PrecoAcima : SituacaoTresVias.PrecoAbaixo;
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
