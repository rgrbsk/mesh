using Erp.Model.Usuario;
using Microsoft.AspNetCore.Identity;

namespace Erp.Model.Usuario
{
    public class Usuario : IdentityUser<Guid>
    {
        public Usuario() => Id = Guid.CreateVersion7();
        public string Nome { get; set; } = string.Empty;

        public string Sobrenome { get; set; } = string.Empty;

        public StatusUsuario Status { get; set; } = StatusUsuario.Ativo;
        public string CPF { get; set; } = string.Empty;

        public string Cargo { get; set; } = "Cargo padrão";

        public DateTime? DataCadastro { get; set; } = DateTime.UtcNow;

        public DateTime? DataModificacao { get; set; } = DateTime.UtcNow;

        public string NumeroContato { get; set; } = string.Empty;

        public string Observacao { get; set; } = string.Empty;

        public List<string>? Tag { get; set; }

        public string? TemaPreferencial { get; set; } = "light";
    }
}
