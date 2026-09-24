namespace Erp.Model.Financeiro
{
    public enum StatusTitulo
    {
        /// <summary>Em aberto, nada pago.</summary>
        Aberto = 0,

        /// <summary>Recebeu baixa parcial.</summary>
        ParcialmentePago = 1,

        Pago = 2,

        /// <summary>Cancelado — devolução, nota denegada, acordo desfeito.</summary>
        Cancelado = 3,
    }
}
