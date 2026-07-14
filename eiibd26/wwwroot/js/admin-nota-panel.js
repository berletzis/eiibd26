/* ============================================================================
   admin-nota-panel.js — Notas clínicas admin (Ingredientes / Grupos)
   ----------------------------------------------------------------------------
   Vanilla JS, sin dependencias. Tres cosas:
     1) Filtro de búsqueda sobre la tabla de la lista.
     2) Abrir/cerrar el panel lateral (el markup ya viene server-rendered).
     3) Agregar / quitar filas repetibles de secciones y referencias.
   Binding de listas por el truco de ".Index": cada fila trae un hidden
   Name.Index con una llave única, así borrar una fila NO exige renumerar.
   ============================================================================ */
(function () {
    "use strict";

    /* ---------- 1) Filtro de búsqueda ---------- */
    function initFiltro() {
        var input = document.querySelector("[data-nota-filter]");
        var tabla = document.querySelector("[data-nota-filterable]");
        if (!input || !tabla) return;

        input.addEventListener("input", function () {
            var q = input.value.trim().toLowerCase();
            var filas = tabla.querySelectorAll("tbody tr");
            filas.forEach(function (tr) {
                if (tr.hasAttribute("data-nota-empty")) return; // fila "no hay registros"
                var celda = tr.querySelector("[data-nombre]");
                var texto = (celda ? celda.textContent : tr.textContent).toLowerCase();
                tr.classList.toggle("eii-row-hidden", q.length > 0 && texto.indexOf(q) === -1);
            });
        });
    }

    /* ---------- 2) Panel lateral ---------- */
    function initPanel() {
        var panel = document.getElementById("notaPanel");
        if (!panel) return;

        // Animación de entrada: el panel nace sin is-open y lo activamos en el frame siguiente.
        requestAnimationFrame(function () { panel.classList.add("is-open"); });

        var closeUrl = panel.getAttribute("data-close-url") || window.location.pathname;
        function cerrar() {
            panel.classList.remove("is-open");
            window.setTimeout(function () { window.location.href = closeUrl; }, 180);
        }

        var backdrop = panel.querySelector("[data-nota-close]");
        if (backdrop) backdrop.addEventListener("click", cerrar);

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) cerrar();
        });
    }

    /* ---------- 3) Filas repetibles ---------- */
    var contador = 0;
    function nuevaLlave() { return "n" + (contador++); }

    function agregarFila(tplId, contenedorSel) {
        var tpl = document.getElementById(tplId);
        var contenedor = document.querySelector(contenedorSel);
        if (!tpl || !contenedor) return;
        var html = tpl.innerHTML.replace(/__IDX__/g, nuevaLlave());
        var wrap = document.createElement("div");
        wrap.innerHTML = html.trim();
        var fila = wrap.firstElementChild;
        if (!fila) return;
        contenedor.appendChild(fila);
        var primero = fila.querySelector("input, textarea");
        if (primero) primero.focus();
    }

    function initRepetibles() {
        var addSeccion = document.querySelector("[data-nota-add-seccion]");
        if (addSeccion) addSeccion.addEventListener("click", function () {
            agregarFila("tpl-nota-seccion", "[data-nota-secciones]");
        });

        var addRef = document.querySelector("[data-nota-add-ref]");
        if (addRef) addRef.addEventListener("click", function () {
            agregarFila("tpl-nota-referencia", "[data-nota-referencias]");
        });

        // Delegación para el botón × de cada fila (sirve para filas iniciales y nuevas).
        document.addEventListener("click", function (e) {
            var del = e.target.closest("[data-nota-del]");
            if (!del) return;
            var fila = del.closest("[data-nota-row]");
            if (fila) fila.remove();
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initFiltro();
        initPanel();
        initRepetibles();
    });
})();
