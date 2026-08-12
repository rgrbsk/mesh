using Erp.Model.Acesso;
// Service/Acesso/PermissaoRequirement.cs
using Microsoft.AspNetCore.Authorization;
namespace Erp.Service.Acesso
{


    public sealed class PermissaoRequirement(string permissao) : IAuthorizationRequirement
    {
        public string Permissao { get; } = permissao;
    }

    public sealed class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissaoRequirement requirement)
        {
            if (context.User.HasClaim(Permissoes.ClaimType, requirement.Permissao))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
