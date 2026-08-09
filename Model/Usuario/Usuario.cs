namespace Erp.Model.Usuario
{
    /// <summary>
    /// Identidade GLOBAL — NÃO é tenant-scoped. O email é único no sistema todo e
    /// o mesmo usuário pode pertencer a várias Empresas via <see cref="UsuarioEmpresa"/>.
    /// O tenant ativo é escolhido no login e vai no claim <c>tenant_id</c>.
    /// </summary>
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Nome { get; set; } = string.Empty;

        public string Sobrenome { get; set; } = string.Empty;

        public string CPF { get; set; } = string.Empty;

        public string Cargo { get; set; } = "Cargo padrão";
        public string Email { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public DateTime DataModificacao { get; set; } = DateTime.Now;

        public string NumeroContato { get; set; } = string.Empty;

        public string Observacao { get; set; } = string.Empty;

        /// <summary>Hash da senha (PasswordHasher) — a configurar depois.</summary>
        public string? SenhaHash { get; set; }

        /// <summary>Empresas às quais este usuário tem acesso.</summary>
        public ICollection<UsuarioEmpresa> Empresas { get; set; } = new List<UsuarioEmpresa>();
    }
}
