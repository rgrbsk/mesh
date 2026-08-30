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
            string destinatario, string tituloCotacao, string link, DateTime prazo)
        {
            var assunto = $"Cotação: {tituloCotacao}";

            var corpo =
                $"""
                 <p>Olá,</p>
                 <p>Você foi convidado a enviar uma proposta para <strong>{WebUtility.HtmlEncode(tituloCotacao)}</strong>.</p>
                 <p><a href="{link}">Abrir o formulário da cotação</a></p>
                 <p>O link é pessoal e vale até <strong>{prazo:dd/MM/yyyy}</strong>.
                 Não é preciso criar conta nem senha.</p>
                 <p>—<br/>{WebUtility.HtmlEncode(_opcoes.NomeRemetente)}</p>
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
