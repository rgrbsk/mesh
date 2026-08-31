using Erp.Service.Fiscal;

namespace Erp.Components.Modules.Notas;

/// <summary>
/// Cor, ícone e rótulo de cada situação do confronto. Extraído do diálogo de
/// importação porque agora duas telas mostram o mesmo mapa — e cor de
/// divergência que muda de tela para tela ensina o usuário a desconfiar.
/// </summary>
public static class ConfrontoVisual
{
    public static string Texto(SituacaoConfronto situacao) => situacao switch
    {
        SituacaoConfronto.Conferido => "Confere",
        SituacaoConfronto.PrecoAcima => "Preço acima",
        SituacaoConfronto.PrecoAbaixo => "Preço abaixo",
        SituacaoConfronto.QuantidadeDivergente => "Qtde. divergente",
        SituacaoConfronto.ForaDaCompra => "Fora da compra",
        SituacaoConfronto.NaoEntregue => "Não entregue",
        _ => situacao.ToString(),
    };

    public static string Classe(SituacaoConfronto situacao) => situacao switch
    {
        SituacaoConfronto.Conferido => "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
        SituacaoConfronto.PrecoAcima => "bg-red-500/15 text-red-700 dark:text-red-300",
        SituacaoConfronto.PrecoAbaixo => "bg-sky-500/15 text-sky-700 dark:text-sky-300",
        SituacaoConfronto.QuantidadeDivergente => "bg-amber-500/15 text-amber-700 dark:text-amber-300",
        SituacaoConfronto.ForaDaCompra => "bg-red-500/15 text-red-700 dark:text-red-300",
        SituacaoConfronto.NaoEntregue => "bg-amber-500/15 text-amber-700 dark:text-amber-300",
        _ => "bg-muted text-muted-foreground",
    };

    public static string Icone(SituacaoConfronto situacao) => situacao switch
    {
        SituacaoConfronto.Conferido => "circle-check",
        SituacaoConfronto.PrecoAcima => "trending-up",
        SituacaoConfronto.PrecoAbaixo => "trending-down",
        SituacaoConfronto.QuantidadeDivergente => "scale",
        SituacaoConfronto.ForaDaCompra => "circle-plus",
        SituacaoConfronto.NaoEntregue => "package-x",
        _ => "dot",
    };
}
