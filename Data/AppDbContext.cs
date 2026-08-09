using System.Reflection;
using Erp.Model;
using Erp.Service.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Erp.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantProvider _tenant;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenant)
            : base(options)
            => _tenant = tenant;

        // Tabela do tenant (NÃO é tenant-scoped — ela É o tenant).
        public DbSet<Erp.Model.Empresa.Empresa> Empresas => Set<Erp.Model.Empresa.Empresa>();

        // Identidades e vínculos são GLOBAIS (não filtrados por tenant): o login
        // acontece antes de existir um tenant e um usuário pode estar em várias empresas.
        public DbSet<Erp.Model.Usuario.Usuario> Usuarios => Set<Erp.Model.Usuario.Usuario>();
        public DbSet<Erp.Model.Usuario.UsuarioEmpresa> Vinculos => Set<Erp.Model.Usuario.UsuarioEmpresa>();

        private static readonly MethodInfo SetTenantFilterMethod =
            typeof(AppDbContext).GetMethod(nameof(SetTenantFilter),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Email é único no sistema todo (identidade global).
            mb.Entity<Erp.Model.Usuario.Usuario>()
                .HasIndex(u => u.Email).IsUnique();

            // Um usuário não se vincula duas vezes à mesma empresa.
            mb.Entity<Erp.Model.Usuario.UsuarioEmpresa>()
                .HasIndex(v => new { v.UsuarioId, v.EmpresaId }).IsUnique();

            foreach (var et in mb.Model.GetEntityTypes()
                         .Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType)))
            {
                // filtro global: só linhas do tenant atual (aplicado a TODA ITenantEntity)
                SetTenantFilterMethod.MakeGenericMethod(et.ClrType).Invoke(this, new object[] { mb });

                // índice tenant-first — essencial pra performance
                mb.Entity(et.ClrType).HasIndex(nameof(ITenantEntity.TenantId));
            }
        }

        // Escrito genericamente de propósito: assim o EF reconhece a captura do
        // contexto e reavalia _tenant.TenantId POR QUERY, em vez de "congelar" o
        // primeiro valor no cache do modelo (o bug clássico do filtro dinâmico).
        private void SetTenantFilter<TEntity>(ModelBuilder mb)
            where TEntity : class, ITenantEntity
            => mb.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenant.TenantId);

        // Preenche TenantId sozinho no INSERT — ninguém esquece.
        public override int SaveChanges()
        {
            StampTenant();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            StampTenant();
            return base.SaveChangesAsync(ct);
        }

        private void StampTenant()
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
                         .Where(e => e.State == EntityState.Added))
                entry.Entity.TenantId = _tenant.TenantId;
        }
    }
}
