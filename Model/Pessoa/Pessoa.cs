using Erp.Model.Cidades;
namespace Erp.Model.Pessoa
{
    public class Pessoa
    {
        public int Id { get; set; }

        public TipoPessoa Tipo { get; set; } = TipoPessoa.Cliente;

        public string CNPJ { get; set; } = string.Empty;

        public string RazaoSocial { get; set; } = string.Empty;

        public string? NomeFantasia { get; set; }

        public string? Natureza { get; set; } = "Pessoa Física";

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        public long? IE { get; set; }

        public int CidadeId { get; set; }

        public Cidade? Cidade { get; set; }

        public string? Logradouro { get; set; }

        public int NumeroEndereco { get; set; }

        public string? Bairro { get; set; }

        public int CEP { get; set; }

        public string? Email { get; set; }

        public string? Parecer { get; set; }

        
    }
}
