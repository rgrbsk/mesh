using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Erp.Service.Acesso
{
    public sealed class PermissaoPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
    {
        public const string Prefixo = "perm:";

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!policyName.StartsWith(Prefixo, StringComparison.OrdinalIgnoreCase))
                return await base.GetPolicyAsync(policyName);

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissaoRequirement(policyName[Prefixo.Length..]))
                .Build();
        }
    }

    public sealed class TemPermissaoAttribute(string permissao)
        : Microsoft.AspNetCore.Authorization.AuthorizeAttribute(PermissaoPolicyProvider.Prefixo + permissao);
}
