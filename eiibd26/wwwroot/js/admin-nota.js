/* ============================================================================
   admin-nota.js — Filas repetibles del editor de nota clínica
   ----------------------------------------------------------------------------
   Antes era admin-nota-panel.js. De aquí se fueron dos de sus tres piezas:

     · El filtro de búsqueda sobre la lista → ahora lo hace DataTables en el grid
       (dejarlo daba DOS buscadores sobre la misma tabla).
     · El abrir/cerrar del panel lateral → el panel murió; la nota se edita en
       página completa (NotaClinicaDetalle), a la que se navega y punto.

   Queda solo lo que sigue siendo cierto: agregar y quitar filas de secciones y
   referencias. Binding de listas por el truco de ".Index": cada fila trae un
   hidden Name.Index con una llave única, así borrar una fila NO exige renumerar
   las demás. Vanilla, sin dependencias.
   ============================================================================ */
(function () {
    "use strict";

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

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initRepetibles);
    } else {
        initRepetibles();
    }
})();
