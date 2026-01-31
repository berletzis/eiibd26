// wwwroot/js/content-meta-loader.js
// Script para manejar el botón "+ Más" en cada card y cargar ContentMeta por AJAX

(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-blog-more');
        if (!btn) return;

        e.preventDefault();

        var card = btn.closest('.blog-card');
        if (!card) return;

        var panel = card.querySelector('.more-panel');
        if (!panel) return;

        var metaUrl = btn.getAttribute('data-meta-url') || panel.getAttribute('data-meta-url');
        if (!metaUrl) return;

        // Si ya está visible, colapsar
        if (panel.style.display === 'block') {
            panel.style.display = 'none';
            panel.setAttribute('aria-hidden', 'true');
            btn.textContent = '+ Más';
            return;
        }

        // Si ya se cargó antes, solo mostrar
        if (panel.getAttribute('data-loaded') === 'true') {
            panel.style.display = 'block';
            panel.setAttribute('aria-hidden', 'false');
            btn.textContent = '− Menos';
            return;
        }

        // Cargar por primera vez
        btn.disabled = true;
        btn.textContent = 'Cargando…';

        fetch(metaUrl, { credentials: 'same-origin' })
            .then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.text();
            })
            .then(function (html) {
                panel.innerHTML = html;
                panel.style.display = 'block';
                panel.setAttribute('aria-hidden', 'false');
                panel.setAttribute('data-loaded', 'true');
                btn.textContent = '− Menos';
            })
            .catch(function (err) {
                console.error('Error cargando ContentMeta:', err);
                btn.textContent = '+ Más';
            })
            .finally(function () {
                btn.disabled = false;
            });
    });
})();