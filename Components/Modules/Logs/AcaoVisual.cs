using Erp.Model.Log;

namespace Erp.Components.Modules.Logs;

/// <summary>
/// Cor e rótulo de cada ação. As cores separam por NATUREZA, não por bonito:
/// verde é o que aprova, vermelho o que nega ou quebra, âmbar o que volta para
/// alguém, azul o que anda. Quem varre a tela procura vermelho.
/// </summary>
public static class AcaoVisual
{
    public static string Texto(TipoAcao acao) => acao switch
    {
        TipoAcao.Criacao => "Criação",
        TipoAcao.Alteracao => "Alteração",
        TipoAcao.Exclusao => "Exclusão",
        TipoAcao.Envio => "Envio",
        TipoAcao.Aprovacao => "Aprovação",
        TipoAcao.Recusa => "Recusa",
        TipoAcao.Devolucao => "Devolução",
        TipoAcao.Resposta => "Resposta",
        TipoAcao.Escolha => "Escolha",
        TipoAcao.Acesso => "Acesso",
        TipoAcao.Erro => "Erro",
        _ => acao.ToString(),
    };

    /// <summary>Classes do badge. Tom fraco no fundo e forte no texto, para o
    /// contraste sobreviver ao tema escuro.</summary>
    public static string Classe(TipoAcao acao) => acao switch
    {
        TipoAcao.Criacao => "bg-sky-500/15 text-sky-700 dark:text-sky-300",
        TipoAcao.Alteracao => "bg-slate-500/15 text-slate-700 dark:text-slate-300",
        TipoAcao.Exclusao => "bg-red-500/15 text-red-700 dark:text-red-300",
        TipoAcao.Envio => "bg-blue-500/15 text-blue-700 dark:text-blue-300",
        TipoAcao.Aprovacao => "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
        TipoAcao.Recusa => "bg-red-500/15 text-red-700 dark:text-red-300",
        TipoAcao.Devolucao => "bg-amber-500/15 text-amber-700 dark:text-amber-300",
        TipoAcao.Resposta => "bg-violet-500/15 text-violet-700 dark:text-violet-300",
        TipoAcao.Escolha => "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
        TipoAcao.Acesso => "bg-slate-500/15 text-slate-700 dark:text-slate-300",
        TipoAcao.Erro => "bg-red-600/20 text-red-700 dark:text-red-300 font-semibold",
        _ => "bg-muted text-muted-foreground",
    };

    public static string Icone(TipoAcao acao) => acao switch
    {
        TipoAcao.Criacao => "plus",
        TipoAcao.Alteracao => "pencil",
        TipoAcao.Exclusao => "trash-2",
        TipoAcao.Envio => "send",
        TipoAcao.Aprovacao => "circle-check",
        TipoAcao.Recusa => "circle-x",
        TipoAcao.Devolucao => "undo-2",
        TipoAcao.Resposta => "inbox",
        TipoAcao.Escolha => "trophy",
        TipoAcao.Acesso => "log-in",
        TipoAcao.Erro => "triangle-alert",
        _ => "dot",
    };
}
