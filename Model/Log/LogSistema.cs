namespace Erp.Model.Log
{
    /// <summary>
    /// Log técnico e de segurança, separado do log de negócio (RegistroLog).
    ///
    /// A separação não é organizacional, é de acesso e de retenção: aqui há
    /// endereço IP, agente do navegador e rastreamento de exceção — dado
    /// sensível, que interessa a quem opera o sistema e não a quem compra. Por
    /// isso tem permissão própria e vive em outra tabela, que pode ser expurgada
    /// em ritmo diferente do histórico de aprovações.
    /// </summary>
    public class LogSistema
    {
        public int Id { get; set; }

        public DateTime Quando { get; set; } = DateTime.UtcNow;

        public TipoEventoSistema Evento { get; set; }

        /// <summary>Uma linha. O detalhe longo vai em Detalhes ou StackTrace.</summary>
        public string Mensagem { get; set; } = string.Empty;

        public Guid? UsuarioId { get; set; }

        /// <summary>Instantâneo do nome — o histórico não pode virar anônimo se
        /// a conta for excluída.</summary>
        public string UsuarioNome { get; set; } = string.Empty;

        /// <summary>Em login falho é a única identificação que existe: o e-mail
        /// tentado, que pode nem ser de um usuário real.</summary>
        public string Identificacao { get; set; } = string.Empty;

        // ---- Origem da requisição ----

        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// Provedor / organização dona do IP. Fica em branco enquanto não houver
        /// uma consulta de geolocalização configurada — inventar valor aqui seria
        /// pior que a coluna vazia num log de segurança.
        /// </summary>
        public string Provedor { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;

        /// <summary>Navegador e sistema já resolvidos, para a tela não repetir o
        /// parse do UserAgent a cada renderização.</summary>
        public string Dispositivo { get; set; } = string.Empty;

        // ---- Diagnóstico ----

        public string? Detalhes { get; set; }

        /// <summary>Só em exceção. Protegido por logs.stacktrace.</summary>
        public string? StackTrace { get; set; }

        public bool TemStackTrace => !string.IsNullOrWhiteSpace(StackTrace);
    }
}
