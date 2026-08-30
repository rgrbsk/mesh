namespace Erp.Model.Log
{
    /// <summary>
    /// Uma linha do histórico. Registra o que foi feito, por quem e sobre o quê
    /// — num sistema de compras é isto que responde "quem aprovou aquilo".
    ///
    /// O nome do usuário fica COPIADO aqui, não só a chave estrangeira: se a
    /// conta for excluída depois, o histórico não pode virar uma linha anônima.
    /// </summary>
    public class RegistroLog
    {
        public int Id { get; set; }

        public DateTime Quando { get; set; } = DateTime.UtcNow;

        public TipoAcao Acao { get; set; }

        /// <summary>Onde aconteceu — "Compras", "Cotações", "Usuários".</summary>
        public string Modulo { get; set; } = string.Empty;

        public Guid? UsuarioId { get; set; }

        /// <summary>Instantâneo do nome. Vazio quando a ação veio de fora do
        /// login, como a resposta de um fornecedor pelo link público.</summary>
        public string UsuarioNome { get; set; } = string.Empty;

        /// <summary>O que se lê na tela. Frase pronta, não template.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Tipo do registro afetado, para filtrar — "SolicitacaoCompra".</summary>
        public string Entidade { get; set; } = string.Empty;

        public string EntidadeId { get; set; } = string.Empty;

        /// <summary>
        /// Só em erro. Fica separado da descrição porque é informação de
        /// diagnóstico: expõe caminho de arquivo e estrutura interna, então tem
        /// permissão própria para ser lido.
        /// </summary>
        public string? StackTrace { get; set; }

        public bool TemStackTrace => !string.IsNullOrWhiteSpace(StackTrace);
    }
}
