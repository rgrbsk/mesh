namespace Erp.Model.Solicitacao
{
    /// <summary>Estado do cabeçalho. Rascunho e Enviada são o que esta fase usa;
    /// os demais são o resultado das decisões do aprovador, item a item.</summary>
    public enum StatusSolicitacao
    {
        Rascunho = 0,
        Enviada = 1,
        Aprovada = 2,
        AprovadaParcialmente = 3,
        Recusada = 4,
        Devolvida = 5,
    }
}
