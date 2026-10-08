using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Erp.Data.Tenancy
{
    /// <summary>
    /// Um banco por tenant. O banco CENTRAL (a connection string "Default") é o
    /// catálogo: usuários, papéis, tenants e logs de sistema. Cada tenant tem o
    /// seu banco com os dados de negócio — o mesmo modelo, outra conexão. O
    /// tenant que já existia antes desta separação continua no banco central.
    /// </summary>
    public sealed class ConexoesTenant
    {
        private readonly string _central;
        private readonly ConcurrentDictionary<string, DbContextOptions<AppDbContext>> _opcoes = new();

        public ConexoesTenant(IConfiguration configuracao)
        {
            _central = configuracao.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
            BancoCentral = new NpgsqlConnectionStringBuilder(_central).Database ?? "";
        }

        public string BancoCentral { get; }

        /// <summary>Vazio ou o próprio nome do banco central = banco central.</summary>
        public bool EhCentral(string? banco) =>
            string.IsNullOrWhiteSpace(banco) || banco == BancoCentral;

        public string Normalizar(string? banco) => EhCentral(banco) ? BancoCentral : banco!;

        public string ConnectionString(string? banco) =>
            EhCentral(banco)
                ? _central
                : new NpgsqlConnectionStringBuilder(_central) { Database = banco }.ConnectionString;

        /// <summary>Conexão com o banco do servidor (postgres), para CREATE/DROP DATABASE.</summary>
        public string ConnectionStringServidor() =>
            new NpgsqlConnectionStringBuilder(_central) { Database = "postgres", Pooling = false }.ConnectionString;

        public AppDbContext Criar(string? banco) =>
            new(_opcoes.GetOrAdd(Normalizar(banco), b =>
                new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString(b)).Options));

        public AppDbContext Central() => Criar(null);
    }
}
