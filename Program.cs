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

// Single database: uma connection string, um DbContext.
// Fábrica, não DbContext scoped: no Blazor Server o escopo dura o circuito
// INTEIRO, então um DbContext compartilhado é usado por componentes que renderizam
// em paralelo — dois awaits simultâneos nele estouram "A second operation was
// started on this context instance". Cada operação de repositório abre e fecha
// o seu contexto.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// O Identity (UserManager/SignInManager/stores) exige um AppDbContext scoped.
// Ele sai da mesma fábrica, então continua havendo UMA configuração só — e o uso
// do Identity é sequencial dentro da requisição, onde scoped não é problema.
builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddIdentity<Usuario, Papel>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequiredLength = 8;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

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
    Erp.Service.Log.ProvedorIpService provedores) =>
{
    // Origem da tentativa. Registrada mesmo quando o login falha — é
    // exatamente aí que interessa saber de onde veio.
    var origem = new
    {
        Ip = http.Connection.RemoteIpAddress?.ToString() ?? "",
        Agente = http.Request.Headers.UserAgent.ToString(),
    };

    // Vem vazio enquanto a consulta externa estiver desligada, que é o padrão.
    var provedor = await provedores.Consultar(origem.Ip);

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
            Provedor = provedor,
        });

        return Results.Redirect("/login?erro=1");
    }

    var result = await signIn.PasswordSignInAsync(
        user, senha, isPersistent: lembrar ?? false, lockoutOnFailure: true);

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
            Provedor = provedor,
        });

        return Results.Redirect("/home");
    }

    await logs.Registrar(new Erp.Model.Log.LogSistema
    {
        Evento = Erp.Model.Log.TipoEventoSistema.LoginFalho,
        Mensagem = result.IsLockedOut
            ? "Conta bloqueada por tentativas seguidas."
            : "Senha incorreta.",
        UsuarioId = user.Id,
        UsuarioNome = $"{user.Nome} {user.Sobrenome}".Trim(),
        Identificacao = email,
        Ip = origem.Ip,
        UserAgent = origem.Agente,
        Provedor = provedor,
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
