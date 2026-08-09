// ── Supressão do modal de reconexão em navegações intencionais ───────────────
// Um POST de página inteira (login/logout) derruba o circuito SignalR por um
// instante, e o Blazor mostra o "reconectando" antes da nova página carregar.
// Ao sair intencionalmente, injetamos um estilo que esconde o dialog e o backdrop
// — sem desabilitar o modal para quedas de conexão de verdade.
function suppressReconnectModal() {
    if (document.getElementById('__suppress_reconnect')) return;
    const style = document.createElement('style');
    style.id = '__suppress_reconnect';
    style.textContent =
        '#components-reconnect-modal,#components-reconnect-modal::backdrop{display:none !important;}';
    document.head.appendChild(style);
}

window.addEventListener('pagehide', suppressReconnectModal);
window.addEventListener('beforeunload', suppressReconnectModal);

// ── POST nativo do login ─────────────────────────────────────────────────────
// O sign-in de cookie precisa de uma resposta HTTP real, que o circuito Blazor
// não tem. Montamos e submetemos um <form> de verdade.
window.submitLogin = function (email, senha) {
    suppressReconnectModal(); // evita o flash de "reconectando" ao navegar

    const form = document.createElement('form');
    form.method = 'post';
    form.action = '/auth/login';

    const add = (name, value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
    };
    add('email', email);
    add('senha', senha);

    document.body.appendChild(form);
    form.submit();
};
