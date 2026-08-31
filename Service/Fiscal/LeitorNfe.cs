using Erp.Model.Fiscal;
using System.Globalization;
using System.Xml.Linq;

namespace Erp.Service.Fiscal
{
    /// <summary>
    /// Lê o XML de uma NF-e e devolve o que interessa para o confronto.
    ///
    /// Não valida assinatura nem consulta a SEFAZ: isso é conferência fiscal, e
    /// exige certificado e serviço externo. Aqui o objetivo é ler o que o
    /// fornecedor mandou para comparar com o que foi comprado.
    /// </summary>
    public static class LeitorNfe
    {
        /// <summary>Namespace do portal fiscal. Todo elemento da NF-e vive nele,
        /// e procurar sem o namespace não acha nada.</summary>
        private static readonly XNamespace Nfe = "http://www.portalfiscal.inf.br/nfe";

        public static NotaFiscal Ler(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new FormatException("Arquivo vazio.");

            XDocument documento;

            try
            {
                documento = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Arquivo não é um XML válido: {ex.Message}");
            }

            // O arquivo pode vir de dois jeitos: <nfeProc> (nota + protocolo de
            // autorização) ou <NFe> sozinha. Procurar infNFe em qualquer
            // profundidade cobre os dois sem precisar distinguir.
            var infNFe = documento.Descendants(Nfe + "infNFe").FirstOrDefault()
                ?? throw new FormatException(
                    "Não parece uma NF-e: elemento infNFe não encontrado.");

            var ide = infNFe.Element(Nfe + "ide");
            var emit = infNFe.Element(Nfe + "emit");
            var dest = infNFe.Element(Nfe + "dest");
            var total = infNFe.Element(Nfe + "total")?.Element(Nfe + "ICMSTot");

            var nota = new NotaFiscal
            {
                // O Id vem como "NFe" + 44 dígitos; a chave é só a parte numérica.
                Chave = (infNFe.Attribute("Id")?.Value ?? "").Replace("NFe", "").Trim(),
                Numero = Texto(ide, "nNF"),
                Serie = Texto(ide, "serie"),
                Emissao = Data(ide),
                EmitenteCnpj = SoDigitos(Texto(emit, "CNPJ")),
                EmitenteNome = Texto(emit, "xNome"),
                DestinatarioCnpj = SoDigitos(Texto(dest, "CNPJ")),
                ValorProdutos = Numero(total, "vProd"),
                ValorFrete = Numero(total, "vFrete"),
                ValorTotal = Numero(total, "vNF"),
                Ambiente = (int)Numero(ide, "tpAmb"),
                Protocolo = documento.Descendants(Nfe + "infProt")
                                     .FirstOrDefault()?.Element(Nfe + "nProt")?.Value ?? "",
                Xml = xml,
            };

            if (string.IsNullOrWhiteSpace(nota.Chave))
                throw new FormatException("NF-e sem chave de acesso.");

            foreach (var det in infNFe.Elements(Nfe + "det"))
            {
                var prod = det.Element(Nfe + "prod");

                if (prod is null)
                    continue;

                nota.Itens.Add(new NotaFiscalItem
                {
                    Numero = int.TryParse(det.Attribute("nItem")?.Value, out var n) ? n : 0,
                    CodigoFornecedor = Texto(prod, "cProd"),
                    Descricao = Texto(prod, "xProd"),
                    Ncm = Texto(prod, "NCM"),
                    Cfop = Texto(prod, "CFOP"),
                    Unidade = Texto(prod, "uCom"),
                    Quantidade = Numero(prod, "qCom"),
                    ValorUnitario = Numero(prod, "vUnCom"),
                    ValorTotal = Numero(prod, "vProd"),
                });
            }

            if (nota.Itens.Count == 0)
                throw new FormatException("NF-e sem itens.");

            return nota;
        }

        private static string Texto(XElement? pai, string nome) =>
            pai?.Element(Nfe + nome)?.Value?.Trim() ?? "";

        /// <summary>
        /// O XML da NF-e usa SEMPRE ponto decimal, independente do idioma da
        /// máquina. Ler com a cultura corrente transformaria "138.3000" em
        /// 1.383.000 numa máquina pt-BR — o erro clássico, e silencioso.
        /// </summary>
        private static decimal Numero(XElement? pai, string nome) =>
            decimal.TryParse(Texto(pai, nome), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor)
                ? valor
                : 0m;

        /// <summary>
        /// Data de emissão. A 3.10 usa dhEmi com fuso; versões antigas usam dEmi
        /// só com a data. Convertido para UTC porque é assim que tudo é gravado
        /// no banco.
        /// </summary>
        private static DateTime Data(XElement? ide)
        {
            var texto = Texto(ide, "dhEmi");

            if (string.IsNullOrWhiteSpace(texto))
                texto = Texto(ide, "dEmi");

            return DateTimeOffset.TryParse(
                       texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
                ? data.UtcDateTime
                : DateTime.UtcNow;
        }

        /// <summary>CNPJ vem sem máscara na NF-e, mas o cadastro pode ter sido
        /// digitado com pontos — comparar exige normalizar os dois lados.</summary>
        public static string SoDigitos(string? valor) =>
            new(( valor ?? "").Where(char.IsDigit).ToArray());
    }
}
