using Erp.Model.Cidades;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text;

namespace Erp.Data
{
    /// <summary>
    /// Carrega os municípios do IBGE a partir do CSV embutido no assembly
    /// (Data/cidades-ibge.csv, no formato "codigoIbge;nome;UF").
    ///
    /// Fica separado do DbSeeder porque é dado de referência, não de exemplo:
    /// são 5.571 linhas que entram uma vez e não se mexe mais. A carga é
    /// incremental — roda de novo depois de uma atualização da lista e insere
    /// só o que falta, comparando pelo código do IBGE, que é a identidade real
    /// do município (o nome muda, o código não).
    /// </summary>
    public static class CidadeSeeder
    {
        private const int CodigoPaisBrasil = 1058;
        private const string NomePais = "Brasil";
        private const string Recurso = "Erp.Data.cidades-ibge.csv";

        public static async Task SeedAsync(AppDbContext db)
        {
            var doArquivo = Ler();

            if (doArquivo.Count == 0)
                return;

            var jaGravados = await db.Cidades
                .Select(c => c.CodigoIbge)
                .ToListAsync();

            var existentes = jaGravados.ToHashSet();

            var novas = doArquivo
                .Where(c => !existentes.Contains(c.CodigoIbge))
                .ToList();

            if (novas.Count == 0)
                return;

            db.Cidades.AddRange(novas);
            await db.SaveChangesAsync();
        }

        private static List<Cidade> Ler()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(Recurso);

            if (stream is null)
                return [];

            using var leitor = new StreamReader(stream, Encoding.UTF8);

            var cidades = new List<Cidade>();

            while (leitor.ReadLine() is { } linha)
            {
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                var partes = linha.Split(';');

                // Linha malformada é pulada em silêncio: um município a menos
                // não justifica derrubar a subida da aplicação.
                if (partes.Length < 3 || !int.TryParse(partes[0], out var codigo))
                    continue;

                cidades.Add(new Cidade
                {
                    CodigoIbge = codigo,
                    Descricao = partes[1].Trim(),
                    EstadoString = partes[2].Trim(),
                    CodigoPais = CodigoPaisBrasil,
                    PaisString = NomePais,
                });
            }

            return cidades;
        }
    }
}
