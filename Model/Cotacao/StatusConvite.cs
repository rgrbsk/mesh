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

        /// <summary>
        /// Ganhou pelo menos um item e foi avisado. O MESMO link reabre, agora
        /// numa segunda fase: mostra o que ele venceu e aceita o XML da NF-e.
        /// </summary>
        Vencedor = 3,

        /// <summary>Mandou a nota. O link trava de novo.</summary>
        NotaEnviada = 4,
    }
}
