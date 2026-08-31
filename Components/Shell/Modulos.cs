using Erp.Model.Acesso;
using System.Security.Claims;
// Um using por pasta de módulo — é o ÚNICO lugar que referencia os componentes
// de conteúdo (via typeof). Adicionar módulo = criar a pasta + o using aqui.
using Erp.Components.Modules.Painel;
using Erp.Components.Modules.Pessoas;
using Erp.Components.Modules.Solicitacoes;
using Erp.Components.Modules.Produtos;
using Erp.Components.Modules.Usuarios;
using Erp.Components.Modules.Financeiro;
using Erp.Components.Modules.Relatorios;
using Erp.Components.Modules.Kanban;
using Erp.Components.Modules.Adicionais;
using Erp.Components.Modules.Cotacoes;
using Erp.Components.Modules.Aprovacoes;
using Erp.Components.Modules.Logs;
using Erp.Components.Modules.Notas;

namespace Erp.Components.Shell;

/// <summary>Um módulo navegável: chave, título, ícone, seção, permissão exigida
/// e o componente de conteúdo que ele abre.</summary>
public record Modulo(
    string Key, string Title, string Icon, string Group, string? Permissao, Type Componente, string? Cor,
    /// <summary>Fora da barra de abas, mas ainda roteável por /home/{key}. Para
    /// o que já tem atalho próprio no topo e viraria um segundo caminho para o
    /// mesmo lugar.</summary>
    bool ForaDasAbas = false);

/// <summary>Fonte ÚNICA dos módulos (sidebar + conteúdo).
/// Adicionar um módulo = UMA linha aqui. A página Home renderiza o conteúdo via
/// &lt;DynamicComponent&gt; a partir do Componente — sem switch pra manter em paralelo.</summary>
public static class Modulos
{
    /// <summary>Módulo em que a aplicação abre ao entrar em /home sem chave.</summary>
    public const string Padrao = "painel";

    public static readonly IReadOnlyList<Modulo> All = new List<Modulo>
    {
        //     Key            Título                 Ícone                     Grupo        Permissão                  Componente
        new("painel",      "Painel",             "chart-no-axes-combined", "Geral",     null,                      typeof(PainelModule),"#C51E3A"),
        new("compras",     "Solicit. de Compra", "shopping-cart",          "Comercial", Permissoes.ComprasVer,     typeof(SolicitacoesModule), "#89CFF0"),
        new("kanban",      "Kanban",             "kanban",                 "Comercial", Permissoes.ComprasVer,     typeof(KanbanModule), "#00CED1", ForaDasAbas: true),
        new("cotacoes",    "Cotações",           "mail",                   "Comercial", Permissoes.CotacoesGerir,  typeof(CotacoesModule), "#FF7F50", ForaDasAbas: true),
        new("aprovacoes",  "Aprovações",         "circle-check",           "Comercial", Permissoes.ComprasAprovar, typeof(AprovacoesModule), "#7CFC00"),
        new("notas",       "Notas Fiscais",      "file-check-2",           "Comercial", Permissoes.NotasGerir,     typeof(NotasModule), "#FFB347"),
        new("pessoas","Pessoas",       "users",                  "Comercial", Permissoes.FornecedorVer,  typeof(PessoasModule), "#ADFF2F"),
        new("centrosdecusto",  "Centros de Custo",         "currency",                 "Gestão",    Permissoes.CentrosCustoGerir, typeof(FinanceiroModule), "#4F7942"),
        new("produtos",    "Produtos",           "package",                "Operações", Permissoes.ProdutosVer,    typeof(ProdutosModule), "#E4A0F7"),
        new("relatorios",  "Relatórios",         "file-text",              "Gestão",    null,                      typeof(RelatoriosModule),  "#DE3163"),
        new("logs",        "Logs",               "footprints",             "Sistema",   Permissoes.LogsVer,        typeof(LogsModule), "#FF4F00"),
        new("usuarios",    "Usuários",           "user-key",               "Sistema",   Permissoes.UsuariosGerir,  typeof(UsuariosModule), "#FCF75E"),
        new("adicionais",  "Adicionais",         "settings-2",             "Sistema",   Permissoes.EtapasGerir,    typeof(AdicionaisModule), "#007FFF"),
    };

    public static Modulo? Find(string key) => All.FirstOrDefault(m => m.Key == key);

    public static IReadOnlyList<Modulo> BuscarPermissoesDoUsuario(ClaimsPrincipal user) =>
        All.Where(m => m.Permissao is null
                    || user.HasClaim(Permissoes.ClaimType, m.Permissao))
           .ToList();
}
