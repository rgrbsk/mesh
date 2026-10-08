// Mapa "de onde os usuários acessam" do console da plataforma.
// Carrega o jsVectorMap só quando o mapa é aberto — as demais telas não pagam
// pelos ~140 KB da biblioteca e do contorno do mundo.

let carregando = null;

function script(src) {
    return new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = src;
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    });
}

function carregar() {
    if (window.jsVectorMap && window.jsVectorMap.maps && window.jsVectorMap.maps.world)
        return Promise.resolve();

    if (!carregando) {
        const css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = 'lib/jsvectormap/jsvectormap.min.css';
        document.head.appendChild(css);

        carregando = script('lib/jsvectormap/jsvectormap.min.js')
            .then(() => script('lib/jsvectormap/maps/world.js'));
    }

    return carregando;
}

export async function desenhar(id, pontos) {
    await carregar();

    const alvo = document.getElementById(id);
    if (!alvo) return;

    if (alvo._mapa) {
        alvo._mapa.destroy();
        alvo._mapa = null;
    }
    alvo.innerHTML = '';

    const escuro = document.documentElement.classList.contains('dark');
    const maior = Math.max(1, ...pontos.map(p => p.acessos));

    alvo._mapa = new jsVectorMap({
        selector: '#' + id,
        map: 'world',
        zoomButtons: true,
        zoomOnScroll: false,
        backgroundColor: 'transparent',
        regionStyle: {
            initial: { fill: escuro ? '#3f3f46' : '#d4d4d8', stroke: escuro ? '#27272a' : '#ffffff', strokeWidth: 0.4 },
            hover: { fillOpacity: 0.85, cursor: 'default' },
        },
        markerStyle: {
            initial: { fill: '#8b5cf6', stroke: '#ffffff', strokeWidth: 1.5, fillOpacity: 0.9 },
            hover: { fill: '#a78bfa', cursor: 'pointer' },
        },
        markers: pontos.map(p => ({
            name: `${p.nome} — ${p.acessos} acesso(s), ${p.usuarios} usuário(s)`,
            coords: [p.lat, p.lon],
            style: { r: 5 + Math.round(9 * p.acessos / maior) },
        })),
    });
}

export function destruir(id) {
    const alvo = document.getElementById(id);
    if (alvo && alvo._mapa) {
        alvo._mapa.destroy();
        alvo._mapa = null;
    }
}
