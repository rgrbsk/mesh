using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Service.Acesso
{
    /// <summary>
    /// Revalida o usuário do circuito a cada intervalo comparando o SecurityStamp
    /// do cookie com o do banco.
    ///
    /// Sem isso, o circuito Blazor (que fica aberto por horas) continua com os
    /// claims do momento do login: trocar o papel do usuário, revogar uma permissão
    /// ou desativar a conta só teria efeito no próximo login. O SecurityStamp muda
    /// a cada alteração relevante no usuário, então basta compará-lo pra derrubar
    /// a sessão viva.
    /// </summary>
    public sealed class IdentityRevalidatingAuthenticationStateProvider
        : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdentityOptions _options;

        public IdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> options)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // O UserManager é scoped e este provider vive pelo circuito inteiro —
            // por isso um escopo próprio a cada revalidação.
            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

            return await ValidateSecurityStampAsync(userManager, authenticationState.User);
        }

        private async Task<bool> ValidateSecurityStampAsync(
            UserManager<Usuario> userManager, ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
                return false;

            if (!userManager.SupportsUserSecurityStamp)
                return true;

            var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
            var userStamp = await userManager.GetSecurityStampAsync(user);

            return principalStamp == userStamp;
        }
    }
}
