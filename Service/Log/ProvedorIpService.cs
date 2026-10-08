using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Erp.Service.Log
{
    /// <summary>
    /// Configuração da consulta de provedor. Vem DESLIGADA: descobrir o dono de
    /// um IP significa mandar o endereço do seu usuário para um terceiro, e essa
    /// é uma decisão de privacidade que cabe a quem opera o sistema, não um
    /// padrão que eu deva ligar sozinho.
    /// </summary>
    public sealed class ProvedorIpOptions
    {
        public const string Secao = "ProvedorIp";

        /// <summary>Liga a consulta externa.</summary>
        public bool Habilitado { get; set; }

        /// <summary>
        /// Endpoint que recebe o IP no lugar de {ip} e devolve JSON. O padrão é
        /// o ip-api.com, gratuito e sem chave para uso baixo — troque pelo
        /// serviço que você contratar.
        /// </summary>
        public string Url { get; set; } = "http://ip-api.com/json/{ip}?fields=status,isp,org,country,city,lat,lon";

        /// <summary>Propriedades do JSON tentadas em ordem até uma vir preenchida.</summary>
        public string[] Campos { get; set; } = ["isp", "org"];

        public int TimeoutSegundos { get; set; } = 3;
    }

    /// <summary>
    /// Descobre a operadora/organização dona de um IP.
    ///
    /// Nunca lança e nunca demora: se a consulta falhar ou estourar o tempo, o
    /// provedor volta vazio. Um login não pode ficar mais lento — nem falhar —
    /// porque um serviço de terceiro está fora do ar.
    /// </summary>
    public sealed class ProvedorIpService
    {
        private readonly IHttpClientFactory _http;
        private readonly ProvedorIpOptions _opcoes;
        private readonly ILogger<ProvedorIpService> _log;

        /// <summary>
        /// Cache em memória: o mesmo usuário loga do mesmo IP o dia inteiro, e
        /// cada consulta é uma ida à internet. Some quando a aplicação reinicia,
        /// o que é aceitável para um dado de conveniência.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Origem> CacheOrigem = new();

        public ProvedorIpService(
            IHttpClientFactory http, IOptions<ProvedorIpOptions> opcoes, ILogger<ProvedorIpService> log)
        {
            _http = http;
            _opcoes = opcoes.Value;
            _log = log;
        }

        /// <summary>Origem aproximada de um IP. Vazia com a consulta desligada.</summary>
        public sealed record Origem(string Provedor, string Pais, string Cidade, double? Latitude, double? Longitude)
        {
            public static readonly Origem Vazia = new("", "", "", null, null);
        }

        public async Task<string> Consultar(string? ip) => (await Localizar(ip)).Provedor;

        /// <summary>
        /// Operadora, país, cidade e coordenadas do IP. Acesso pela rede local
        /// (localhost, 192.168.x) não tem localização própria: é marcado com a
        /// localização pública do servidor, que é onde a pessoa fisicamente está.
        /// </summary>
        public async Task<Origem> Localizar(string? ip)
        {
            if (!_opcoes.Habilitado || string.IsNullOrWhiteSpace(ip)
                || !IPAddress.TryParse(ip, out var endereco))
                return Origem.Vazia;

            var local = EhLocal(endereco);
            var chave = local ? "local" : ip;

            if (CacheOrigem.TryGetValue(chave, out var doCache))
                return doCache;

            var origem = Origem.Vazia;

            try
            {
                using var cliente = _http.CreateClient();
                cliente.Timeout = TimeSpan.FromSeconds(_opcoes.TimeoutSegundos);

                // Sem IP no caminho, o serviço responde pelo IP público de quem
                // pergunta — o próprio servidor.
                var url = local ? _opcoes.Url.Replace("{ip}", "") : _opcoes.Url.Replace("{ip}", ip);
                using var json = JsonDocument.Parse(await cliente.GetStringAsync(url));
                var raiz = json.RootElement;

                string Texto(string campo) =>
                    raiz.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                double? Numero(string campo) =>
                    raiz.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

                if (Texto("status") is "success" or "")
                {
                    var provedor = _opcoes.Campos.Select(Texto).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? "";
                    origem = new Origem(
                        local ? "Rede local" : provedor,
                        Texto("country"),
                        local ? $"{Texto("city")} (rede local)".Trim() : Texto("city"),
                        Numero("lat"),
                        Numero("lon"));
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Consulta de origem falhou para {Ip}.", ip);
            }

            // Grava o vazio também: sem isso um IP que sempre falha viraria uma
            // consulta nova a cada login.
            CacheOrigem[chave] = origem;

            return origem;
        }

        private static bool EhLocal(IPAddress endereco)
        {
            if (IPAddress.IsLoopback(endereco))
                return true;

            var bytes = endereco.GetAddressBytes();

            return endereco.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                && (bytes[0] == 10
                 || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                 || (bytes[0] == 192 && bytes[1] == 168));
        }
    }
}
