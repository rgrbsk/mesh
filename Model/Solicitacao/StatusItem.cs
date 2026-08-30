namespace Erp.Model.Solicitacao
{
    /// <summary>
    /// A decisão é por ITEM, porque o aprovador é o responsável pelo centro de
    /// custo do item — uma solicitação com itens de dois centros passa por dois
    /// aprovadores e pode terminar meio aprovada.
    /// </summary>
    public enum StatusItem
    {
        Pendente = 0,
        Aprovado = 1,
        Recusado = 2,
        Devolvido = 3,
    }
}
