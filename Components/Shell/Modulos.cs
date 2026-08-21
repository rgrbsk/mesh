using Erp.Model.Acesso;
using System.Security.Claims;
// Um using por pasta de módulo — é o ÚNICO lugar que referencia os componentes
// de conteúdo (via typeof). Adicionar módulo = criar a pasta + o using aqui.
using Erp.Components.Modules.Painel;
using Erp.Components.Modules.Clientes;
using Erp.Components.Modules.Solicitacoes;
using Erp.Components.Modules.Estoque;
using Erp.Components.Modules.Financeiro;
using Erp.Components.Modules.Relatorios;
using Erp.Components.Modules.Configuracoes;
using Erp.Components.Modules;

namespace Erp.Components.Shell;

/// <summary>Um módulo navegável: chave, título, ícone, seção, permissão exigida
/// e o componente de conteúdo que ele abre.</summary>
public record Modulo(
    string Key, string Title, string Icon, string Group, string? Permissao, Type Componente, string? Cor);

/// <summary>Fonte ÚNICA dos módulos (sidebar + conteúdo).
/// Adicionar um módulo = UMA linha aqui. A página Home renderiza o conteúdo via
/// &lt;DynamicComponent&gt; a partir do Componente — sem switch pra manter em paralelo.</summary>
public static class Modulos
{
    /// <summary>Módulo em que a aplicação abre ao entrar em /home sem chave.</summary>
    public const string Padrao = "inicio";

    public static readonly IReadOnlyList<Modulo> All = new List<Modulo>
    {
        //     Key            Título                 Ícone                     Grupo        Permissão                  Componente
        new("painel",      "Painel",             "chart-no-axes-combined", "Geral",     null,                      typeof(PainelModule),"#C51E3A"),
        new("compras",     "Solicit. de Compra", "shopping-cart",          "Comercial", Permissoes.ComprasVer,     typeof(SolicitacoesModule), "#89CFF0"),
        new("fornecedores","Fornecedores",       "users",                  "Comercial", Permissoes.FornecedorVer,  typeof(ClientesModule), "#ADFF2F"),
        new("centrosdecusto",  "Centros de Custo",         "currency",                 "Gestão",    null,          typeof(FinanceiroModule), "#4F7942"),
        new("estoque",     "Estoque",            "list",                   "Operações", Permissoes.EstoqueVer,     typeof(EstoqueModule), "#B284BE"),
        new("relatorios",  "Relatórios",         "file-text",              "Gestão",    null,                      typeof(RelatoriosModule),  "#DE3163"),
        new("log",   "Logs",          "footprints",             "Sistema",   null,                      typeof(ConfiguracoesModule), "#FF4F00"),
        new("permissoes",  "Permissões",         "user-key",               "Sistema",   Permissoes.UsuariosGerir,  typeof(ConfiguracoesModule), "#FCF75E"),
        new("inicio",  "Início",         "layers",               "Geral",   null,  typeof(Inicio),"#007FFF" )
    };

    public static Modulo? Find(string key) => All.FirstOrDefault(m => m.Key == key);

    public static IReadOnlyList<Modulo> BuscarPermissoesDoUsuario(ClaimsPrincipal user) =>
        All.Where(m => m.Permissao is null
                    || user.HasClaim(Permissoes.ClaimType, m.Permissao))
           .ToList();
}
