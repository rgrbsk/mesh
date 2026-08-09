using Microsoft.AspNetCore.Http;

namespace Erp.Service.Tenancy
{
    /// <summary>
    /// Resolve o tenant a partir do claim <c>tenant_id</c> do usuário autenticado.
    ///
    /// SEAM: hoje lê do <see cref="IHttpContextAccessor"/>, o que cobre o request
    /// HTTP inicial e chamadas de API. Em Blazor Server, o circuito SignalR vive
    /// depois do HttpContext — quando a autenticação estiver plugada, troque a
    /// resolução para ler do AuthenticationState no início do circuito.
    /// </summary>
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _http;

        public TenantProvider(IHttpContextAccessor http) => _http = http;

        public Guid TenantId => TryGetTenantId(out var id)
            ? id
            : throw new InvalidOperationException(
                "Tenant não resolvido: claim 'tenant_id' ausente no usuário autenticado.");

        public bool TryGetTenantId(out Guid tenantId)
        {
            var claim = _http.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out tenantId);
        }
    }
}
