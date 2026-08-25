// Tema claro/escuro. Carregado de forma SÍNCRONA no <head> (ver App.razor):
// a classe .dark precisa estar no <html> antes da primeira pintura, senão a
// tela pisca branca a cada navegação antes do circuito Blazor subir.
//
// A preferência mora no localStorage — leitura instantânea, sem ida ao
// servidor. "sistema" acompanha a configuração do SO.
(function () {
    const CHAVE = "mesh-tema";

    const preferenciaDoSistema = () =>
        window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";

    const resolver = (tema) => (tema === "sistema" || !tema ? preferenciaDoSistema() : tema);

    function aplicar(tema) {
        const efetivo = resolver(tema);
        document.documentElement.classList.toggle("dark", efetivo === "dark");
        // Faz o navegador pintar scrollbars e controles nativos no tom certo.
        document.documentElement.style.colorScheme = efetivo;
        return efetivo === "dark";
    }

    window.mesh = window.mesh || {};
    window.mesh.tema = {
        /// Tema salvo, sem resolver: "light", "dark" ou "sistema".
        salvo: () => localStorage.getItem(CHAVE) || "sistema",

        /// true se o tema em vigor é o escuro.
        escuro: () => document.documentElement.classList.contains("dark"),

        definir(tema) {
            localStorage.setItem(CHAVE, tema);
            return aplicar(tema);
        },

        alternar() {
            return window.mesh.tema.definir(
                document.documentElement.classList.contains("dark") ? "light" : "dark");
        },
    };

    // Aplica já, ainda no <head>.
    aplicar(localStorage.getItem(CHAVE));

    // Enquanto o usuário estiver em "sistema", acompanha a troca no SO.
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
        if ((localStorage.getItem(CHAVE) || "sistema") === "sistema") aplicar("sistema");
    });
})();
