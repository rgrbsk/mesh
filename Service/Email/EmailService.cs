using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Erp.Service.Email
{
    /// <summary>
    /// Configuração do servidor de saída. As credenciais NÃO ficam no
    /// appsettings.json versionado: coloque-as em
    /// appsettings.Development.local.json, que é git-ignored e sobrepõe este.
    /// </summary>
    public sealed class EmailOptions
    {
        public const string Secao = "Email";

        public string Host { get; set; } = string.Empty;

        public int Porta { get; set; } = 587;

        public bool UsarSsl { get; set; } = true;

        public string Usuario { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        /// <summary>De quem o fornecedor vê que veio.</summary>
        public string Remetente { get; set; } = string.Empty;

        public string NomeRemetente { get; set; } = "mesh";

        /// <summary>Sem host não há como enviar — a tela cai no copiar link.</summary>
        public bool Configurado => !string.IsNullOrWhiteSpace(Host)
                                && !string.IsNullOrWhiteSpace(Remetente);
    }

    /// <summary>
    /// Envio de e-mail. Uma responsabilidade só: o texto do convite mora aqui
    /// porque é conteúdo, não regra — quem chama passa o link pronto.
    ///
    /// Nunca lança para o chamador: e-mail é entrega best-effort, e uma cotação
    /// não pode deixar de ser aberta porque o SMTP recusou. Quem chama recebe
    /// false e mostra o link para envio manual.
    /// </summary>
    public sealed class EmailService
    {
        private readonly EmailOptions _opcoes;
        private readonly ILogger<EmailService> _log;

        public EmailService(IOptions<EmailOptions> opcoes, ILogger<EmailService> log)
        {
            _opcoes = opcoes.Value;
            _log = log;
        }

        public bool Configurado => _opcoes.Configurado;

        /// <summary>
        /// Manda o link da cotação para um fornecedor. O corpo é curto de
        /// propósito: quanto mais parecido com propaganda, mais chance de cair
        /// em spam — e o que importa é o link.
        /// </summary>
        public Task<bool> EnviarConviteCotacao(
            string destinatario,
            int numeroCotacao,
            string tituloCotacao,
            string link,
            DateTime prazo,
            Comprador comprador)
        {
            // O número no assunto é o que o fornecedor cita ao responder por
            // telefone ou e-mail — e o que ele procura na caixa depois.
            var assunto = $"Cotação #{numeroCotacao} — {tituloCotacao}";

            var corpo =
                $"""
                 <p>Olá,</p>
                 <p>{Apresentacao(comprador)} está cotando <strong>{WebUtility.HtmlEncode(tituloCotacao)}</strong>
                 e convidou sua empresa a enviar uma proposta.</p>
                 <p><a href="{link}">Abrir o formulário da cotação #{numeroCotacao}</a></p>
                 <p>O link é pessoal e vale até <strong>{prazo:dd/MM/yyyy}</strong>.
                 Não é preciso criar conta nem senha.</p>
                 {Assinatura(comprador, numeroCotacao)}
                 """;

            return Enviar(destinatario, assunto, corpo);
        }

        /// <summary>
        /// Quem está comprando. Vai em todo e-mail: fornecedor recebe cotação de
        /// muita gente, e mensagem sem remetente identificado ou é ignorada ou
        /// vira suspeita de golpe — ainda mais uma que pede para clicar num link.
        /// </summary>
        public sealed record Comprador(string Empresa, string Cnpj, string Contato, string EmailContato);

        private static string Apresentacao(Comprador comprador) =>
            string.IsNullOrWhiteSpace(comprador.Empresa)
                ? "Uma empresa"
                : $"<strong>{WebUtility.HtmlEncode(comprador.Empresa)}</strong>";

        /// <summary>Rodapé com quem procurar e a referência da rodada. É o que
        /// o fornecedor usa para responder fora do sistema.</summary>
        private static string Assinatura(Comprador comprador, int numeroCotacao)
        {
            var linhas = new List<string>();

            if (!string.IsNullOrWhiteSpace(comprador.Empresa))
                linhas.Add($"<strong>{WebUtility.HtmlEncode(comprador.Empresa)}</strong>");

            if (!string.IsNullOrWhiteSpace(comprador.Cnpj))
                linhas.Add($"CNPJ {WebUtility.HtmlEncode(comprador.Cnpj)}");

            if (!string.IsNullOrWhiteSpace(comprador.Contato))
                linhas.Add($"Responsável: {WebUtility.HtmlEncode(comprador.Contato)}"
                         + (string.IsNullOrWhiteSpace(comprador.EmailContato)
                             ? ""
                             : $" — {WebUtility.HtmlEncode(comprador.EmailContato)}"));

            linhas.Add($"Referência: cotação #{numeroCotacao}");

            return $"<p>—<br/>{string.Join("<br/>", linhas)}</p>";
        }

        /// <summary>
        /// Avisa o fornecedor de que ele venceu e que o link dele reabriu para o
        /// envio da nota. É o mesmo endereço da cotação: o token não muda, muda
        /// a fase.
        /// </summary>
        public Task<bool> EnviarAvisoDeVitoria(
            string destinatario,
            int numeroCotacao,
            string tituloCotacao,
            string link,
            DateTime prazo,
            Comprador comprador)
        {
            var assunto = $"Você venceu a cotação #{numeroCotacao} — {tituloCotacao}";

            var corpo =
                $"""
                 <p>Olá,</p>
                 <p>Sua proposta para <strong>{WebUtility.HtmlEncode(tituloCotacao)}</strong>
                 (cotação #{numeroCotacao}) foi a escolhida por {Apresentacao(comprador)}.</p>
                 <p><a href="{link}">Ver o que você fornece e enviar a nota fiscal</a></p>
                 <p>É o mesmo link que você usou para cotar. Lá estão os itens, quantidades e preços
                 da sua proposta, e o campo para anexar o XML da NF-e.</p>
                 <p>A nota precisa ser emitida contra o CNPJ acima, e com o CNPJ da sua empresa como
                 emitente — é assim que o sistema confere.</p>
                 <p>Prazo para o envio da nota: <strong>{prazo:dd/MM/yyyy}</strong>.</p>
                 {Assinatura(comprador, numeroCotacao)}
                 """;

            return Enviar(destinatario, assunto, corpo);
        }

        public async Task<bool> Enviar(string destinatario, string assunto, string corpoHtml)
        {
            if (!_opcoes.Configurado)
            {
                _log.LogInformation(
                    "E-mail não enviado para {Destinatario}: SMTP não configurado.", destinatario);

                return false;
            }

            try
            {
                using var cliente = new SmtpClient(_opcoes.Host, _opcoes.Porta)
                {
                    EnableSsl = _opcoes.UsarSsl,
                    Credentials = string.IsNullOrWhiteSpace(_opcoes.Usuario)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(_opcoes.Usuario, _opcoes.Senha),
                };

                using var mensagem = new MailMessage
                {
                    From = new MailAddress(_opcoes.Remetente, _opcoes.NomeRemetente),
                    Subject = assunto,
                    Body = corpoHtml,
                    IsBodyHtml = true,
                };

                mensagem.To.Add(destinatario);

                await cliente.SendMailAsync(mensagem);

                return true;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao enviar e-mail para {Destinatario}.", destinatario);

                return false;
            }
        }
    }
}
