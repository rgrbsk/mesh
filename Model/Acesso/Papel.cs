using Microsoft.AspNetCore.Identity;
namespace Erp.Model.Acesso
{
    public class Papel : IdentityRole<Guid>
    {
        public Papel() => Id = Guid.CreateVersion7();

        public Papel(string nome) : this() => Name = nome;

        public string Descricao { get; set; } = string.Empty;
    }
}

