using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Erp.Service.Plataforma
{
    /// <summary>
    /// Acompanha cada circuito Blazor (uma aba aberta): quem é, desde quando e
    /// quando mexeu por último. É o que alimenta "usuários online agora".
    /// </summary>
    public sealed class SessoesCircuitHandler : CircuitHandler
    {
        private readonly MetricasAplicacao _metricas;
        private readonly AuthenticationStateProvider _autenticacao;
        private string? _circuitoId;

        public SessoesCircuitHandler(MetricasAplicacao metricas, AuthenticationStateProvider autenticacao)
        {
            _metricas = metricas;
            _autenticacao = autenticacao;
        }

        public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _circuitoId = circuit.Id;
            _metricas.AbrirSessao(circuit.Id);
            _autenticacao.AuthenticationStateChanged += AoMudarAutenticacao;
            await Identificar();
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _autenticacao.AuthenticationStateChanged -= AoMudarAutenticacao;
            _metricas.FecharSessao(circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _metricas.Conexao(circuit.Id, true);
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _metricas.Conexao(circuit.Id, false);
            return Task.CompletedTask;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
            Func<CircuitInboundActivityContext, Task> next) =>
            contexto =>
            {
                if (_circuitoId is not null)
                    _metricas.Atividade(_circuitoId);

                return next(contexto);
            };

        private void AoMudarAutenticacao(Task<AuthenticationState> estado) =>
            _ = Identificar(estado);

        private async Task Identificar(Task<AuthenticationState>? estado = null)
        {
            if (_circuitoId is null)
                return;

            try
            {
                var usuario = (await (estado ?? _autenticacao.GetAuthenticationStateAsync())).User;
                var sessao = _metricas.AbrirSessao(_circuitoId);

                sessao.UsuarioId = Guid.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                    ? id
                    : null;
                sessao.Login = usuario.Identity?.Name ?? "";
            }
            catch (InvalidOperationException)
            {
                // Estado de autenticação ainda não definido neste circuito: a
                // próxima mudança de estado identifica.
            }
        }
    }
}
