namespace Erp.Components.Erp;

/// <summary>A navigable ERP module: its key, tab title, sidebar icon and section.</summary>
public record ErpModule(string Key, string Title, string Icon, string Group);

/// <summary>Single source of truth for the ERP modules shown in the sidebar/dock.</summary>
public static class ErpModules
{
    public static readonly IReadOnlyList<ErpModule> All = new List<ErpModule>
    {
        new("painel",        "Painel",        "chart-no-axes-combined", "Geral"),
        new("clientes",      "Fornecedores",      "users",            "Comercial"),
        new("produtos",      "Produtos",      "package",          "Comercial"),
        new("pedidos",       "Solicit. de Compra",       "shopping-cart",    "Comercial"),
        new("estoque",       "Estoque",       "list",            "Operações"),
        new("financeiro",    "Financeiro",    "wallet",           "Gestão"),
        new("relatorios",    "Relatórios",    "file-text",        "Gestão"),
        new("configuracoes", "Auditoria", "footprints",         "Sistema"),
        new("configuracoes", "Permissões", "user-key",         "Sistema")
    };

    public static ErpModule? Find(string key) => All.FirstOrDefault(m => m.Key == key);
}
