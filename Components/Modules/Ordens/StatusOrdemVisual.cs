using BlazorBlueprint.Components;
using Erp.Model.Compra;
using Erp.Model.Financeiro;

namespace Erp.Components.Modules.Ordens;

/// <summary>
/// Como cada estado aparece. Fica fora dos .razor porque a lista, o diálogo e
/// a tela de recebimento mostram o mesmo badge — duplicar o switch faria os
/// rótulos divergirem com o tempo. Mesmo critério do StatusVisual das
/// solicitações.
/// </summary>
public static class StatusOrdemVisual
{
    public static string Texto(StatusOrdemCompra status) => status switch
    {
        StatusOrdemCompra.Rascunho => "Rascunho",
        StatusOrdemCompra.Enviada => "Enviada ao fornecedor",
        StatusOrdemCompra.ParcialmenteRecebida => "Recebida em parte",
        StatusOrdemCompra.Recebida => "Recebida",
        StatusOrdemCompra.Cancelada => "Cancelada",
        _ => status.ToString(),
    };

    public static BadgeVariant Badge(StatusOrdemCompra status) => status switch
    {
        StatusOrdemCompra.Rascunho => BadgeVariant.Outline,
        StatusOrdemCompra.Enviada => BadgeVariant.Secondary,
        StatusOrdemCompra.ParcialmenteRecebida => BadgeVariant.Secondary,
        StatusOrdemCompra.Recebida => BadgeVariant.Default,
        StatusOrdemCompra.Cancelada => BadgeVariant.Destructive,
        _ => BadgeVariant.Outline,
    };

    /// <summary>Ícone lucide do estado, para a linha ser reconhecível de
    /// relance sem ler o texto.</summary>
    public static string Icone(StatusOrdemCompra status) => status switch
    {
        StatusOrdemCompra.Rascunho => "file-pen",
        StatusOrdemCompra.Enviada => "send",
        StatusOrdemCompra.ParcialmenteRecebida => "package-open",
        StatusOrdemCompra.Recebida => "package-check",
        StatusOrdemCompra.Cancelada => "circle-x",
        _ => "file",
    };

    /// <summary>
    /// Em que passo do ciclo o pedido está, para o BbStepper do diálogo.
    /// Cancelada não tem passo: o ciclo foi interrompido, e a tela mostra isso
    /// com um aviso em vez da régua.
    /// </summary>
    public static int Passo(StatusOrdemCompra status) => status switch
    {
        StatusOrdemCompra.Rascunho => 0,
        StatusOrdemCompra.Enviada => 1,
        StatusOrdemCompra.ParcialmenteRecebida => 2,
        StatusOrdemCompra.Recebida => 2,
        _ => 0,
    };

    // ---- Contas a pagar: o mesmo critério, do outro lado do ciclo ----

    public static string Texto(StatusTitulo status) => status switch
    {
        StatusTitulo.Aberto => "Em aberto",
        StatusTitulo.ParcialmentePago => "Pago em parte",
        StatusTitulo.Pago => "Quitado",
        StatusTitulo.Cancelado => "Cancelado",
        _ => status.ToString(),
    };

    public static BadgeVariant Badge(StatusTitulo status) => status switch
    {
        StatusTitulo.Aberto => BadgeVariant.Outline,
        StatusTitulo.ParcialmentePago => BadgeVariant.Secondary,
        StatusTitulo.Pago => BadgeVariant.Default,
        StatusTitulo.Cancelado => BadgeVariant.Destructive,
        _ => BadgeVariant.Outline,
    };
}
