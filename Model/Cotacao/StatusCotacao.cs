namespace Erp.Model.Cotacao
{
    public enum StatusCotacao
    {
        /// <summary>Montando: dá para mexer nos itens e nos convites.</summary>
        Rascunho = 0,

        /// <summary>Links enviados, esperando resposta dos fornecedores.</summary>
        Aberta = 1,

        /// <summary>Não aceita mais proposta. Os links param de funcionar.</summary>
        Encerrada = 2,
    }
}
