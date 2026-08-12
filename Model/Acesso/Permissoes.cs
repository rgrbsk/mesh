// Model/Acesso/Permissoes.cs
namespace Erp.Model.Acesso
{
    public static class Permissoes
    {
        public const string ClaimType = "perm";

        public const string ComprasVer = "compras.ver";
        public const string ComprasCriar = "compras.criar";
        public const string ComprasAprovar = "compras.aprovar";
        public const string EstoqueVer = "estoque.ver";
        public const string EstoqueAjustar = "estoque.ajustar";
        public const string FornecedorVer = "fornecedor.ver";
        public const string FornecedorEditar = "fornecedor.editar";
        public const string UsuariosGerir = "usuarios.gerir";

        public static readonly string[] Todas =
        [
            ComprasVer, ComprasCriar, ComprasAprovar,
            EstoqueVer, EstoqueAjustar,
            FornecedorVer, FornecedorEditar,
            UsuariosGerir,
        ];
    }
}