using System.Reflection;

namespace Erp.Service
{
    /// <summary>
    /// Registro por convenção: toda classe do projeto cujo nome termina em
    /// "Repository" ou "Service" entra no DI como scoped, sem precisar de uma
    /// linha por tipo no Program.cs.
    ///
    /// Scoped é o tempo de vida certo aqui porque essas classes dependem do
    /// AppDbContext, que é scoped — um singleton segurando um DbContext vaza
    /// entidades rastreadas entre usuários.
    /// </summary>
    public static class RegistroServicos
    {
        private static readonly string[] Sufixos = ["Repository", "Service"];

        public static IServiceCollection AddRepositoriosEServicos(this IServiceCollection services)
        {
            var tipos = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => Sufixos.Any(s => t.Name.EndsWith(s, StringComparison.Ordinal)));

            foreach (var tipo in tipos)
            {
                // Como ele mesmo: permite @inject EmpresaRepository direto.
                services.AddScoped(tipo);

                // E como a interface homônima (IEmpresaRepository), se existir.
                var contrato = tipo.GetInterfaces()
                    .FirstOrDefault(i => i.Name == "I" + tipo.Name);

                if (contrato is not null)
                    services.AddScoped(contrato, sp => sp.GetRequiredService(tipo));
            }

            return services;
        }
    }
}
