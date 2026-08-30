namespace Erp.Model.Log
{
    /// <summary>
    /// O que aconteceu. É um conjunto fechado de propósito: log com verbo livre
    /// vira texto que ninguém consegue filtrar depois.
    /// </summary>
    public enum TipoAcao
    {
        Criacao = 0,
        Alteracao = 1,
        Exclusao = 2,
        Envio = 3,
        Aprovacao = 4,
        Recusa = 5,
        Devolucao = 6,
        Resposta = 7,
        Escolha = 8,
        Acesso = 9,
        Erro = 10,
    }
}
