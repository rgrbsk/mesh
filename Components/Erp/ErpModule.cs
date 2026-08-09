namespace Erp.Components.Erp;

/// <summary>A navigable ERP module: its key, tab title, sidebar icon and section.</summary>
public record ErpModule(string Key, string Title, string Icon, string Group);

/// <summary>Single source of truth for the ERP modules shown in the sidebar/dock.</summary>
public static class ErpModules
{
    public static readonly IReadOnlyList<ErpModule> All = new List<ErpModule>
    {
        new("painel",        "Painel",        "layout-dashboard", "Geral"),
        new("clientes",      "Clientes",      "users",            "Comercial"),
        new("produtos",      "Produtos",      "package",          "Comercial"),
        new("pedidos",       "Pedidos",       "shopping-cart",    "Comercial"),
        new("estoque",       "Estoque",       "boxes",            "Operações"),
        new("financeiro",    "Financeiro",    "wallet",           "Gestão"),
        new("relatorios",    "Relatórios",    "file-text",        "Gestão"),
        new("configuracoes", "Configurações", "settings",         "Sistema"),
    };

    public static ErpModule? Find(string key) => All.FirstOrDefault(m => m.Key == key);
}
