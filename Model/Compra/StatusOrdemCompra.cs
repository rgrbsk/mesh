namespace Erp.Model.Compra
{
    /// <summary>
    /// Estado do pedido ao fornecedor. É o documento que faltava entre a
    /// escolha do vencedor e a chegada da mercadoria: sem ele não há o que
    /// enviar ao fornecedor, nem contra o que conferir o recebimento.
    /// </summary>
    public enum StatusOrdemCompra
    {
        /// <summary>Montada, ainda não enviada. Único estado editável.</summary>
        Rascunho = 0,

        /// <summary>Enviada ao fornecedor. A partir daqui os itens congelam.</summary>
        Enviada = 1,

        /// <summary>Parte da quantidade já entrou; o saldo continua pendente.</summary>
        ParcialmenteRecebida = 2,

        /// <summary>Todo o pedido foi recebido.</summary>
        Recebida = 3,

        /// <summary>Cancelada antes do atendimento. Não some: o histórico fica.</summary>
        Cancelada = 4,
    }
}
