using BlazorBlueprint.Components;
using Erp.Model.Solicitacao;

namespace Erp.Components.Modules.Solicitacoes;

/// <summary>
/// Como cada estado aparece. Fica fora dos .razor porque a lista e o diálogo
/// mostram o mesmo badge — duplicar o switch nos dois deixaria os rótulos
/// divergirem com o tempo.
/// </summary>
public static class StatusVisual
{
    public static string Texto(StatusSolicitacao status) => status switch
    {
        StatusSolicitacao.Rascunho => "Rascunho",
        StatusSolicitacao.Enviada => "Aguardando aprovação",
        StatusSolicitacao.Aprovada => "Aprovada",
        StatusSolicitacao.AprovadaParcialmente => "Aprovada em parte",
        StatusSolicitacao.Recusada => "Recusada",
        StatusSolicitacao.Devolvida => "Devolvida para ajuste",
        _ => status.ToString(),
    };

    public static BadgeVariant Badge(StatusSolicitacao status) => status switch
    {
        StatusSolicitacao.Rascunho => BadgeVariant.Outline,
        StatusSolicitacao.Enviada => BadgeVariant.Secondary,
        StatusSolicitacao.Aprovada => BadgeVariant.Default,
        StatusSolicitacao.AprovadaParcialmente => BadgeVariant.Secondary,
        StatusSolicitacao.Recusada => BadgeVariant.Destructive,
        StatusSolicitacao.Devolvida => BadgeVariant.Outline,
        _ => BadgeVariant.Outline,
    };

    public static string Texto(StatusItem status) => status switch
    {
        StatusItem.Pendente => "Pendente",
        StatusItem.Aprovado => "Aprovado",
        StatusItem.Recusado => "Recusado",
        StatusItem.Devolvido => "Devolvido",
        _ => status.ToString(),
    };
}
