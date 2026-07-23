/* ============================================================================
   admin-platillos-ia.js — Botones "Generar con IA" de los editores de Platillos
   ----------------------------------------------------------------------------
   Reusa el comportamiento del botón de Síntomas: deshabilita + spinner durante
   la llamada, y RELLENA el formulario — NUNCA guarda. El humano revisa y guarda.
   Si el campo ya tiene contenido, pide confirmación antes de sobrescribir.

   Dos tipos de botón, por data-attribute:
     data-ia-kind="texto"  → rellena un solo campo (NotasEII, Descripción).
                             data-ia-target = selector del input/textarea destino.
     data-ia-kind="nota"   → rellena el editor de nota clínica completo
                             (título + 3 secciones + referencias).

   Endpoint en data-ia-endpoint. Vanilla, sin dependencias.
   ============================================================================ */
(function () {
    "use strict";

    function spinnerOn(btn) {
        btn.disabled = true;
        btn.dataset.iaLabel = btn.innerHTML;
        btn.innerHTML = '<span class="loading-spinner"></span><span>Generando…</span>';
    }

    function spinnerOff(btn) {
        btn.disabled = false;
        if (btn.dataset.iaLabel) btn.innerHTML = btn.dataset.iaLabel;
    }

    async function llamar(endpoint) {
        var resp = await fetch(endpoint, {
            method: "POST",
            headers: { "Content-Type": "application/json" }
        });
        var data = null;
        try { data = await resp.json(); } catch (e) { /* respuesta no-JSON */ }
        if (!resp.ok || !data || !data.ok) {
            var msg = (data && data.error) ? data.error : ("Error " + resp.status);
            throw new Error(msg);
        }
        return data;
    }

    // ---- Tipo "texto": un solo campo -----------------------------------------
    function tieneValor(el) { return el && String(el.value || "").trim().length > 0; }

    async function generarTexto(btn) {
        var target = document.querySelector(btn.dataset.iaTarget);
        if (!target) { alert("No se encontró el campo destino."); return; }
        if (tieneValor(target) &&
            !confirm("Este campo ya tiene contenido. ¿Reemplazarlo con lo generado por la IA?")) {
            return;
        }
        spinnerOn(btn);
        try {
            var data = await llamar(btn.dataset.iaEndpoint);
            target.value = data.texto || "";
            target.focus();
        } catch (e) {
            alert("No se pudo generar el contenido: " + e.message);
        } finally {
            spinnerOff(btn);
        }
    }

    // ---- Tipo "nota": editor de nota clínica completo ------------------------
    function filas(contenedorSel) {
        var c = document.querySelector(contenedorSel);
        return c ? Array.prototype.slice.call(c.querySelectorAll("[data-nota-row]")) : [];
    }

    // Asegura que haya al menos n filas, usando el botón "Agregar" del editor
    // (lo maneja admin-nota.js) para que las filas nuevas queden bien indexadas.
    function asegurarFilas(contenedorSel, addBtnSel, n) {
        var addBtn = document.querySelector(addBtnSel);
        var guarda = 0;
        while (filas(contenedorSel).length < n && addBtn && guarda++ < 50) addBtn.click();
        return filas(contenedorSel);
    }

    function setCampo(fila, selector, valor) {
        var el = fila.querySelector(selector);
        if (el) el.value = valor || "";
    }

    function notaTieneContenido() {
        var titulo = document.querySelector("#NotaTitulo");
        if (tieneValor(titulo)) return true;
        return filas("[data-nota-secciones]").some(function (f) {
            var t = f.querySelector('[name$=".Contenido"]');
            return t && String(t.value || "").trim().length > 0;
        });
    }

    function mostrarRevision(btn, mostrar) {
        var aviso = document.querySelector("#eii-ia-revision");
        if (!aviso) {
            if (!mostrar) return;
            aviso = document.createElement("div");
            aviso.id = "eii-ia-revision";
            aviso.className = "eii-ia-revision";
            btn.parentNode.insertBefore(aviso, btn.nextSibling);
        }
        aviso.hidden = !mostrar;
        if (mostrar) {
            aviso.innerHTML = '<i class="bi bi-exclamation-triangle" aria-hidden="true"></i> ' +
                "La IA no encontró una fuente aplicable de la lista blanca: se generó sin " +
                "referencias. <strong>Revísala con prioridad</strong> antes de publicar.";
        }
    }

    async function generarNota(btn) {
        if (notaTieneContenido() &&
            !confirm("La nota ya tiene contenido. ¿Reemplazar título y secciones con lo generado por la IA?")) {
            return;
        }
        spinnerOn(btn);
        try {
            var data = await llamar(btn.dataset.iaEndpoint);

            var titulo = document.querySelector("#NotaTitulo");
            if (titulo) titulo.value = data.titulo || "";

            // 3 secciones fijas de la estructura de la nota.
            var secciones = [
                { titulo: "¿Qué es?", contenido: data.queEs },
                { titulo: "¿Qué suele pasar?", contenido: data.queSuelePasar },
                { titulo: "Importante", contenido: data.importante }
            ];
            var filasSec = asegurarFilas("[data-nota-secciones]", "[data-nota-add-seccion]", secciones.length);
            secciones.forEach(function (s, i) {
                if (!filasSec[i]) return;
                setCampo(filasSec[i], '[name$=".Titulo"]', s.titulo);
                setCampo(filasSec[i], '[name$=".Contenido"]', s.contenido);
            });

            // Referencias: REGENERAR = borrador nuevo. Reemplaza TODAS las referencias por las de la
            // IA (ya filtradas por lista blanca). Se limpian primero las anteriores para no arrastrar
            // una manual o heredada de un estado previo (el usuario ya confirmó el sobrescribir).
            limpiarReferencias();
            var fuentes = Array.isArray(data.fuentes) ? data.fuentes : [];
            if (fuentes.length > 0) {
                var filasRef = asegurarFilas("[data-nota-referencias]", "[data-nota-add-ref]", fuentes.length);
                fuentes.forEach(function (f, i) {
                    if (!filasRef[i]) return;
                    setCampo(filasRef[i], '[name$=".Titulo"]', f);
                    setCampo(filasRef[i], '[name$=".Url"]', "");
                });
            }
            marcarTodasLasReferencias();

            mostrarRevision(btn, !!data.revisionPrioritaria);
            mostrarSugerencias(data.referenciasCandidatas);
        } catch (e) {
            alert("No se pudo generar la nota: " + e.message);
        } finally {
            spinnerOff(btn);
        }
    }

    // ---- Referencias recuperadas (links reales del índice) -------------------
    function escaparHtml(s) {
        var d = document.createElement("div");
        d.textContent = (s == null) ? "" : String(s);
        return d.innerHTML;
    }

    // Inserta una referencia en el editor: reusa una fila vacía o crea una nueva.
    function agregarReferencia(titulo, url) {
        var contenedor = document.querySelector("[data-nota-referencias]");
        if (!contenedor) return;
        var rows = Array.prototype.slice.call(contenedor.querySelectorAll("[data-nota-row]"));
        var target = rows.find(function (r) {
            var t = r.querySelector('[name$=".Titulo"]');
            var u = r.querySelector('[name$=".Url"]');
            return (!t || !String(t.value || "").trim()) && (!u || !String(u.value || "").trim());
        });
        if (!target) {
            var addBtn = document.querySelector("[data-nota-add-ref]");
            if (addBtn) addBtn.click();
            rows = contenedor.querySelectorAll("[data-nota-row]");
            target = rows[rows.length - 1];
        }
        if (!target) return;
        setCampo(target, '[name$=".Titulo"]', titulo);
        setCampo(target, '[name$=".Url"]', url);
        marcarFila(target);   // el .value programático no dispara 'input'
    }

    function mostrarSugerencias(cands) {
        var panel = document.querySelector("#eii-ref-sugeridas");
        if (!panel) return;
        cands = Array.isArray(cands) ? cands : [];
        if (cands.length === 0) { panel.hidden = true; panel.innerHTML = ""; return; }

        // Título/URL vienen de páginas externas crawleadas → escapar SIEMPRE (anti-inyección).
        var html = '<div class="eii-ref-sugeridas__head">' +
            '<i class="bi bi-link-45deg" aria-hidden="true"></i> Referencias sugeridas ' +
            '(links reales recuperados del índice). Revisa que respalden lo que dice la nota antes de agregarlas.' +
            '</div>';
        cands.forEach(function (c) {
            var meta = escaparHtml(c.sitio || "");
            if (c.porcentaje != null) meta += (meta ? " · " : "") + c.porcentaje + "% similar";
            html += '<div class="eii-ref-sug">' +
                '<div class="eii-ref-sug__info">' +
                '<a href="' + escaparHtml(c.url) + '" target="_blank" rel="noopener" class="eii-ref-sug__title">' +
                escaparHtml(c.titulo || c.url) + '</a>' +
                '<span class="eii-ref-sug__meta">' + meta + '</span>' +
                '</div>' +
                '<button type="button" class="eii-btn eii-btn--ghost eii-btn--sm eii-ref-sug__add">Agregar</button>' +
                '</div>';
        });
        panel.innerHTML = html;
        panel.hidden = false;

        panel.querySelectorAll(".eii-ref-sug").forEach(function (row, i) {
            var c = cands[i];
            var add = row.querySelector(".eii-ref-sug__add");
            if (!add) return;
            add.addEventListener("click", function () {
                agregarReferencia(c.titulo || c.url, c.url);
                add.disabled = true;
                add.textContent = "Agregada ✓";
            });
        });
    }

    // ---- Referencias fuera de la lista blanca: marca ámbar (no bloquea) ------
    // Espeja la lógica tolerante de FiltrarPorListaBlanca (backend): sin acentos/mayúsculas,
    // match bidireccional contains/contained-by. Solo AVISA para que el humano revise.
    function normalizar(s) {
        return (s == null ? "" : String(s)).trim().toLowerCase()
            .normalize("NFD").replace(/[̀-ͯ]/g, "");
    }

    var fuentesPermitidasNorm = (function () {
        var el = document.querySelector("#eii-fuentes-permitidas");
        if (!el) return [];
        try {
            var arr = JSON.parse(el.textContent || "[]");
            return (Array.isArray(arr) ? arr : []).map(normalizar).filter(Boolean);
        } catch (e) { return []; }
    })();

    function esFueraDeLista(titulo) {
        var t = normalizar(titulo);
        if (!t) return false;                                  // vacío = sin marca
        if (fuentesPermitidasNorm.length === 0) return false;  // sin lista, no marcamos nada
        return !fuentesPermitidasNorm.some(function (p) {
            return t.indexOf(p) !== -1 || p.indexOf(t) !== -1;
        });
    }

    function limpiarReferencias() {
        var c = document.querySelector("[data-nota-referencias]");
        if (!c) return;
        c.querySelectorAll("[data-nota-row]").forEach(function (r) { r.remove(); });
    }

    function marcarFila(row) {
        if (!row) return;
        var titInput = row.querySelector('[name$=".Titulo"]');
        if (!titInput) return;
        var fuera = esFueraDeLista(titInput.value);
        row.classList.toggle("eii-nota-row--fuera", fuera);
        var aviso = row.querySelector(".eii-ref-fuera");
        if (fuera && !aviso) {
            aviso = document.createElement("div");
            aviso.className = "eii-ref-fuera";
            aviso.innerHTML = '<i class="bi bi-exclamation-triangle" aria-hidden="true"></i> ' +
                "Fuera de la lista blanca aprobada — verifica que sea una fuente real y que respalde lo que dice la nota.";
            var grid = row.querySelector(".eii-nota-row__grid");
            if (grid) grid.insertAdjacentElement("afterend", aviso); else row.appendChild(aviso);
        }
        if (aviso) aviso.hidden = !fuera;
    }

    function marcarTodasLasReferencias() {
        var c = document.querySelector("[data-nota-referencias]");
        if (!c) return;
        c.querySelectorAll("[data-nota-row]").forEach(marcarFila);
    }

    function initMarcaReferencias() {
        marcarTodasLasReferencias();   // al cargar (sirve de lente de auditoría sobre lo ya generado)
        // Re-evaluar al escribir/pegar en el título de una referencia (delegado: cubre filas nuevas).
        document.addEventListener("input", function (e) {
            var input = e.target;
            if (input && input.matches && input.matches('[name^="NotaReferencias"][name$=".Titulo"]')) {
                marcarFila(input.closest("[data-nota-row]"));
            }
        });
    }

    function init() {
        document.querySelectorAll("[data-ia-btn]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                if (btn.dataset.iaKind === "nota") generarNota(btn);
                else generarTexto(btn);
            });
        });
        initMarcaReferencias();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
