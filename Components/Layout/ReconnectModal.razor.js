// Sem botões: apenas exibe/oculta o modal. A reconexão é tentada
// automaticamente em loop pelo próprio Blazor (reconnectionOptions no App.razor).
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

function handleReconnectStateChanged(event) {
    const state = event.detail.state;

    if (state === "show") {
        if (!reconnectModal.open) {
            reconnectModal.showModal();
        }
    } else if (state === "hide") {
        reconnectModal.close();
    } else if (state === "rejected") {
        // O servidor voltou mas o circuito antigo não existe mais: recarrega.
        location.reload();
    }
}
