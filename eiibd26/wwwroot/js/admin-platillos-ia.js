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

            // Referencias: una por fuente válida (ya filtradas por lista blanca en el backend).
            var fuentes = Array.isArray(data.fuentes) ? data.fuentes : [];
            if (fuentes.length > 0) {
                var filasRef = asegurarFilas("[data-nota-referencias]", "[data-nota-add-ref]", fuentes.length);
                fuentes.forEach(function (f, i) {
                    if (!filasRef[i]) return;
                    setCampo(filasRef[i], '[name$=".Titulo"]', f);
                    setCampo(filasRef[i], '[name$=".Url"]', "");
                });
            }

            mostrarRevision(btn, !!data.revisionPrioritaria);
        } catch (e) {
            alert("No se pudo generar la nota: " + e.message);
        } finally {
            spinnerOff(btn);
        }
    }

    function init() {
        document.querySelectorAll("[data-ia-btn]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                if (btn.dataset.iaKind === "nota") generarNota(btn);
                else generarTexto(btn);
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
