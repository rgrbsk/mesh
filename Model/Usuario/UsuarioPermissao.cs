using Erp.Model.Empresa;
namespace Erp.Model.Usuario
{
    public class UsuarioPermissao
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public Guid EmpresaId { get; set; }

        //public Empresa? Empresa { get; set; }


        
    }
}
