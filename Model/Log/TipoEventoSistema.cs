namespace Erp.Model.Log
{
    /// <summary>
    /// Eventos de infraestrutura e segurança. Diferente do TipoAcao, que é de
    /// negócio: aqui o assunto é quem entrou, de onde, e o que quebrou.
    /// </summary>
    public enum TipoEventoSistema
    {
        Login = 0,
        LoginFalho = 1,
        Logout = 2,
        SenhaRedefinida = 3,
        AcessoNegado = 4,
        Excecao = 5,
        Inicializacao = 6,
    }
}
