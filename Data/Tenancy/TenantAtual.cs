using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Erp.Data.Tenancy
{
    /// <summary>
    /// Qual banco esta requisição (ou circuito) usa. Vem do claim "tenant" que
    /// o login grava no cookie; a página pública da cotação, que não tem login,
    /// informa o tenant pelo próprio link.
    /// </summary>
    public sealed class TenantAtual
    {
        public const string ClaimBanco = "tenant";
        public const string ClaimEmpresa = "empresa";

        private readonly IHttpContextAccessor _http;
        private readonly IServiceProvider _servicos;
        private string? _definido;
        private bool _foiDefinido;

        public TenantAtual(IHttpContextAccessor http, IServiceProvider servicos)
        {
            _http = http;
            _servicos = servicos;
        }

        /// <summary>Fixa o tenant desta escopo (link público da cotação).</summary>
        public void Definir(string? banco)
        {
            _definido = banco;
            _foiDefinido = true;
        }

        public async Task<string?> Banco() =>
            _foiDefinido ? _definido : (await Usuario())?.FindFirstValue(ClaimBanco);

        public async Task<Guid?> EmpresaId() =>
            Guid.TryParse((await Usuario())?.FindFirstValue(ClaimEmpresa), out var id) ? id : null;

        private async Task<ClaimsPrincipal?> Usuario()
        {
            // Dentro do circuito Blazor não há HttpContext: quem sabe o usuário
            // é o AuthenticationStateProvider. Numa requisição HTTP comum (login,
            // endpoints) ele ainda não foi preenchido e lança — aí vale o HttpContext.
            try
            {
                var provedor = _servicos.GetService<AuthenticationStateProvider>();
                if (provedor is not null)
                {
                    var estado = await provedor.GetAuthenticationStateAsync();
                    if (estado.User.Identity?.IsAuthenticated == true)
                        return estado.User;
                }
            }
            catch (InvalidOperationException)
            {
            }

            var http = _http.HttpContext?.User;
            return http?.Identity?.IsAuthenticated == true ? http : null;
        }
    }
}
