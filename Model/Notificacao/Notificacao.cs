namespace Erp.Model.Notificacao
{
    /// <summary>
    /// Aviso dirigido a UMA pessoa. Existe porque o fluxo de compras é uma
    /// sequência de esperas: quem envia espera decisão, quem aprova não sabe que
    /// chegou algo, e sem aviso os dois lados dependem de lembrar de abrir a
    /// tela.
    ///
    /// O texto fica gravado pronto, não montado na leitura: a solicitação pode
    /// mudar depois, e o aviso tem que continuar dizendo o que dizia quando foi
    /// gerado.
    /// </summary>
    public class Notificacao
    {
        public int Id { get; set; }

        public Guid DestinatarioId { get; set; }

        public Erp.Model.Usuario.Usuario? Destinatario { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Mensagem { get; set; } = string.Empty;

        /// <summary>Ícone lucide, para o aviso ser reconhecível de relance.</summary>
        public string Icone { get; set; } = "bell";

        /// <summary>Para onde clicar leva. Vazio = aviso sem destino.</summary>
        public string Link { get; set; } = string.Empty;

        public bool Lida { get; set; }

        public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

        public DateTime? LidaEm { get; set; }
    }
}
