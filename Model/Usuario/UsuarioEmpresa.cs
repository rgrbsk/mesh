namespace Erp.Model.Usuario
{
    /// <summary>
    /// Vínculo N:N entre <see cref="Usuario"/> e <see cref="Empresa.Empresa"/>.
    ///
    /// GLOBAL — de propósito NÃO implementa ITenantEntity: no login (antes de existir
    /// um tenant) precisamos listar TODAS as empresas do usuário para ele escolher.
    /// Consulte-a sempre explicitamente por UsuarioId / EmpresaId.
    /// </summary>
    public class UsuarioEmpresa
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public Guid UsuarioId { get; set; }
        public Guid EmpresaId { get; set; }

        // public string Papel { get; set; }  // role por empresa — configurar depois

        public Usuario? Usuario { get; set; }
        public Erp.Model.Empresa.Empresa? Empresa { get; set; }
    }
}
