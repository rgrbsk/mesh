using Erp.Model.Cidades;
namespace Erp.Model.Pessoa
{
    public class Pessoa
    {
        public int Id { get; set; }

        public TipoPessoa Tipo { get; set; } = TipoPessoa.Cliente;

        /// <summary>CPF ou CNPJ, conforme a Natureza. Guardado como texto: o
        /// documento tem zeros à esquerda e formatação.</summary>
        public string CNPJ { get; set; } = string.Empty;

        public string RazaoSocial { get; set; } = string.Empty;

        public string? NomeFantasia { get; set; }

        public string? Natureza { get; set; } = "Pessoa Física";

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ModificadoEm { get; set; } = DateTime.UtcNow;

        /// <summary>Texto, não número: a IE tem zeros à esquerda, letras em
        /// alguns estados e aceita o valor "ISENTO".</summary>
        public string? IE { get; set; }

        public string? InscricaoMunicipal { get; set; }

        public string? RegimeTributario { get; set; }

        /// <summary>Opcional: o cadastro precisa poder ser salvo antes de a
        /// cidade estar escolhida (ou cadastrada).</summary>
        public int? CidadeId { get; set; }

        public Cidade? Cidade { get; set; }

        public string? Logradouro { get; set; }

        /// <summary>Texto por causa de "S/N" e de números como "123-A".</summary>
        public string? NumeroEndereco { get; set; }

        public string? Bairro { get; set; }

        /// <summary>Texto: como número, o CEP 01310-100 viraria 1310100.</summary>
        public string? CEP { get; set; }

        public string? Email { get; set; }

        public string? Parecer { get; set; }
    }
}
