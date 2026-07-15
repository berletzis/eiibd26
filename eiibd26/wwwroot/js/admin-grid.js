/* ============================================================================
   admin-grid.js — Grid compartido de los catálogos admin
   ----------------------------------------------------------------------------
   Auto-init: la página no escribe JS, solo marca su markup con data-*.

   DOS ROLES, un solo archivo:

   1) LA PESTAÑA DEL GRID — <table data-eii-grid id="gridX">
        DataTables client-side sobre filas server-rendered + stateSave.
        Escucha avisos de guardado y ofrece una píldora "Actualizar".

   2) LA PESTAÑA DE EDICIÓN — <div data-eii-grid-notify="unidad" hidden>
        Se renderiza SOLO tras un guardado exitoso. Al cargar, avisa a la
        pestaña del grid. No recarga nada, no cierra nada.

   El grid NUNCA se mueve solo: muestra la píldora y el usuario decide. Al
   pulsar "Actualizar" recarga, y stateSave lo devuelve a su búsqueda / orden /
   página — por eso las dos piezas van juntas.

   Config de la tabla (todo opcional salvo el id):
     data-eii-grid-empty        texto cuando no hay filas
     data-eii-grid-page-length  filas por página (default 25)
     data-eii-grid-order        orden inicial, "0:asc" (default: el del server)
     data-eii-grid-state        "off" desactiva stateSave
     data-eii-grid-server       URL de un handler GridData → modo server-side
     <th data-no-sort>          columna no ordenable

   EL ID DE LA TABLA ES OBLIGATORIO: stateSave guarda en localStorage con la
   llave DataTables_{id}_{pathname}. Sin id, la llave no es estable y el estado
   no sobrevive a la recarga — que es justo lo que hace usable a la píldora.

   Depende de jQuery (global, viene del _Layout) + DataTables (lo carga la
   página del grid). Falla suave: sin DataTables la tabla se queda
   server-rendered, y la página de edición no necesita ninguna de las dos.
   ============================================================================ */
(function () {
    "use strict";

    var CANAL = "eii-admin-grid";
    var LS_KEY = "eii-admin-grid-guardado";

    var LANG_ES = {
        emptyTable: "No hay registros.",
        zeroRecords: "Ningún registro coincide con la búsqueda.",
        info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
        infoEmpty: "Mostrando 0 registros",
        infoFiltered: "(filtrado de _MAX_ en total)",
        lengthMenu: "Mostrar _MENU_ registros",
        loadingRecords: "Cargando…",
        processing: "Procesando…",
        search: "Buscar:",
        thousands: ",",
        paginate: {
            first: "Primero",
            last: "Último",
            next: "Siguiente",
            previous: "Anterior"
        },
        aria: {
            sortAscending: ": activar para ordenar la columna ascendente",
            sortDescending: ": activar para ordenar la columna descendente"
        }
    };

    /* ---------- Grid ---------- */
    function initGrid(table) {
        var $ = window.jQuery;
        if (!$ || !$.fn || !$.fn.DataTable) return;      // falla suave
        if ($.fn.DataTable.isDataTable(table)) return;   // idempotente

        // Columnas no ordenables: se marcan en el <th>, no por índice, para que
        // insertar una columna no desincronice una lista de números.
        var noSort = [];
        var ths = table.querySelectorAll("thead th");
        for (var i = 0; i < ths.length; i++) {
            if (ths[i].hasAttribute("data-no-sort")) noSort.push(i);
        }

        var lang = Object.assign({}, LANG_ES);
        var empty = table.getAttribute("data-eii-grid-empty");
        if (empty) lang.emptyTable = empty;

        var opts = {
            serverSide: false,
            stateSave: table.getAttribute("data-eii-grid-state") !== "off",
            pageLength: parseInt(table.getAttribute("data-eii-grid-page-length") || "25", 10),
            language: lang,
            order: [],   // por defecto respeta el orden que mandó el servidor
            columnDefs: noSort.length ? [{ targets: noSort, orderable: false }] : []
        };

        var order = table.getAttribute("data-eii-grid-order"); // "0:asc"
        if (order) {
            var parts = order.split(":");
            var col = parseInt(parts[0], 10);
            if (!isNaN(col)) opts.order = [[col, parts[1] === "desc" ? "desc" : "asc"]];
        }

        // Punto de extensión: server-side para cuando una tabla grande adopte el
        // componente. Hoy NINGUNA página lo usa — los catálogos son de ≤57 filas.
        var serverUrl = table.getAttribute("data-eii-grid-server");
        if (serverUrl) {
            opts.serverSide = true;
            opts.processing = true;
            opts.ajax = { url: serverUrl, type: "POST" };
        }

        $(table).DataTable(opts);
    }

    /* ---------- Píldora "Se guardaron cambios · Actualizar" ---------- */
    /* Markup con lo que ya existe: alert de Bootstrap (el mismo idioma que los
       alert-success/-danger de estas páginas) + .eii-btn. Cero CSS nuevo. */
    function mostrarPildora() {
        if (document.querySelector("[data-eii-grid-aviso]")) return; // ya está puesta

        var ancla = document.querySelector(".eii-admin-grid-wrap") ||
                    document.querySelector("[data-eii-grid]");
        if (!ancla || !ancla.parentNode) return;

        var aviso = document.createElement("div");
        aviso.className = "alert alert-info d-flex align-items-center gap-2 py-2";
        aviso.setAttribute("role", "status");
        aviso.setAttribute("data-eii-grid-aviso", "");
        // HTML estático: nada de lo que llega por el canal se interpola aquí.
        aviso.innerHTML =
            '<i class="bi bi-arrow-clockwise" aria-hidden="true"></i>' +
            '<span>Se guardaron cambios en otra pestaña.</span>' +
            '<button type="button" class="eii-btn eii-btn--primary eii-btn--sm ms-2" data-eii-grid-recargar>Actualizar</button>' +
            '<button type="button" class="btn-close ms-auto" aria-label="Descartar" data-eii-grid-descartar></button>';

        ancla.parentNode.insertBefore(aviso, ancla);

        aviso.querySelector("[data-eii-grid-recargar]").addEventListener("click", function () {
            window.location.reload();
        });
        aviso.querySelector("[data-eii-grid-descartar]").addEventListener("click", function () {
            aviso.remove();
        });
    }

    /* Escucha los avisos de guardado. Guardarraíl: solo reaccionamos a EVENTOS en
       vivo — nunca leemos el valor guardado al cargar. Así la píldora aparece si y
       solo si hubo un guardado desde que se abrió esta pestaña. */
    function initEscuchaGuardado() {
        try {
            if ("BroadcastChannel" in window) {
                var bc = new BroadcastChannel(CANAL);
                bc.onmessage = function (e) {
                    if (e && e.data && e.data.tipo === "guardado") mostrarPildora();
                };
            }
        } catch (e) { /* sin canal: queda el respaldo de storage */ }

        // Respaldo: 'storage' se dispara en las OTRAS pestañas cuando una escribe.
        window.addEventListener("storage", function (e) {
            if (e.key === LS_KEY && e.newValue) mostrarPildora();
        });
    }

    /* ---------- Aviso desde la pestaña de edición ---------- */
    function initAvisoGuardado(el) {
        var msg = { tipo: "guardado", entidad: el.getAttribute("data-eii-grid-notify") || "" };

        try {
            if ("BroadcastChannel" in window) {
                // No lo cerramos: cerrar en el mismo tick puede tragarse el mensaje.
                // La pestaña lo libera al descargarse.
                new BroadcastChannel(CANAL).postMessage(msg);
                return;
            }
        } catch (e) { /* cae al respaldo */ }

        try {
            localStorage.setItem(LS_KEY, JSON.stringify({ t: new Date().getTime(), entidad: msg.entidad }));
        } catch (e) { /* sin storage no hay aviso; el guardado ya ocurrió igual */ }
    }

    function init() {
        var grids = document.querySelectorAll("[data-eii-grid]");
        grids.forEach(initGrid);
        if (grids.length) initEscuchaGuardado();

        document.querySelectorAll("[data-eii-grid-notify]").forEach(initAvisoGuardado);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
