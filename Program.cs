using BlazorBlueprint.Components;
using Erp.Components;
using Erp.Data;
using Erp.Localization;
using Erp.Model.Acesso;
using Erp.Model.Usuario;
using Erp.Service.Acesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Server;
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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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
    [FromForm] string email,
    [FromForm] string senha,
    [FromForm] bool? lembrar,
    UserManager<Usuario> users,
    SignInManager<Usuario> signIn) =>
{
    var user = await users.FindByEmailAsync(email);
    if (user is null)
        return Results.Redirect("/login?erro=1");

    var result = await signIn.PasswordSignInAsync(
        user, senha, isPersistent: lembrar ?? false, lockoutOnFailure: true);

    return result.Succeeded
        ? Results.Redirect("/home")
        : Results.Redirect("/login?erro=1");
});

app.MapPost("/auth/logout", async (SignInManager<Usuario> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.Redirect("/login");
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
