
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

// O usuário não pode fechar o loading com Esc: sem circuito não há app.
reconnectModal.addEventListener("cancel", (e) => e.preventDefault());

function handleReconnectStateChanged(event) {
    const state = event.detail.state;

    if (state === "hide") {
        reconnectModal.close();
    } else if (state === "rejected") {
        // O servidor voltou mas o circuito antigo não existe mais: recarrega.
        location.reload();
    } else if (!reconnectModal.open) {
        // show, retrying, paused, failed… — qualquer estado ativo abre o modal.
        reconnectModal.showModal();
    }
}
