using Erp.Model.Acesso;
using Erp.Model.Usuario;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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


        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
        }
    }
}
