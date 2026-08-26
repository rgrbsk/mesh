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

        public DbSet<Erp.Model.CentroCusto.CentroCusto> CentrosCusto => Set<Erp.Model.CentroCusto.CentroCusto>();

        public DbSet<Erp.Model.Produto.Produto> Produtos => Set<Erp.Model.Produto.Produto>();


        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Cidade é referência: apagar uma cidade não pode levar junto os
            // cadastros que apontam pra ela (o default do EF aqui era Cascade).
            mb.Entity<Erp.Model.Pessoa.Pessoa>()
              .HasOne(p => p.Cidade)
              .WithMany()
              .HasForeignKey(p => p.CidadeId)
              .OnDelete(DeleteBehavior.Restrict);

            // Código é identificador de negócio: duplicar cria dois cadastros
            // pro mesmo item, caro de desfazer depois que há movimento.
            mb.Entity<Erp.Model.Produto.Produto>()
              .HasIndex(p => p.Codigo)
              .IsUnique();

            mb.Entity<Erp.Model.CentroCusto.CentroCusto>()
              .HasIndex(c => c.Codigo)
              .IsUnique();

            // Auto-relacionamento da árvore. Restrict de propósito: apagar um
            // centro que tem filhos tem que falhar, não levar a subárvore junto.
            mb.Entity<Erp.Model.CentroCusto.CentroCusto>()
              .HasOne(c => c.Pai)
              .WithMany(c => c.Filhos)
              .HasForeignKey(c => c.PaiId)
              .OnDelete(DeleteBehavior.Restrict);

            // Propriedades calculadas não viram coluna.
            mb.Entity<Erp.Model.Produto.Produto>().Ignore(p => p.PontoPedidoSugerido);
            mb.Entity<Erp.Model.Produto.Produto>().Ignore(p => p.PrecisaRepor);

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
