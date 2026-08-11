using System.Security.Claims;
using Erp.Components;
using Erp.Data;
using Erp.Localization;
using Erp.Data.Interceptor;
using Erp.Service.Tenancy;
using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Usuario = Erp.Model.Usuario.Usuario;

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

// Multitenancy: provider do tenant (scoped ao request/circuito), interceptor de
// RLS e o DbContext apontando pro Postgres.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<TenantConnectionInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
});

// Autenticação: cookie custom sobre a própria tabela Usuario (sem ASP.NET Identity).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();          // expõe AuthenticationState ao Blazor
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

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
// port makes it cross-origin and breaks the interactive circuit (CORS).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ───────────────────────── Autenticação (endpoints HTTP) ─────────────────────────
// SignInAsync escreve o cookie na resposta HTTP — por isso o login vive aqui, num
// POST real, e NÃO dentro do circuito interativo (que não tem resposta pra escrever).

// 1) Login: valida credenciais e emite o cookie SEM tenant (empresa é escolhida depois).
app.MapPost("/auth/login", async (
    HttpContext http,
    [FromForm] string email,
    [FromForm] string senha,
    AppDbContext db,
    IPasswordHasher<Usuario> hasher) =>
{
    var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

    if (user is null || string.IsNullOrEmpty(user.SenhaHash) ||
        hasher.VerifyHashedPassword(user, user.SenhaHash, senha) == PasswordVerificationResult.Failed)
    {
        return Results.Redirect("/login?erro=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Nome),
        new(ClaimTypes.Email, user.Email),
        // sem "tenant_id" ainda — definido na seleção de empresa
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect("/login/empresas");
}).DisableAntiforgery();

// 2) Seleção de empresa (a tela /login/empresas será feita depois; este é o endpoint
//    que ela deve POSTar). Verifica o vínculo e re-emite o cookie com o tenant_id.
app.MapPost("/auth/empresa", async (
    HttpContext http,
    [FromForm] Guid empresaId,
    AppDbContext db) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdStr, out var userId))
        return Results.Redirect("/login");

    var vinculado = await db.Vinculos
        .AnyAsync(v => v.UsuarioId == userId && v.EmpresaId == empresaId);
    if (!vinculado)
        return Results.Redirect("/login/empresas?erro=1");

    // Troca de empresa = re-login: mantém os claims, troca só o tenant_id.
    var claims = http.User.Claims.Where(c => c.Type != "tenant_id").ToList();
    claims.Add(new Claim("tenant_id", empresaId.ToString()));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect("/home");
}).RequireAuthorization().DisableAntiforgery();

// 3) Logout.
app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();
