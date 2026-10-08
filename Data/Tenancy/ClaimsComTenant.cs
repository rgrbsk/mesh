using System.Security.Claims;
using Erp.Model.Acesso;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Data.Tenancy
{
    /// <summary>
    /// Grava no cookie a empresa e o banco do usuário. É por eles que cada
    /// requisição sabe em que banco consultar — sem ida ao catálogo a cada clique.
    /// </summary>
    public sealed class ClaimsComTenant : UserClaimsPrincipalFactory<Usuario, Papel>
    {
        private readonly ConexoesTenant _conexoes;

        public ClaimsComTenant(
            UserManager<Usuario> usuarios, RoleManager<Papel> papeis,
            IOptions<IdentityOptions> opcoes, ConexoesTenant conexoes)
            : base(usuarios, papeis, opcoes)
        {
            _conexoes = conexoes;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
        {
            var identidade = await base.GenerateClaimsAsync(usuario);

            if (usuario.EmpresaId is { } empresaId)
            {
                await using var central = _conexoes.Central();
                var banco = await central.Empresas
                    .Where(e => e.Id == empresaId)
                    .Select(e => e.Banco)
                    .FirstOrDefaultAsync();

                identidade.AddClaim(new Claim(TenantAtual.ClaimEmpresa, empresaId.ToString()));
                identidade.AddClaim(new Claim(TenantAtual.ClaimBanco, banco ?? ""));
            }

            return identidade;
        }
    }
}
