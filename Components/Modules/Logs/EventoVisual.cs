using Erp.Model.Log;

namespace Erp.Components.Modules.Logs;

/// <summary>
/// Cor e rótulo dos eventos de sistema. Mesma lógica do AcaoVisual: vermelho é
/// o que falha ou é barrado, verde o que entra, cinza o rotineiro.
/// </summary>
public static class EventoVisual
{
    public static string Texto(TipoEventoSistema evento) => evento switch
    {
        TipoEventoSistema.Login => "Login",
        TipoEventoSistema.LoginFalho => "Login falho",
        TipoEventoSistema.Logout => "Logout",
        TipoEventoSistema.SenhaRedefinida => "Senha redefinida",
        TipoEventoSistema.AcessoNegado => "Acesso negado",
        TipoEventoSistema.Excecao => "Exceção",
        TipoEventoSistema.Inicializacao => "Inicialização",
        _ => evento.ToString(),
    };

    public static string Classe(TipoEventoSistema evento) => evento switch
    {
        TipoEventoSistema.Login => "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
        TipoEventoSistema.LoginFalho => "bg-red-500/15 text-red-700 dark:text-red-300",
        TipoEventoSistema.Logout => "bg-slate-500/15 text-slate-700 dark:text-slate-300",
        TipoEventoSistema.SenhaRedefinida => "bg-amber-500/15 text-amber-700 dark:text-amber-300",
        TipoEventoSistema.AcessoNegado => "bg-red-500/15 text-red-700 dark:text-red-300",
        TipoEventoSistema.Excecao => "bg-red-600/20 text-red-700 dark:text-red-300 font-semibold",
        TipoEventoSistema.Inicializacao => "bg-sky-500/15 text-sky-700 dark:text-sky-300",
        _ => "bg-muted text-muted-foreground",
    };

    public static string Icone(TipoEventoSistema evento) => evento switch
    {
        TipoEventoSistema.Login => "log-in",
        TipoEventoSistema.LoginFalho => "shield-x",
        TipoEventoSistema.Logout => "log-out",
        TipoEventoSistema.SenhaRedefinida => "key-round",
        TipoEventoSistema.AcessoNegado => "shield-ban",
        TipoEventoSistema.Excecao => "triangle-alert",
        TipoEventoSistema.Inicializacao => "power",
        _ => "dot",
    };
}
