using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Erp.Service.Plataforma
{
    /// <summary>
    /// Telemetria em memória do processo: requisições HTTP da última hora,
    /// sessões Blazor abertas e uso de CPU. Singleton — vive enquanto a
    /// aplicação estiver de pé e zera quando ela reinicia, o que é o esperado
    /// para métrica de operação (o histórico de negócio fica no banco).
    /// </summary>
    public sealed partial class MetricasAplicacao
    {
        public readonly record struct Requisicao(DateTime Quando, string Metodo, string Rota, int Status, double Ms);

        public sealed class Sessao
        {
            public required string CircuitoId { get; init; }
            public Guid? UsuarioId { get; set; }
            public string Login { get; set; } = "";
            public DateTime Desde { get; init; } = DateTime.UtcNow;
            public DateTime UltimaAtividade { get; set; } = DateTime.UtcNow;
            public bool Conectado { get; set; } = true;
        }

        private static readonly TimeSpan Janela = TimeSpan.FromHours(1);
        private const int Limite = 50_000;

        private readonly ConcurrentQueue<Requisicao> _requisicoes = new();
        private readonly ConcurrentDictionary<string, Sessao> _sessoes = new();
        private long _totalRequisicoes;

        private readonly object _cpuLock = new();
        private DateTime _cpuAmostra = DateTime.UtcNow;
        private TimeSpan _cpuTempo = Process.GetCurrentProcess().TotalProcessorTime;
        private double _cpuPercentual;

        public DateTime IniciadoEm { get; } = Process.GetCurrentProcess().StartTime.ToUniversalTime();

        public long TotalRequisicoes => Interlocked.Read(ref _totalRequisicoes);

        // ---------------- Requisições ----------------

        public void Registrar(string metodo, string caminho, int status, double ms)
        {
            Interlocked.Increment(ref _totalRequisicoes);
            _requisicoes.Enqueue(new Requisicao(DateTime.UtcNow, metodo, Normalizar(caminho), status, ms));
            Aparar();
        }

        /// <summary>Requisições ainda dentro da janela de uma hora.</summary>
        public List<Requisicao> Requisicoes()
        {
            Aparar();
            var corte = DateTime.UtcNow - Janela;
            return _requisicoes.Where(r => r.Quando >= corte).ToList();
        }

        private void Aparar()
        {
            var corte = DateTime.UtcNow - Janela;
            while (_requisicoes.TryPeek(out var r) && (r.Quando < corte || _requisicoes.Count > Limite))
                _requisicoes.TryDequeue(out _);
        }

        /// <summary>Troca ids, GUIDs e tokens por marcadores: /cotacao/aB3x… e
        /// /cotacao/Zz9q… são a mesma rota para quem olha desempenho.</summary>
        private static string Normalizar(string caminho)
        {
            if (string.IsNullOrEmpty(caminho))
                return "/";

            var partes = caminho.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => Numero().IsMatch(p) ? "{id}"
                           : Guid.TryParse(p, out _) ? "{id}"
                           : p.Length >= 20 ? "{token}"
                           : p.ToLowerInvariant());

            return "/" + string.Join('/', partes);
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex Numero();

        // ---------------- Sessões (circuitos Blazor) ----------------

        public Sessao AbrirSessao(string circuitoId) =>
            _sessoes.GetOrAdd(circuitoId, id => new Sessao { CircuitoId = id });

        public void FecharSessao(string circuitoId) => _sessoes.TryRemove(circuitoId, out _);

        public void Atividade(string circuitoId)
        {
            if (_sessoes.TryGetValue(circuitoId, out var sessao))
                sessao.UltimaAtividade = DateTime.UtcNow;
        }

        public void Conexao(string circuitoId, bool conectado)
        {
            if (_sessoes.TryGetValue(circuitoId, out var sessao))
                sessao.Conectado = conectado;
        }

        public List<Sessao> Sessoes() => _sessoes.Values.ToList();

        // ---------------- Processo ----------------

        /// <summary>CPU do processo desde a última leitura, em % de todos os
        /// núcleos. Leituras com menos de 1 s de intervalo devolvem a anterior.</summary>
        public double CpuPercentual()
        {
            lock (_cpuLock)
            {
                var agora = DateTime.UtcNow;
                var decorrido = agora - _cpuAmostra;
                if (decorrido < TimeSpan.FromSeconds(1))
                    return _cpuPercentual;

                var tempo = Process.GetCurrentProcess().TotalProcessorTime;
                _cpuPercentual = Math.Clamp(
                    (tempo - _cpuTempo).TotalMilliseconds
                    / (decorrido.TotalMilliseconds * Environment.ProcessorCount) * 100, 0, 100);

                _cpuAmostra = agora;
                _cpuTempo = tempo;
                return _cpuPercentual;
            }
        }
    }
}
