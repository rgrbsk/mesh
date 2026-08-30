namespace Erp.Model.Cotacao
{
    public enum StatusConvite
    {
        /// <summary>Link válido, ainda sem resposta.</summary>
        Pendente = 0,

        /// <summary>Fornecedor enviou. O link trava — reabrir é ação do comprador.</summary>
        Respondido = 1,

        /// <summary>Comprador cancelou este convite.</summary>
        Cancelado = 2,
    }
}
