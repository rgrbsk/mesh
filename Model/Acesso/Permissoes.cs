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
        public const string NotasGerir = "notas.gerir";
        public const string EtapasGerir = "etapas.gerir";
        public const string LogsVer = "logs.ver";
        public const string LogsStackTrace = "logs.stacktrace";
        public const string LogsSistema = "logs.sistema";
        public const string UsuariosGerir = "usuarios.gerir";

        // ---- Ordem de compra e recebimento ----
        public const string OrdensVer = "ordens.ver";
        public const string OrdensGerir = "ordens.gerir";
        public const string RecebimentoRegistrar = "recebimento.registrar";

        // ---- Orçamento ----
        public const string OrcamentoVer = "orcamento.ver";
        public const string OrcamentoGerir = "orcamento.gerir";

        // ---- Alçada de aprovação ----
        public const string AlcadasGerir = "alcadas.gerir";

        // ---- Contas a pagar ----
        public const string TitulosVer = "titulos.ver";
        public const string TitulosBaixar = "titulos.baixar";

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
            new(OrdensVer,         "Ver ordens de compra", "Enxerga os pedidos emitidos e o saldo a receber.",          "Compras"),
            new(OrdensGerir,       "Gerir ordens de compra","Emite, envia e cancela pedidos ao fornecedor.",            "Compras"),
            new(RecebimentoRegistrar,"Registrar recebimento","Confere a mercadoria que chega e dá entrada no pedido.",  "Compras"),
            new(NotasGerir,        "Receber notas fiscais","Importa o XML da NF-e e confronta com a compra.",           "Compras"),
            new(OrcamentoVer,      "Ver orçamento",        "Consulta a verba e o consumo dos centros de custo.",        "Gestão"),
            new(OrcamentoGerir,    "Gerir orçamento",      "Define a verba mensal de cada centro de custo.",            "Gestão"),
            new(AlcadasGerir,      "Gerir alçadas",        "Define a cadeia de aprovação por valor e as delegações.",   "Gestão"),
            new(TitulosVer,        "Ver contas a pagar",   "Enxerga os títulos gerados e seus vencimentos.",            "Financeiro"),
            new(TitulosBaixar,     "Baixar títulos",       "Registra o pagamento e cancela títulos.",                   "Financeiro"),
            new(EtapasGerir,       "Gerir etapas",         "Customiza as colunas do fluxo até a aprovação.",            "Adicionais"),
            new(LogsVer,           "Ver logs",             "Enxerga o histórico de quem fez o quê.",                    "Sistema"),
            new(LogsSistema,       "Ver logs de sistema",  "Acessos, IPs e exceções. Dado sensível de operação.",        "Sistema"),
            new(LogsStackTrace,    "Ver stacktrace",       "Abre o rastreamento técnico dos erros no log.",             "Sistema"),
            new(UsuariosGerir,     "Gerir usuários",       "Cadastra usuários e concede permissões.",                   "Sistema"),
        ];

        public static readonly string[] Todas = [.. Catalogo.Select(p => p.Chave)];

        public static string Titulo(string chave) =>
            Catalogo.FirstOrDefault(p => p.Chave == chave)?.Titulo ?? chave;
    }
}
