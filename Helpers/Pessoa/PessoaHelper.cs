using BlazorBlueprint.Primitives;
using Erp.Model.Pessoa;

namespace Erp.Helpers.Pessoa
{
    public class PessoaHelper
    {
        public static List<SelectOption<string>> ListarTipos()
        {
            return Enum.GetValues<TipoPessoa>()
            .Select(x => new SelectOption<string>(
                x.ToString(), // Value (como string)
                x.ToString()  // Text (como string)
            ))
            .ToList();
        }
    }
}
