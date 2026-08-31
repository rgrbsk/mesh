using Erp.Model.Log;
using Erp.Repository.Log;

namespace Erp.Service.Log
{
    /// <summary>
    /// Espelha os erros do ILogger na tabela de log de sistema.
    ///
    /// Antes disso o RegistrarExcecao existia e não era chamado de lugar nenhum:
    /// a coluna de stacktrace só encheria se alguém lembrasse de invocá-la em
    /// cada catch. Plugando no pipeline de log, toda exceção que a aplicação já
    /// registra — inclusive as do circuito Blazor e as não tratadas — cai na
    /// tabela sem ninguém precisar lembrar.
    /// </summary>
    public sealed class LogSistemaLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _servicos;

        public LogSistemaLoggerProvider(IServiceProvider servicos) => _servicos = servicos;

        public ILogger CreateLogger(string categoria) => new LogSistemaLogger(categoria, _servicos);

        public void Dispose() { }

        private sealed class LogSistemaLogger : ILogger
        {
            /// <summary>
            /// Guarda contra recursão: gravar o log usa EF, o EF loga, e esse
            /// log voltaria para cá. Sem esta trava, um erro de banco vira um
            /// laço infinito de tentativas de gravar o erro de banco.
            /// </summary>
            private static readonly AsyncLocal<bool> Gravando = new();

            private readonly string _categoria;
            private readonly IServiceProvider _servicos;

            public LogSistemaLogger(string categoria, IServiceProvider servicos)
            {
                _categoria = categoria;
                _servicos = servicos;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            // Só Error e acima: Warning e Information passam aos milhares e
            // afogariam a tabela em ruído.
            public bool IsEnabled(LogLevel nivel) => nivel >= LogLevel.Error;

            public void Log<TState>(
                LogLevel nivel, EventId id, TState state, Exception? excecao,
                Func<TState, Exception?, string> formatar)
            {
                if (!IsEnabled(nivel) || Gravando.Value)
                    return;

                // Categorias do próprio log e do EF ficam de fora pelo mesmo
                // motivo da trava acima.
                if (_categoria.StartsWith("Erp.Repository.Log", StringComparison.Ordinal)
                    || _categoria.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                    return;

                var mensagem = formatar(state, excecao);

                // Dispara e segue: o pipeline de log é síncrono e não pode
                // esperar uma ida ao banco para devolver o controle.
                _ = GravarAsync(nivel, mensagem, excecao);
            }

            private async Task GravarAsync(LogLevel nivel, string mensagem, Exception? excecao)
            {
                Gravando.Value = true;
                try
                {
                    using var escopo = _servicos.CreateScope();

                    var repositorio = escopo.ServiceProvider
                        .GetRequiredService<LogSistemaRepository>();

                    await repositorio.Registrar(new LogSistema
                    {
                        Evento = TipoEventoSistema.Excecao,
                        Mensagem = Encurtar(mensagem),
                        Detalhes = $"{nivel} · {_categoria}"
                                 + (excecao is null ? "" : $" · {excecao.GetType().FullName}"),
                        StackTrace = excecao?.ToString(),
                    });
                }
                catch
                {
                    // Falhar ao gravar o log de um erro não pode gerar outro erro.
                }
                finally
                {
                    Gravando.Value = false;
                }
            }

            /// <summary>A mensagem vai para uma coluna de grade; o texto inteiro
            /// fica no stacktrace.</summary>
            private static string Encurtar(string mensagem) =>
                mensagem.Length <= 500 ? mensagem : mensagem[..500] + "…";
        }
    }
}
