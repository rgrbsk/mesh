namespace Erp.Components.Modules.Relatorios;

/// <summary>
/// Alinhamento das colunas de relatório.
///
/// O resultado chega como texto já formatado, então o tipo original se perdeu —
/// é o RÓTULO que diz se a coluna é número. Fica num lugar só porque a prévia,
/// o CSV e o PDF precisam concordar: coluna alinhada de um jeito na tela e de
/// outro no papel é o tipo de detalhe que faz alguém desconfiar do número.
/// </summary>
public static class ColunaVisual
{
    private static readonly HashSet<string> Numericas =
    [
        "Quantidade", "Qtde.", "Valor", "Valor total", "Solicitações", "Itens",
        "Linhas", "Pendentes", "Aprovadas", "Recusadas", "Cotações", "Convites",
        "Responderam", "Respostas", "Notas", "Cadastros", "Com e-mail", "Pedidos",
        "Ações", "Pessoas", "Ocorrências", "Entrou", "Falhou", "IPs", "Decisões",
        "Aprovou", "Recusou", "Devolveu", "Horas em média", "Escolhido",
        "Menor preço", "Diferença", "Saldo", "Mínimo", "Ponto de pedido",
        "Itens vencidos", "Itens parados", "Pelo link", "Homologação",
    ];

    public static bool EhNumerica(string coluna) => Numericas.Contains(coluna);
}
