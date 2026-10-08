using BlazorBlueprint.Components;
using Erp.Components;
using Erp.Data;
using Erp.Localization;
using Erp.Model.Acesso;
using Erp.Model.Usuario;
using Erp.Repository.Empresa;
using Erp.Service;
using Erp.Service.Acesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Segredos locais (senha do banco, etc.): arquivo git-ignored que sobrepõe o
// appsettings.Development.json. Opcional — em produção o arquivo não existe.
builder.Configuration.AddJsonFile("appsettings.Development.local.json",
    optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Localização pt-BR de todas as strings de UI do BlazorBlueprint (filtros, grid, etc.).
builder.Services.AddBlazorBlueprintComponents(BbLocalizationPtBr.Configure);

// Um banco por tenant. A connection string "Default" é o banco CENTRAL
// (catálogo: login, papéis, tenants, logs de sistema); cada tenant tem o seu
// banco de negócio, com o mesmo modelo.
// Fábrica, não DbContext scoped: no Blazor Server o escopo dura o circuito
// INTEIRO, então um DbContext compartilhado é usado por componentes que renderizam
// em paralelo — dois awaits simultâneos nele estouram "A second operation was
// started on this context instance". Cada operação de repositório abre e fecha
// o seu contexto — no banco do tenant de quem está usando.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Erp.Data.Tenancy.ConexoesTenant>();
builder.Services.AddSingleton<Erp.Data.Tenancy.FabricaCentral>();
builder.Services.AddScoped<Erp.Data.Tenancy.TenantAtual>();
builder.Services.AddScoped<IDbContextFactory<AppDbContext>, Erp.Data.Tenancy.FabricaTenant>();
builder.Services.AddScoped<Erp.Data.Tenancy.EspelhoUsuarios>();
builder.Services.AddScoped<Erp.Service.Plataforma.ProvisionamentoTenant>();

// O Identity (UserManager/SignInManager/stores) exige um AppDbContext scoped —
// e o login mora sempre no banco central.
builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<Erp.Data.Tenancy.ConexoesTenant>().Central());

builder.Services.AddIdentity<Usuario, Papel>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequiredLength = 8;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<Erp.Data.Tenancy.ClaimsComTenant>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/login";
    o.AccessDeniedPath = "/sem-acesso";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});

// revalida o cookie a cada 5 min: papel revogado derruba a sessão sem esperar 8h
builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromMinutes(5));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissaoPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
// O Scan abaixo pega só quem termina em "Repository"; este é serviço, então
// entra à mão. As credenciais vêm do appsettings.Development.local.json.
builder.Services.Configure<Erp.Service.Email.EmailOptions>(
    builder.Configuration.GetSection(Erp.Service.Email.EmailOptions.Secao));
builder.Services.AddScoped<Erp.Service.Email.EmailService>();

builder.Services.Scan(s => s
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
        .AsSelfWithInterfaces()
        .WithScopedLifetime());
// Espelha os erros do ILogger na tabela de log de sistema. Registrado com o
// IServiceProvider raiz porque o provider vive fora de qualquer escopo — ele
// abre o seu próprio a cada gravação.
builder.Services.AddHttpClient();
builder.Services.Configure<Erp.Service.Log.ProvedorIpOptions>(
    builder.Configuration.GetSection(Erp.Service.Log.ProvedorIpOptions.Secao));
builder.Services.AddScoped<Erp.Service.Log.ProvedorIpService>();

builder.Services.AddSingleton<ILoggerProvider>(sp =>
    new Erp.Service.Log.LogSistemaLoggerProvider(sp));

// Telemetria do processo para o painel do dono (requisições, sessões, CPU).
builder.Services.AddSingleton<Erp.Service.Plataforma.MetricasAplicacao>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler,
    Erp.Service.Plataforma.SessoesCircuitHandler>();

var app = builder.Build();

// Seed de desenvolvimento: aplica migrations e cria o usuário demo.
if (app.Environment.IsDevelopment())
{
    try
    {
        await DbSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Seed/migrations não aplicados (Postgres acessível?).");
    }

    // Cenário de demonstração (usuários por função, centros, alçadas,
    // fornecedores e produtos coerentes). Roda UMA vez: depois que existe, o
    // que for ajustado pela tela não é sobrescrito nas próximas subidas.
    try
    {
        await CenarioDemo.AplicarUmaVezAsync(app.Services, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Cenário de demonstração não aplicado.");
    }
}

// Municípios do IBGE: dado de REFERÊNCIA, não de exemplo — roda em qualquer
// ambiente, não só em desenvolvimento. É incremental: a segunda execução não
// insere nada.
try
{
    using var escopo = app.Services.CreateScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

    await CidadeSeeder.SeedAsync(contexto);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Lista de municípios não carregada.");
}

// Bancos dos tenants: cada um recebe as migrations novas ao subir, como o
// central. Um tenant com problema não impede os outros nem a aplicação.
try
{
    var conexoes = app.Services.GetRequiredService<Erp.Data.Tenancy.ConexoesTenant>();
    List<string> bancos;
    await using (var central = conexoes.Central())
        bancos = await central.Empresas.Where(e => e.Banco != "").Select(e => e.Banco).ToListAsync();

    foreach (var banco in bancos.Where(b => !conexoes.EhCentral(b)).Distinct())
    {
        try
        {
            await using var contexto = conexoes.Criar(banco);
            await contexto.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Migrations não aplicadas no banco do tenant {Banco}.", banco);
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Bancos dos tenants não verificados.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Mede cada requisição de página/endpoint. Fica de fora o que não diz nada
// sobre a aplicação: arquivos estáticos e o canal do Blazor (/_blazor), que é
// uma conexão longa e distorceria a latência.
app.Use(async (http, next) =>
{
    var caminho = http.Request.Path.Value ?? "/";
    var ultimo = caminho[(caminho.LastIndexOf('/') + 1)..];

    if (caminho.StartsWith("/_blazor") || caminho.StartsWith("/_framework")
        || caminho.StartsWith("/_content") || ultimo.Contains('.'))
    {
        await next();
        return;
    }

    var relogio = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        http.RequestServices.GetRequiredService<Erp.Service.Plataforma.MetricasAplicacao>()
            .Registrar(http.Request.Method, caminho, http.Response.StatusCode, relogio.Elapsed.TotalMilliseconds);
    }
});

// HTTPS redirect is only enforced outside Development. In local dev the app is
// served over http, and redirecting the Blazor SignalR negotiate to the https
// port makes it cross-origin and breaks the interactive circuit (CORS) — além de
// disparar erro de SSL no navegador quando o cert de dev não é confiável (Firefox).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// Login: valida credenciais e emite o cookie na RESPOSTA HTTP (não dá pra fazer
// isso dentro do circuito interativo — por isso a tela de login é estática e posta aqui).
app.MapPost("/auth/login", async (
    HttpContext http,
    [FromForm] string email,
    [FromForm] string senha,
    [FromForm] bool? lembrar,
    UserManager<Usuario> users,
    SignInManager<Usuario> signIn,
    Erp.Repository.Log.LogSistemaRepository logs,
    Erp.Service.Log.ProvedorIpService provedores,
    Erp.Data.Tenancy.ConexoesTenant conexoes) =>
{
    // Origem da tentativa. Registrada mesmo quando o login falha — é
    // exatamente aí que interessa saber de onde veio.
    var origem = new
    {
        Ip = http.Connection.RemoteIpAddress?.ToString() ?? "",
        Agente = http.Request.Headers.UserAgent.ToString(),
    };

    // Vem vazio enquanto a consulta externa estiver desligada, que é o padrão.
    var geo = await provedores.Localizar(origem.Ip);

    var user = await users.FindByEmailAsync(email);

    if (user is null)
    {
        // Usuário inexistente e senha errada dão a MESMA resposta ao
        // navegador: distinguir os dois conta a quem tenta se aquele e-mail
        // existe. A diferença fica só no log.
        await logs.Registrar(new Erp.Model.Log.LogSistema
        {
            Evento = Erp.Model.Log.TipoEventoSistema.LoginFalho,
            Mensagem = "Tentativa de login com e-mail não cadastrado.",
            Identificacao = email,
            Ip = origem.Ip,
            UserAgent = origem.Agente,
            Provedor = geo.Provedor,
            Pais = geo.Pais,
            Cidade = geo.Cidade,
            Latitude = geo.Latitude,
            Longitude = geo.Longitude,
        });

        return Results.Redirect("/login?erro=1");
    }

    // Conta inativa não entra. Mesma resposta genérica ao navegador — dizer
    // "conta inativa" confirmaria que o e-mail existe; o motivo fica no log.
    if (user.Status != Erp.Model.Usuario.StatusUsuario.Ativo)
    {
        await logs.Registrar(new Erp.Model.Log.LogSistema
        {
            Evento = Erp.Model.Log.TipoEventoSistema.LoginFalho,
            Mensagem = "Tentativa de login em conta inativa.",
            UsuarioId = user.Id,
            UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
            Identificacao = email,
            Ip = origem.Ip,
            UserAgent = origem.Agente,
            Provedor = geo.Provedor,
            Pais = geo.Pais,
            Cidade = geo.Cidade,
            Latitude = geo.Latitude,
            Longitude = geo.Longitude,
        });

        return Results.Redirect("/login?erro=1");
    }

    // Tenant suspenso pelo dono da aplicação: ninguém da empresa entra. A
    // resposta ao navegador é a genérica; o motivo fica no log.
    if (user.EmpresaId is { } empresaId)
    {
        await using var contexto = conexoes.Central();
        if (await contexto.Empresas.AnyAsync(e => e.Id == empresaId && !e.Ativa))
        {
            await logs.Registrar(new Erp.Model.Log.LogSistema
            {
                Evento = Erp.Model.Log.TipoEventoSistema.LoginFalho,
                Mensagem = "Tentativa de login com a empresa (tenant) suspensa.",
                UsuarioId = user.Id,
                UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
                Identificacao = email,
                Ip = origem.Ip,
                UserAgent = origem.Agente,
                Provedor = geo.Provedor,
                Pais = geo.Pais,
                Cidade = geo.Cidade,
                Latitude = geo.Latitude,
                Longitude = geo.Longitude,
            });

            return Results.Redirect("/login?erro=1");
        }
    }

    // Dono da aplicação (ou quem tiver MFA ligado) não entra só com senha:
    // confere a senha, guarda num cookie temporário QUEM está entrando e manda
    // para o segundo fator. A sessão só é criada depois do código certo — e
    // quem ainda não cadastrou o autenticador é obrigado a cadastrar antes.
    var exigeMfa = user.TwoFactorEnabled
                || await users.IsInRoleAsync(user, Erp.Model.Acesso.Permissoes.PapelSuperAdmin);

    var result = exigeMfa
        ? await signIn.CheckPasswordSignInAsync(user, senha, lockoutOnFailure: true)
        : await signIn.PasswordSignInAsync(user, senha, isPersistent: lembrar ?? false, lockoutOnFailure: true);

    if (exigeMfa && result.Succeeded)
    {
        await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignInAsync(http,
            IdentityConstants.TwoFactorUserIdScheme,
            new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Id.ToString())],
                IdentityConstants.TwoFactorUserIdScheme)));

        return Results.Redirect(user.TwoFactorEnabled ? "/login/mfa" : "/login/mfa/ativar");
    }

    if (result.Succeeded)
    {
        await logs.Registrar(new Erp.Model.Log.LogSistema
        {
            Evento = Erp.Model.Log.TipoEventoSistema.Login,
            Mensagem = "Login efetuado.",
            UsuarioId = user.Id,
            UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
            Identificacao = email,
            Ip = origem.Ip,
            UserAgent = origem.Agente,
            Provedor = geo.Provedor,
            Pais = geo.Pais,
            Cidade = geo.Cidade,
            Latitude = geo.Latitude,
            Longitude = geo.Longitude,
        });

        return Results.Redirect("/home");
    }

    await logs.Registrar(new Erp.Model.Log.LogSistema
    {
        Evento = Erp.Model.Log.TipoEventoSistema.LoginFalho,
        Mensagem = !result.IsLockedOut
            ? "Senha incorreta."
            : user.LockoutEnd > DateTimeOffset.UtcNow.AddHours(1)
                ? "Tentativa de login com acesso restrito pela plataforma."
                : "Conta bloqueada por tentativas seguidas.",
        UsuarioId = user.Id,
        UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
        Identificacao = email,
        Ip = origem.Ip,
        UserAgent = origem.Agente,
        Provedor = geo.Provedor,
        Pais = geo.Pais,
        Cidade = geo.Cidade,
        Latitude = geo.Latitude,
        Longitude = geo.Longitude,
    });

    return Results.Redirect("/login?erro=1");
});

app.MapPost("/auth/logout", async (
    HttpContext http,
    SignInManager<Usuario> signIn,
    UserManager<Usuario> users,
    Erp.Repository.Log.LogSistemaRepository logs) =>
{
    // Lê quem é ANTES de encerrar a sessão: depois do SignOutAsync o principal
    // já não identifica ninguém.
    var user = await users.GetUserAsync(http.User);

    if (user is not null)
        await logs.Registrar(new Erp.Model.Log.LogSistema
        {
            Evento = Erp.Model.Log.TipoEventoSistema.Logout,
            Mensagem = "Sessão encerrada.",
            UsuarioId = user.Id,
            UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
            Identificacao = user.Email ?? "",
            Ip = http.Connection.RemoteIpAddress?.ToString() ?? "",
            UserAgent = http.Request.Headers.UserAgent.ToString(),
        });

    await signIn.SignOutAsync();

    return Results.Redirect("/login");
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
