using System.Data.Common;
using Erp.Service.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erp.Data.Interceptor
{
    /// <summary>
    /// Reforça o RLS do Postgres: seta <c>app.current_tenant</c> a cada conexão
    /// aberta. Re-setado sempre porque o pool reusa conexões físicas.
    /// </summary>
    public class TenantConnectionInterceptor : DbConnectionInterceptor
    {
        private readonly ITenantProvider _tenant;

        public TenantConnectionInterceptor(ITenantProvider tenant) => _tenant = tenant;

        public override async Task ConnectionOpenedAsync(
            DbConnection connection, ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (!_tenant.TryGetTenantId(out var tenantId))
                return; // sem tenant (ex.: migrations/DDL) — nada a setar

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.current_tenant', @t, false)";
            var p = cmd.CreateParameter();
            p.ParameterName = "t";
            p.Value = tenantId.ToString();
            cmd.Parameters.Add(p);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (!_tenant.TryGetTenantId(out var tenantId))
                return;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.current_tenant', @t, false)";
            var p = cmd.CreateParameter();
            p.ParameterName = "t";
            p.Value = tenantId.ToString();
            cmd.Parameters.Add(p);
            cmd.ExecuteNonQuery();
        }
    }
}
