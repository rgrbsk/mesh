namespace Erp.Model.Acesso
{
    /// <summary>Uma permissão do catálogo, com o texto que a tela mostra.</summary>
    public record Permissao(string Chave, string Titulo, string Descricao, string Grupo);

    public static class Permissoes
    {
        public const string ClaimType = "perm";

        public const string ComprasVer = "compras.ver";
        public const string ComprasCriar = "compras.criar";
        public const string ComprasAprovar = "compras.aprovar";
        public const string ProdutosVer = "produtos.ver";
        public const string ProdutosEditar = "produtos.editar";
        public const string FornecedorVer = "fornecedor.ver";
        public const string FornecedorEditar = "fornecedor.editar";
        public const string CentrosCustoGerir = "centroscusto.gerir";
        public const string CotacoesGerir = "cotacoes.gerir";
        public const string EtapasGerir = "etapas.gerir";
        public const string LogsVer = "logs.ver";
        public const string LogsStackTrace = "logs.stacktrace";
        public const string UsuariosGerir = "usuarios.gerir";

        /// <summary>
        /// Catálogo: é daqui que sai a tela de permissões e o seed do papel
        /// Administrador. Acrescentar permissão = uma linha aqui.
        /// </summary>
        public static readonly IReadOnlyList<Permissao> Catalogo =
        [
            new(ComprasVer,        "Ver solicitações",     "Enxerga as solicitações de compra.",                        "Compras"),
            new(ComprasCriar,      "Criar solicitações",   "Abre e envia solicitações de compra.",                      "Compras"),
            new(ComprasAprovar,    "Aprovar solicitações", "Aprova, recusa ou devolve o que chega para o seu centro.",  "Compras"),
            new(ProdutosVer,       "Ver produtos",         "Enxerga o catálogo e os parâmetros de reposição.",          "Produtos"),
            new(ProdutosEditar,    "Editar produtos",      "Cadastra e altera produtos.",                               "Produtos"),
            new(FornecedorVer,     "Ver pessoas",          "Enxerga fornecedores, clientes e demais cadastros.",        "Cadastros"),
            new(FornecedorEditar,  "Editar pessoas",       "Cadastra, altera e exclui pessoas.",                        "Cadastros"),
            new(CentrosCustoGerir, "Gerir centros de custo","Cadastra centros e define quem aprova cada um.",           "Cadastros"),
            new(CotacoesGerir,     "Gerir cotações",       "Monta rodadas de cotação e envia os links aos fornecedores.", "Compras"),
            new(EtapasGerir,       "Gerir etapas",         "Customiza as colunas do fluxo até a aprovação.",            "Adicionais"),
            new(LogsVer,           "Ver logs",             "Enxerga o histórico de quem fez o quê.",                    "Sistema"),
            new(LogsStackTrace,    "Ver stacktrace",       "Abre o rastreamento técnico dos erros no log.",             "Sistema"),
            new(UsuariosGerir,     "Gerir usuários",       "Cadastra usuários e concede permissões.",                   "Sistema"),
        ];

        public static readonly string[] Todas = [.. Catalogo.Select(p => p.Chave)];

        public static string Titulo(string chave) =>
            Catalogo.FirstOrDefault(p => p.Chave == chave)?.Titulo ?? chave;
    }
}
