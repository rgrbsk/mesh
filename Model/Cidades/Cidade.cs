namespace Erp.Model.Cidades
{
    public class Cidade
    {
        public int Id { get; set; }

        public string Descricao { get; set; } = string.Empty;

        public string EstadoString { get; set; } = string.Empty;

        public int CodigoPais { get; set; }

        public string? PaisString { get; set; }

        public int CodigoIbge { get; set; }
    }
}
