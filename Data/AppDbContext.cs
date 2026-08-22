using Erp.Model.Acesso;
using Erp.Model.Usuario;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Erp.Data
{
    /// <summary>
    /// Contexto único da aplicação (single database — uma connection string,
    /// sem particionamento por tenant).
    /// </summary>
    public class AppDbContext : IdentityDbContext<Usuario, Papel, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Erp.Model.Empresa.Empresa> Empresas => Set<Erp.Model.Empresa.Empresa>();

        public DbSet<Erp.Model.Usuario.Usuario> Usuarios => Set<Erp.Model.Usuario.Usuario>();

        public DbSet<Erp.Model.Acesso.Papel> Papeis => Set<Erp.Model.Acesso.Papel>();

        public DbSet<Erp.Model.Cidades.Cidade> Cidades => Set<Erp.Model.Cidades.Cidade>();

        public DbSet<Erp.Model.Pessoa.Pessoa> Pessoas => Set<Erp.Model.Pessoa.Pessoa>();


        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            // Automatically treats Unspecified DateTimes as UTC when saving or reading
            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                            v => v
                        ));
                    }
                }
            }

        }
    }
}
