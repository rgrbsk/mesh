using System.Security.Claims;
using Erp.Model.Acesso;
using Erp.Model.Aprovacao;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CentroCusto = Erp.Model.CentroCusto.CentroCusto;
using Produto = Erp.Model.Produto.Produto;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Data
{
    /// <summary>
    /// Cenário de demonstração: deixa os cadastros com nomes que contam o
    /// fluxo sozinhos — quem pede, quem aprova o quê, quem compra.
    ///
    /// Roda sozinho UMA vez, na primeira subida depois de existir (ver
    /// AplicarUmaVezAsync); nas seguintes não toca em nada, para não desfazer o
    /// que for ajustado pela tela. Os passos são idempotentes.
    ///
    /// Não apaga solicitações, cotações nem notas: só renomeia e reorganiza os
    /// CADASTROS. Registros antigos continuam apontando para os mesmos ids.
    ///
    /// <code>
    /// PRODUÇÃO (1)                         responsável: Usuário Diretoria
    ///  ├─ EPI E SEGURANÇA DO TRABALHO (1.01)   alçada: Gestor EPI até R$ 500 → Diretoria
    ///  └─ MATERIAIS DE PRODUÇÃO (1.02)         alçada: Gestor Materiais até R$ 1.000 → Diretoria
    /// ADMINISTRATIVO (2)                   responsável: Usuário Diretoria (sem alçada)
    /// </code>
    /// </summary>
    public static class CenarioDemo
    {
        public const string SenhaPadrao = "Senha@123";

        private const string PapelAdmin = DbSeeder.PapelAdmin;
        private const string PapelSolicitante = "Solicitante";
        private const string PapelAprovador = "Aprovador";
        private const string PapelComprador = "Comprador";

        private sealed record Persona(
            string Email, string[] EmailsAntigos, string Nome, string Sobrenome, string Cargo, string Papel);

        private static readonly Persona[] Pessoas =
        [
            new("admin@demo.com",             [],                       "Administrador", "do Sistema",          "Administração do sistema",          PapelAdmin),
            new("solicitante@demo.com",       ["admin@demo1.com"],      "Usuário",       "Solicitante",         "Encarregado de produção",           PapelSolicitante),
            new("gestor.materiais@demo.com",  ["aprovador@demo.com"],   "Usuário",       "Gestor Materiais",    "Gestor de materiais de produção",   PapelAprovador),
            new("gestor.epi@demo.com",        [],                       "Usuário",       "Gestor EPI",          "Técnico de segurança do trabalho",  PapelAprovador),
            new("diretoria@demo.com",         [],                       "Usuário",       "Diretoria",           "Diretor industrial",                PapelAprovador),
            new("compras@demo.com",           ["comprador@demo.com"],   "Usuário",       "Compras",             "Comprador",                         PapelComprador),
        ];

        private static readonly Dictionary<string, (string Descricao, string[] Permissoes)> Papeis = new()
        {
            [PapelSolicitante] = ("Abre solicitações e acompanha o andamento.",
                [Permissoes.ComprasVer, Permissoes.ComprasCriar, Permissoes.ProdutosVer]),
            [PapelAprovador] = ("Decide os itens dos centros de custo pelos quais responde.",
                [Permissoes.ComprasVer, Permissoes.ComprasAprovar, Permissoes.ProdutosVer]),
            [PapelComprador] = ("Cota, escolhe fornecedor e confere a nota fiscal.",
                [Permissoes.ComprasVer, Permissoes.ComprasCriar, Permissoes.CotacoesGerir, Permissoes.NotasGerir,
                 Permissoes.FornecedorVer, Permissoes.FornecedorEditar, Permissoes.ProdutosVer, Permissoes.ProdutosEditar]),
        };

        /// <summary>
        /// Aplica o cenário só se ele ainda não terminou de ser aplicado. A marca
        /// é o de-para da Plotag para o papel 1,07 m, gravado no ÚLTIMO passo:
        /// se algo falhar no meio, a próxima subida tenta de novo (os passos
        /// são idempotentes); depois de completo, nunca mais roda.
        /// </summary>
        public static async Task AplicarUmaVezAsync(IServiceProvider services, ILogger logger)
        {
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var concluido = await db.ProdutosFornecedor.AnyAsync(d =>
                    d.CodigoFornecedor == "1070100752" && d.Produto!.Codigo == "PAP-107-100"
                    && d.DescricaoFornecedor.StartsWith("PAPEL MAXPLOT-1070"))
                    && await db.Produtos.AnyAsync(p => p.Codigo == "PAP-A4-500");

                if (concluido)
                    return;
            }

            await AplicarAsync(services, logger);
        }

        public static async Task AplicarAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<AppDbContext>();
            var usuarios = sp.GetRequiredService<UserManager<Usuario>>();
            var papeis = sp.GetRequiredService<RoleManager<Papel>>();

            await AjustarPapeis(papeis, usuarios);
            var ids = await AjustarUsuarios(usuarios);
            var centros = await AjustarCentros(db, ids);
            await AjustarAlcadas(db, centros, ids);
            var fornecedores = await AjustarFornecedores(db);
            await AjustarProdutos(db, centros, fornecedores);
            await AjustarDePara(db, fornecedores);

            logger.LogWarning("Cenário de demonstração aplicado. Senha de todos os usuários: {Senha}", SenhaPadrao);
        }

        // ------------------------------------------------------------------
        // Papéis: um por função. Permissão direta no usuário fica vazia, para
        // que a tela de Usuários mostre a função de cada um só pelo papel.
        // ------------------------------------------------------------------

        private static async Task AjustarPapeis(RoleManager<Papel> papeis, UserManager<Usuario> usuarios)
        {
            // O papel administrador foi renomeado à mão em algum momento
            // ("Administrador1"); o seed procura pelo nome original. Junta os
            // dois de novo e remove a cópia.
            var admin = await papeis.FindByNameAsync(PapelAdmin);
            if (admin is null)
            {
                admin = new Papel(PapelAdmin);
                await papeis.CreateAsync(admin);
            }

            admin.Descricao = "Acesso total ao sistema.";
            await papeis.UpdateAsync(admin);
            await DefinirPermissoes(papeis, admin, Permissoes.Todas);

            foreach (var outro in papeis.Roles.Where(r => r.Name != null && r.Name.StartsWith(PapelAdmin) && r.Name != PapelAdmin).ToList())
            {
                foreach (var membro in await usuarios.GetUsersInRoleAsync(outro.Name!))
                {
                    await usuarios.RemoveFromRoleAsync(membro, outro.Name!);
                    if (!await usuarios.IsInRoleAsync(membro, PapelAdmin))
                        await usuarios.AddToRoleAsync(membro, PapelAdmin);
                }

                await papeis.DeleteAsync(outro);
            }

            foreach (var (nome, (descricao, permissoes)) in Papeis)
            {
                var papel = await papeis.FindByNameAsync(nome);
                if (papel is null)
                {
                    papel = new Papel(nome);
                    await papeis.CreateAsync(papel);
                }

                papel.Descricao = descricao;
                await papeis.UpdateAsync(papel);
                await DefinirPermissoes(papeis, papel, permissoes);
            }
        }

        private static async Task DefinirPermissoes(RoleManager<Papel> papeis, Papel papel, IEnumerable<string> desejadas)
        {
            var alvo = desejadas.ToHashSet();
            var atuais = (await papeis.GetClaimsAsync(papel)).Where(c => c.Type == Permissoes.ClaimType).ToList();

            foreach (var sobra in atuais.Where(c => !alvo.Contains(c.Value)))
                await papeis.RemoveClaimAsync(papel, sobra);

            foreach (var falta in alvo.Except(atuais.Select(c => c.Value)))
                await papeis.AddClaimAsync(papel, new Claim(Permissoes.ClaimType, falta));
        }

        // ------------------------------------------------------------------
        // Usuários: renomeia os que já existem (mantém o id, então solicitações
        // e decisões antigas continuam ligadas a eles) e cria os que faltam.
        // ------------------------------------------------------------------

        private static async Task<Dictionary<string, Guid>> AjustarUsuarios(UserManager<Usuario> usuarios)
        {
            var ids = new Dictionary<string, Guid>();

            foreach (var p in Pessoas)
            {
                var usuario = await usuarios.FindByEmailAsync(p.Email);

                foreach (var antigo in p.EmailsAntigos)
                    usuario ??= await usuarios.FindByEmailAsync(antigo);

                if (usuario is null)
                {
                    usuario = new Usuario { UserName = p.Email, Email = p.Email, EmailConfirmed = true };
                    var criado = await usuarios.CreateAsync(usuario, SenhaPadrao);
                    if (!criado.Succeeded)
                        throw new InvalidOperationException($"Não foi possível criar {p.Email}: "
                            + string.Join("; ", criado.Errors.Select(e => e.Description)));
                }
                else
                {
                    if (!string.Equals(usuario.Email, p.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        await usuarios.SetEmailAsync(usuario, p.Email);
                        await usuarios.SetUserNameAsync(usuario, p.Email);
                        usuario.EmailConfirmed = true;
                    }

                    // Senha conhecida para a demonstração: entrar como cada
                    // persona é o que mostra a separação de responsabilidades.
                    await usuarios.RemovePasswordAsync(usuario);
                    await usuarios.AddPasswordAsync(usuario, SenhaPadrao);
                    await usuarios.SetLockoutEndDateAsync(usuario, null);
                    await usuarios.ResetAccessFailedCountAsync(usuario);
                }

                usuario.Nome = p.Nome;
                usuario.Sobrenome = p.Sobrenome;
                usuario.Cargo = p.Cargo;
                usuario.Status = Erp.Model.Usuario.StatusUsuario.Ativo;
                usuario.DataModificacao = DateTime.UtcNow;
                await usuarios.UpdateAsync(usuario);

                foreach (var papelAtual in await usuarios.GetRolesAsync(usuario))
                    if (papelAtual != p.Papel)
                        await usuarios.RemoveFromRoleAsync(usuario, papelAtual);

                if (!await usuarios.IsInRoleAsync(usuario, p.Papel))
                    await usuarios.AddToRoleAsync(usuario, p.Papel);

                var diretas = (await usuarios.GetClaimsAsync(usuario)).Where(c => c.Type == Permissoes.ClaimType).ToList();
                if (diretas.Count > 0)
                    await usuarios.RemoveClaimsAsync(usuario, diretas);

                await usuarios.UpdateSecurityStampAsync(usuario);

                ids[p.Email] = usuario.Id;
            }

            return ids;
        }

        // ------------------------------------------------------------------
        // Centros de custo
        // ------------------------------------------------------------------

        private sealed record Centros(CentroCusto Producao, CentroCusto Epi, CentroCusto Materiais, CentroCusto Administrativo);

        private static async Task<Centros> AjustarCentros(AppDbContext db, Dictionary<string, Guid> ids)
        {
            var diretoria = ids["diretoria@demo.com"];

            async Task<CentroCusto> Centro(string codigo, string[] codigosAntigos, string nome, CentroCusto? pai, Guid responsavel)
            {
                var centro = await db.CentrosCusto.FirstOrDefaultAsync(c => c.Codigo == codigo);

                foreach (var antigo in codigosAntigos)
                    centro ??= await db.CentrosCusto.FirstOrDefaultAsync(c => c.Codigo == antigo);

                if (centro is null)
                {
                    centro = new CentroCusto();
                    db.CentrosCusto.Add(centro);
                }

                centro.Codigo = codigo;
                centro.Nome = nome;
                centro.Pai = pai;
                centro.PaiId = pai?.Id;
                centro.ResponsavelId = responsavel;
                centro.Ativo = true;
                centro.BloqueiaAcimaOrcamento = false;
                centro.ModificadoEm = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return centro;
            }

            var producao = await Centro("1", [], "PRODUÇÃO", null, diretoria);
            var epi = await Centro("1.01", [], "EPI E SEGURANÇA DO TRABALHO", producao, ids["gestor.epi@demo.com"]);
            var materiais = await Centro("1.02", [], "MATERIAIS DE PRODUÇÃO", producao, ids["gestor.materiais@demo.com"]);
            var administrativo = await Centro("2", ["9"], "ADMINISTRATIVO", null, diretoria);

            return new Centros(producao, epi, materiais, administrativo);
        }

        // ------------------------------------------------------------------
        // Alçadas: o gestor da área decide até um teto; acima dele, a decisão
        // sobe para a Diretoria. É o que mostra a cadeia funcionando.
        // ------------------------------------------------------------------

        private static async Task AjustarAlcadas(AppDbContext db, Centros centros, Dictionary<string, Guid> ids)
        {
            var diretoria = ids["diretoria@demo.com"];

            var cadeias = new (CentroCusto Centro, (Guid Aprovador, decimal Limite)[] Degraus)[]
            {
                (centros.Epi,       [(ids["gestor.epi@demo.com"], 500m),        (diretoria, 0m)]),
                (centros.Materiais, [(ids["gestor.materiais@demo.com"], 1000m), (diretoria, 0m)]),
                (centros.Producao,  []),
                (centros.Administrativo, []),
            };

            foreach (var (centro, degraus) in cadeias)
            {
                db.Alcadas.RemoveRange(db.Alcadas.Where(a => a.CentroCustoId == centro.Id));

                var ordem = 1;
                foreach (var (aprovador, limite) in degraus)
                {
                    db.Alcadas.Add(new AlcadaAprovacao
                    {
                        CentroCustoId = centro.Id,
                        Ordem = ordem++,
                        AprovadorId = aprovador,
                        LimiteValor = limite,
                        Ativo = true,
                    });
                }
            }

            // Delegações de teste em aberto confundiriam quem decide o quê.
            foreach (var delegacao in await db.Delegacoes.Where(d => d.Ativo).ToListAsync())
                delegacao.Ativo = false;

            await db.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Fornecedores. E-mails em example.com: domínio reservado, nenhum
        // convite de teste sai para uma caixa real.
        // ------------------------------------------------------------------

        private sealed record Fornecedores(
            Erp.Model.Pessoa.Pessoa Plotag, Erp.Model.Pessoa.Pessoa Papelaria,
            Erp.Model.Pessoa.Pessoa ProtecaoTotal, Erp.Model.Pessoa.Pessoa SeguraEpi);

        private static async Task<Fornecedores> AjustarFornecedores(AppDbContext db)
        {
            async Task<Erp.Model.Pessoa.Pessoa> Fornecedor(string cnpj, string razao, string fantasia, string email)
            {
                var todas = await db.Pessoas.ToListAsync();
                var pessoa = todas.FirstOrDefault(p => SoDigitos(p.CNPJ) == cnpj);

                if (pessoa is null)
                {
                    pessoa = new Erp.Model.Pessoa.Pessoa { CNPJ = cnpj };
                    db.Pessoas.Add(pessoa);
                }

                pessoa.Tipo = Erp.Model.Pessoa.TipoPessoa.Fornecedor;
                pessoa.RazaoSocial = razao;
                pessoa.NomeFantasia = fantasia;
                pessoa.Natureza = "Pessoa Jurídica";
                pessoa.Email = email;
                pessoa.ModificadoEm = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return pessoa;
            }

            return new Fornecedores(
                await Fornecedor("00822602000124", "Plotag Sistemas e Suprimentos Ltda", "Plotag", "vendas@plotag.example.com"),
                await Fornecedor("01010101010010", "Claudio Papelaria Ltda", "Claudio Papelaria", "vendas@claudiopapelaria.example.com"),
                await Fornecedor("11222333000181", "Proteção Total Comércio de EPIs Ltda", "Proteção Total EPI", "vendas@protecaototal.example.com"),
                await Fornecedor("22334455000186", "Segura Distribuidora de EPIs Ltda", "Segura EPI", "comercial@seguraepi.example.com"));
        }

        // ------------------------------------------------------------------
        // Produtos. Os parâmetros foram escolhidos para que parte do catálogo
        // esteja no ponto de pedido (aparece em "Repor agora") e parte não, e
        // para que os preços cruzem os tetos das alçadas em quantidades
        // plausíveis: 100 pares de luva (R$ 480) o Gestor EPI aprova sozinho;
        // 150 pares (R$ 720) sobem para a Diretoria.
        // ------------------------------------------------------------------

        private static async Task AjustarProdutos(AppDbContext db, Centros centros, Fornecedores f)
        {
            async Task SalvarProduto(string codigo, string[] antigos, string descricao, string unidade,
                decimal saldo, decimal minimo, decimal consumo, int prazo, decimal pontoPedido, decimal preco,
                CentroCusto centro, Erp.Model.Pessoa.Pessoa fornecedor)
            {
                var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Codigo == codigo);

                foreach (var antigo in antigos)
                    produto ??= await db.Produtos.FirstOrDefaultAsync(p => p.Codigo == antigo);

                if (produto is null)
                {
                    produto = new Produto();
                    db.Produtos.Add(produto);
                }

                produto.Codigo = codigo;
                produto.Descricao = descricao;
                produto.Unidade = unidade;
                produto.SaldoAtual = saldo;
                produto.EstoqueMinimo = minimo;
                produto.ConsumoMedioDiario = consumo;
                produto.PrazoEntregaDias = prazo;
                produto.PontoPedido = pontoPedido;
                produto.PrecoReferencia = preco;
                produto.PrecoReferenciaEm = DateTime.UtcNow;
                produto.CentroCustoPadraoId = centro.Id;
                produto.FornecedorPadraoId = fornecedor.Id;
                produto.Ativo = true;
                produto.ModificadoEm = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }

            //        código          antigos   descrição                                      un.    saldo  mín   cons.  prazo PP     preço
            // Materiais de produção — Plotag
            await SalvarProduto("PAP-107-100",  [],       "Papel plotter 1,07 m x 100 m 75 g",          "RL",    1,    2,   0.5m,  7,    6,     48.91m, centros.Materiais, f.Plotag);
            await SalvarProduto("PAP-170-250",  [],       "Papel plotter 1,70 m x 250 m 56 g",          "RL",    1,    2,   0.5m,  7,    6,    138.30m, centros.Materiais, f.Plotag);
            await SalvarProduto("TIN-PLT-PRT",  [],       "Tinta para plotter preta 1 L",               "UN",    6,    2,   0.2m, 10,    4,    189.00m, centros.Materiais, f.Plotag);
            await SalvarProduto("FIT-ADE-48",   [],       "Fita adesiva transparente 48 mm x 100 m",    "RL",   40,   20,     2m,  5,   30,      7.90m, centros.Materiais, f.Plotag);

            // EPI — Proteção Total / Segura EPI
            await SalvarProduto("EPI-LUV-NIT-M",["TES"],  "Luva nitrílica tamanho M",                   "PAR",  30,   50,    10m,  5,  100,      4.80m, centros.Epi, f.ProtecaoTotal);
            await SalvarProduto("EPI-CAP-B",    [],       "Capacete de segurança classe B com jugular", "UN",   25,   10,   0.3m, 15,   15,     39.90m, centros.Epi, f.ProtecaoTotal);
            await SalvarProduto("EPI-OCL-INC",  [],       "Óculos de proteção lente incolor",           "UN",    8,   15,     1m,  7,   22,     12.50m, centros.Epi, f.SeguraEpi);
            await SalvarProduto("EPI-PRT-AUR",  [],       "Protetor auricular tipo plug",               "PAR", 200,  100,     8m,  7,  156,      1.90m, centros.Epi, f.SeguraEpi);

            // Administrativo — papelaria
            await SalvarProduto("PAP-A4-500",   [],       "Papel sulfite A4 75 g (resma 500 folhas)",   "RS",   12,   10,     1m,  3,   13,     27.90m, centros.Administrativo, f.Papelaria);
        }

        // ------------------------------------------------------------------
        // De-para da Plotag: os códigos dela na NF-e de exemplo apontam para os
        // nossos dois papéis. Os outros dois códigos da nota ficam sem
        // correspondência de propósito — aparecem como "fora da compra" no
        // confronto, que é uma das situações que a conferência precisa mostrar.
        // ------------------------------------------------------------------

        private static async Task AjustarDePara(AppDbContext db, Fornecedores f)
        {
            var papel107 = await db.Produtos.FirstAsync(p => p.Codigo == "PAP-107-100");
            var papel170 = await db.Produtos.FirstAsync(p => p.Codigo == "PAP-170-250");

            var desejado = new Dictionary<string, (int ProdutoId, string Descricao)>
            {
                ["1070100752"] = (papel107.Id, "PAPEL MAXPLOT-1070X100MX75GRS 2\""),
                ["B17025056"] = (papel170.Id, "PAPEL MAXPLOT-170MX250MX56GRS 3\""),
            };

            var atuais = await db.ProdutosFornecedor.Where(d => d.FornecedorId == f.Plotag.Id).ToListAsync();

            // Associação feita a outro produto da Plotag que não seja um destes
            // dois códigos foi teste: sai.
            db.ProdutosFornecedor.RemoveRange(atuais.Where(d => !desejado.ContainsKey(d.CodigoFornecedor)));

            foreach (var (codigo, (produtoId, descricao)) in desejado)
            {
                var linha = atuais.FirstOrDefault(d => d.CodigoFornecedor == codigo);
                if (linha is null)
                {
                    linha = new Erp.Model.Produto.ProdutoFornecedor { FornecedorId = f.Plotag.Id, CodigoFornecedor = codigo };
                    db.ProdutosFornecedor.Add(linha);
                }

                linha.ProdutoId = produtoId;
                linha.DescricaoFornecedor = descricao;
                linha.ModificadoEm = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }

        private static string SoDigitos(string? texto) =>
            new string((texto ?? "").Where(char.IsDigit).ToArray());
    }
}
