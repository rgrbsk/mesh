using Erp.Data;
using Erp.Model.Acesso;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Relatorio
{
    /// <summary>Resultado genérico: colunas e linhas. É o que permite a tela ser
    /// uma só para todos os relatórios.</summary>
    public sealed record ResultadoRelatorio(List<string> Colunas, List<string[]> Linhas)
    {
        public bool Vazio => Linhas.Count == 0;
    }

    /// <summary>
    /// Um relatório do catálogo. A consulta é SQL porque relatório é agregação
    /// de leitura com forma variável — em LINQ tipado cada um exigiria uma
    /// classe de resultado, e a tela deixaria de ser genérica.
    /// </summary>
    public sealed record Relatorio(
        string Chave,
        string Modulo,
        string Titulo,
        string Descricao,
        string? Permissao,
        string Sql,
        bool FiltraPorData = true,
        // Lê do banco central (log de sistema), filtrado pelos usuários do tenant.
        bool Central = false);

    /// <summary>
    /// Catálogo de relatórios + execução.
    ///
    /// O SQL é FIXO no código, nunca montado com entrada do usuário: os únicos
    /// parâmetros são as duas datas, e vão como parâmetros de verdade. Relatório
    /// que aceita SQL de fora é injeção com outro nome.
    /// </summary>
    public class RelatorioRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly Erp.Data.Tenancy.ConexoesTenant _conexoes;
        private readonly Erp.Data.Tenancy.TenantAtual _tenant;

        public RelatorioRepository(
            IDbContextFactory<AppDbContext> fabrica,
            Erp.Data.Tenancy.ConexoesTenant conexoes,
            Erp.Data.Tenancy.TenantAtual tenant)
        {
            _fabrica = fabrica;
            _conexoes = conexoes;
            _tenant = tenant;
        }

        public static readonly IReadOnlyList<Relatorio> Catalogo =
        [
            // ---------------- Compras ----------------
            new("solicitacoes-situacao", "Compras",
                "Solicitações por situação",
                "Quantas estão em cada estado e o total de itens envolvidos.",
                Permissoes.ComprasVer,
                """
                SELECT CASE s."Status"
                         WHEN 0 THEN 'Rascunho' WHEN 1 THEN 'Aguardando aprovação'
                         WHEN 2 THEN 'Aprovada'  WHEN 3 THEN 'Aprovada em parte'
                         WHEN 4 THEN 'Recusada'  WHEN 5 THEN 'Devolvida'
                         ELSE 'Desconhecida' END               AS "Situação",
                       COUNT(DISTINCT s."Id")                  AS "Solicitações",
                       COUNT(i."Id")                           AS "Itens"
                FROM "Solicitacoes" s
                LEFT JOIN "ItensSolicitacao" i ON i."SolicitacaoId" = s."Id"
                WHERE s."CriadoEm" BETWEEN @de AND @ate
                GROUP BY s."Status"
                ORDER BY 2 DESC
                """),

            new("itens-centro-custo", "Compras",
                "Itens por centro de custo",
                "Onde o gasto está sendo comprometido, por quantidade pedida.",
                Permissoes.ComprasVer,
                """
                SELECT cc."Codigo"                              AS "Código",
                       cc."Nome"                                AS "Centro de custo",
                       COUNT(i."Id")                            AS "Linhas",
                       ROUND(SUM(i."Quantidade"), 2)            AS "Quantidade",
                       COUNT(*) FILTER (WHERE i."Status" = 0)   AS "Pendentes"
                FROM "ItensSolicitacao" i
                JOIN "CentrosCusto" cc ON cc."Id" = i."CentroCustoId"
                JOIN "Solicitacoes" s ON s."Id" = i."SolicitacaoId"
                WHERE s."CriadoEm" BETWEEN @de AND @ate
                GROUP BY cc."Codigo", cc."Nome"
                ORDER BY 4 DESC
                """),

            new("solicitacoes-solicitante", "Compras",
                "Solicitações por solicitante",
                "Quem mais pede, e quanto do que pediu foi aprovado.",
                Permissoes.ComprasVer,
                """
                SELECT TRIM(CONCAT(u."Nome", ' ', u."Sobrenome"))       AS "Solicitante",
                       COUNT(*)                                         AS "Solicitações",
                       COUNT(*) FILTER (WHERE s."Status" IN (2,3))      AS "Aprovadas",
                       COUNT(*) FILTER (WHERE s."Status" = 4)           AS "Recusadas"
                FROM "Solicitacoes" s
                JOIN "AspNetUsers" u ON u."Id" = s."SolicitanteId"
                WHERE s."CriadoEm" BETWEEN @de AND @ate
                GROUP BY 1
                ORDER BY 2 DESC
                """),

            new("aprovacoes-tempo", "Compras",
                "Tempo de aprovação",
                "Quanto tempo cada aprovador leva entre o envio e a decisão.",
                Permissoes.ComprasVer,
                """
                SELECT TRIM(CONCAT(u."Nome", ' ', u."Sobrenome"))            AS "Aprovador",
                       COUNT(*)                                              AS "Decisões",
                       COUNT(*) FILTER (WHERE i."Status" = 1)                AS "Aprovou",
                       COUNT(*) FILTER (WHERE i."Status" = 2)                AS "Recusou",
                       COUNT(*) FILTER (WHERE i."Status" = 3)                AS "Devolveu",
                       ROUND(AVG(EXTRACT(EPOCH FROM (i."DecididoEm" - s."EnviadaEm")) / 3600)::numeric, 1)
                                                                             AS "Horas em média"
                FROM "ItensSolicitacao" i
                JOIN "Solicitacoes" s ON s."Id" = i."SolicitacaoId"
                JOIN "AspNetUsers" u ON u."Id" = i."DecididoPorId"
                WHERE i."DecididoEm" BETWEEN @de AND @ate
                GROUP BY 1
                ORDER BY 2 DESC
                """),

            // ---------------- Cotações ----------------
            new("cotacoes-situacao", "Cotações",
                "Cotações por situação",
                "Rodadas abertas, encerradas e quantas propostas cada estado acumula.",
                Permissoes.CotacoesGerir,
                """
                SELECT CASE c."Status" WHEN 0 THEN 'Rascunho' WHEN 1 THEN 'Aberta'
                                       WHEN 2 THEN 'Encerrada' ELSE 'Desconhecida' END AS "Situação",
                       COUNT(DISTINCT c."Id")                                          AS "Cotações",
                       COUNT(DISTINCT f."Id")                                          AS "Convites",
                       COUNT(DISTINCT f."Id") FILTER (WHERE f."Status" >= 1)           AS "Responderam"
                FROM "Cotacoes" c
                LEFT JOIN "Convites" f ON f."CotacaoId" = c."Id"
                WHERE c."CriadoEm" BETWEEN @de AND @ate
                GROUP BY c."Status"
                ORDER BY 2 DESC
                """),

            new("fornecedores-desempenho", "Cotações",
                "Desempenho dos fornecedores",
                "Quantas vezes foi convidado, respondeu e venceu.",
                Permissoes.CotacoesGerir,
                """
                SELECT COALESCE(NULLIF(f."RazaoSocial", ''), p."RazaoSocial", f."Email") AS "Fornecedor",
                       COUNT(DISTINCT f."Id")                                            AS "Convites",
                       COUNT(DISTINCT f."Id") FILTER (WHERE f."Status" >= 1)             AS "Respostas",
                       COUNT(DISTINCT ci."Id")                                           AS "Itens vencidos"
                FROM "Convites" f
                LEFT JOIN "Pessoas" p ON p."Id" = f."PessoaId"
                LEFT JOIN "CotacaoItens" ci ON ci."ConviteVencedorId" = f."Id"
                WHERE f."CriadoEm" BETWEEN @de AND @ate
                GROUP BY 1
                ORDER BY 4 DESC, 3 DESC
                """),

            new("economia-escolha", "Cotações",
                "Escolha × menor preço",
                "Quanto foi pago acima do menor preço, e em quais itens.",
                Permissoes.CotacoesGerir,
                """
                SELECT pr."Descricao"                                     AS "Produto",
                       ROUND(ci."Quantidade", 2)                          AS "Qtde.",
                       ROUND(vencedora."PrecoUnitario", 2)                AS "Escolhido",
                       ROUND(menor.preco, 2)                              AS "Menor preço",
                       ROUND((vencedora."PrecoUnitario" - menor.preco) * ci."Quantidade", 2)
                                                                          AS "Diferença",
                       COALESCE(NULLIF(ci."MotivoEscolha", ''), '—')      AS "Motivo"
                FROM "CotacaoItens" ci
                JOIN "Produtos" pr ON pr."Id" = ci."ProdutoId"
                JOIN "Propostas" vencedora
                     ON vencedora."CotacaoItemId" = ci."Id"
                    AND vencedora."ConviteFornecedorId" = ci."ConviteVencedorId"
                JOIN LATERAL (
                     SELECT MIN(p2."PrecoUnitario") AS preco
                     FROM "Propostas" p2
                     WHERE p2."CotacaoItemId" = ci."Id" AND p2."PrecoUnitario" IS NOT NULL
                ) menor ON TRUE
                WHERE ci."ConviteVencedorId" IS NOT NULL
                  AND ci."EscolhidoEm" BETWEEN @de AND @ate
                ORDER BY 5 DESC
                """),

            // ---------------- Notas fiscais ----------------
            new("notas-fornecedor", "Notas fiscais",
                "Notas por fornecedor",
                "Quanto entrou de cada um, e quantas vieram pelo link do próprio fornecedor.",
                Permissoes.NotasGerir,
                """
                SELECT n."EmitenteNome"                                       AS "Fornecedor",
                       COUNT(*)                                               AS "Notas",
                       ROUND(SUM(n."ValorTotal"), 2)                          AS "Valor total",
                       COUNT(*) FILTER (WHERE n."ImportadaPorId" IS NULL)     AS "Pelo link",
                       COUNT(*) FILTER (WHERE n."Ambiente" = 2)               AS "Homologação"
                FROM "NotasFiscais" n
                WHERE n."ImportadaEm" BETWEEN @de AND @ate
                GROUP BY 1
                ORDER BY 3 DESC
                """),

            new("notas-sem-vinculo", "Notas fiscais",
                "Notas sem confronto possível",
                "Sem cotação ou sem fornecedor casado — ninguém consegue conferir.",
                Permissoes.NotasGerir,
                """
                SELECT n."Numero"                                        AS "Nota",
                       n."EmitenteNome"                                  AS "Emitente",
                       n."EmitenteCnpj"                                  AS "CNPJ",
                       ROUND(n."ValorTotal", 2)                          AS "Valor",
                       CASE WHEN n."FornecedorId" IS NULL THEN 'Sem cadastro'
                            ELSE 'Sem cotação' END                       AS "Falta"
                FROM "NotasFiscais" n
                WHERE (n."CotacaoId" IS NULL OR n."FornecedorId" IS NULL)
                  AND n."ImportadaEm" BETWEEN @de AND @ate
                ORDER BY 4 DESC
                """),

            // ---------------- Produtos ----------------
            new("produtos-repor", "Produtos",
                "Produtos a repor",
                "Saldo no ponto de pedido ou abaixo. Não depende do período.",
                Permissoes.ProdutosVer,
                """
                SELECT p."Codigo"                                    AS "Código",
                       p."Descricao"                                 AS "Produto",
                       ROUND(p."SaldoAtual", 2)                      AS "Saldo",
                       ROUND(p."EstoqueMinimo", 2)                   AS "Mínimo",
                       ROUND(p."PontoPedido", 2)                     AS "Ponto de pedido",
                       CASE WHEN p."SaldoAtual" <= p."EstoqueMinimo" THEN 'Abaixo do mínimo'
                            ELSE 'Repor' END                         AS "Situação"
                FROM "Produtos" p
                WHERE p."Ativo" AND p."SaldoAtual" <= p."PontoPedido"
                ORDER BY 3
                """,
                FiltraPorData: false),

            new("produtos-comprados", "Produtos",
                "Mais comprados",
                "O que mais aparece em solicitação, por quantidade.",
                Permissoes.ProdutosVer,
                """
                SELECT p."Codigo"                        AS "Código",
                       p."Descricao"                     AS "Produto",
                       COUNT(i."Id")                     AS "Pedidos",
                       ROUND(SUM(i."Quantidade"), 2)     AS "Quantidade"
                FROM "ItensSolicitacao" i
                JOIN "Produtos" p ON p."Id" = i."ProdutoId"
                JOIN "Solicitacoes" s ON s."Id" = i."SolicitacaoId"
                WHERE s."CriadoEm" BETWEEN @de AND @ate
                GROUP BY 1, 2
                ORDER BY 4 DESC
                """),

            // ---------------- Cadastros ----------------
            new("pessoas-tipo", "Cadastros",
                "Pessoas por tipo",
                "Composição do cadastro. Não depende do período.",
                Permissoes.FornecedorVer,
                """
                SELECT CASE p."Tipo"
                         WHEN 0 THEN 'Indeterminado' WHEN 1 THEN 'Cliente'
                         WHEN 2 THEN 'Colaborador'   WHEN 3 THEN 'Vendedor'
                         WHEN 4 THEN 'Fornecedor'    WHEN 5 THEN 'Prospect'
                         WHEN 6 THEN 'Lead'          WHEN 7 THEN 'Parceiro'
                         WHEN 8 THEN 'Transportadora' WHEN 9 THEN 'Distribuidor'
                         WHEN 10 THEN 'Representante' WHEN 11 THEN 'Consultor'
                         ELSE 'Outro' END                                   AS "Tipo",
                       COUNT(*)                                             AS "Cadastros",
                       COUNT(*) FILTER (WHERE p."Email" IS NOT NULL AND p."Email" <> '')
                                                                            AS "Com e-mail"
                FROM "Pessoas" p
                GROUP BY p."Tipo"
                ORDER BY 2 DESC
                """,
                FiltraPorData: false),

            new("centros-responsavel", "Cadastros",
                "Centros de custo e aprovadores",
                "Centro sem responsável trava a aprovação — aparece aqui em branco.",
                Permissoes.CentrosCustoGerir,
                """
                SELECT cc."Codigo"                                         AS "Código",
                       cc."Nome"                                           AS "Centro de custo",
                       COALESCE(TRIM(CONCAT(u."Nome", ' ', u."Sobrenome")), '— SEM RESPONSÁVEL —')
                                                                           AS "Aprovador",
                       COUNT(i."Id") FILTER (WHERE i."Status" = 0)         AS "Itens parados"
                FROM "CentrosCusto" cc
                LEFT JOIN "AspNetUsers" u ON u."Id" = cc."ResponsavelId"
                LEFT JOIN "ItensSolicitacao" i ON i."CentroCustoId" = cc."Id"
                GROUP BY cc."Codigo", cc."Nome", 3
                ORDER BY 4 DESC
                """,
                FiltraPorData: false),

            // ---------------- Sistema ----------------
            new("acessos-usuario", "Sistema",
                "Acessos por usuário",
                "Entradas e tentativas falhas, com o último acesso.",
                Permissoes.LogsSistema,
                """
                SELECT COALESCE(NULLIF(l."UsuarioNome", ''), l."Identificacao") AS "Usuário",
                       COUNT(*) FILTER (WHERE l."Evento" = 0)                   AS "Entrou",
                       COUNT(*) FILTER (WHERE l."Evento" = 1)                   AS "Falhou",
                       COUNT(DISTINCT l."Ip")                                   AS "IPs",
                       TO_CHAR(MAX(l."Quando"), 'DD/MM/YYYY HH24:MI')           AS "Último acesso"
                FROM "LogsSistema" l
                WHERE l."Evento" IN (0, 1) AND l."Quando" BETWEEN @de AND @ate
                  AND l."UsuarioId" = ANY(@usuarios)
                GROUP BY 1
                ORDER BY 2 DESC
                """,
                Central: true),

            new("atividade-modulo", "Sistema",
                "Atividade por módulo",
                "O que foi feito em cada parte do sistema, e por quantas pessoas.",
                Permissoes.LogsVer,
                """
                SELECT l."Modulo"                          AS "Módulo",
                       COUNT(*)                            AS "Ações",
                       COUNT(DISTINCT l."UsuarioId")       AS "Pessoas",
                       TO_CHAR(MAX(l."Quando"), 'DD/MM/YYYY HH24:MI') AS "Última ação"
                FROM "Logs" l
                WHERE l."Quando" BETWEEN @de AND @ate
                GROUP BY 1
                ORDER BY 2 DESC
                """),

            new("erros-sistema", "Sistema",
                "Erros registrados",
                "Exceções capturadas, agrupadas por mensagem.",
                Permissoes.LogsSistema,
                """
                SELECT LEFT(l."Mensagem", 90)                        AS "Erro",
                       COUNT(*)                                      AS "Ocorrências",
                       TO_CHAR(MAX(l."Quando"), 'DD/MM/YYYY HH24:MI') AS "Última vez"
                FROM "LogsSistema" l
                WHERE l."Evento" = 5 AND l."Quando" BETWEEN @de AND @ate
                  AND l."UsuarioId" = ANY(@usuarios)
                GROUP BY 1
                ORDER BY 2 DESC
                """,
                Central: true),
        ];

        /// <summary>Módulos que têm relatório, na ordem em que aparecem.</summary>
        public static IReadOnlyList<string> Modulos =>
            Catalogo.Select(r => r.Modulo).Distinct().ToList();

        public async Task<ResultadoRelatorio> Executar(string chave, DateTime de, DateTime ate)
        {
            var relatorio = Catalogo.FirstOrDefault(r => r.Chave == chave)
                ?? throw new InvalidOperationException("Relatório não encontrado.");

            await using var contexto = relatorio.Central
                ? _conexoes.Central()
                : await _fabrica.CreateDbContextAsync();

            var usuarios = Array.Empty<Guid>();
            if (relatorio.Central)
            {
                var empresa = await _tenant.EmpresaId();
                usuarios = await contexto.Usuarios
                    .Where(u => empresa == null || u.EmpresaId == empresa)
                    .Select(u => u.Id)
                    .ToArrayAsync();
            }

            await using var conexao = contexto.Database.GetDbConnection();

            await conexao.OpenAsync();

            await using var comando = conexao.CreateCommand();
            comando.CommandText = relatorio.Sql;

            // Sempre os dois parâmetros, mesmo quando o SQL não os usa: o
            // Postgres ignora parâmetro não referenciado, e assim não é preciso
            // ramificar aqui.
            AdicionarParametro(comando, "de", de);
            AdicionarParametro(comando, "ate", ate);

            if (relatorio.Central)
            {
                var parametro = comando.CreateParameter();
                parametro.ParameterName = "usuarios";
                parametro.Value = usuarios;
                comando.Parameters.Add(parametro);
            }

            await using var leitor = await comando.ExecuteReaderAsync();

            var colunas = Enumerable.Range(0, leitor.FieldCount)
                .Select(leitor.GetName)
                .ToList();

            var linhas = new List<string[]>();

            while (await leitor.ReadAsync())
            {
                var linha = new string[leitor.FieldCount];

                for (var i = 0; i < leitor.FieldCount; i++)
                    linha[i] = leitor.IsDBNull(i) ? "" : Formatar(leitor.GetValue(i));

                linhas.Add(linha);
            }

            return new ResultadoRelatorio(colunas, linhas);
        }

        private static void AdicionarParametro(System.Data.Common.DbCommand comando, string nome, DateTime valor)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = nome;
            parametro.Value = DateTime.SpecifyKind(valor, DateTimeKind.Utc);

            comando.Parameters.Add(parametro);
        }

        /// <summary>
        /// Formata para exibição. Decimal com duas casas e data em dd/MM/yyyy —
        /// o resto vai como veio, porque relatório com número em notação
        /// científica não serve para ninguém.
        /// </summary>
        private static string Formatar(object valor) => valor switch
        {
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            DateTime data => data.ToLocalTime().ToString("dd/MM/yyyy"),
            _ => valor.ToString() ?? "",
        };
    }
}
