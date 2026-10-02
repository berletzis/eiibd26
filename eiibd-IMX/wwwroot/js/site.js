/* eiibd-IMX — sin dependencias.
   Las cards ya vienen en el HTML: este archivo solo agrega el modal,
   el "cargar mas" y el estado activo del menu. Si el JS falla, cada card
   sigue teniendo su enlace real al articulo. */
(function () {
  "use strict";

  var backdrop = document.getElementById("backdrop");
  var body     = document.getElementById("modal-body");
  var catHost  = document.getElementById("modal-cat-host");
  var lastFocus = null;

  function abrir(cat, slug) {
    lastFocus = document.activeElement;
    body.innerHTML = '<p class="modal-meta">Cargando…</p>';
    catHost.textContent = "";
    backdrop.setAttribute("data-open", "true");
    document.body.style.overflow = "hidden";
    document.getElementById("modal-close").focus();

    fetch("/api/articulo/" + encodeURIComponent(cat) + "/" + encodeURIComponent(slug),
          { headers: { "X-Requested-With": "fetch" } })
      .then(function (r) {
        if (!r.ok) throw new Error(r.status);
        return r.text();
      })
      .then(function (html) {
        body.innerHTML = html;
        var c = body.querySelector("[data-cat]");
        if (c) catHost.textContent = c.textContent;
        var t = body.querySelector("[data-titulo]");
        if (t) t.id = "modal-title";
        body.scrollIntoView({ block: "start" });
      })
      .catch(function () {
        // Si el fragmento falla, no dejamos al usuario en un modal vacio.
        window.location.href = "/" + cat + "/" + slug;
      });
  }

  function cerrar() {
    backdrop.setAttribute("data-open", "false");
    document.body.style.overflow = "";
    body.innerHTML = "";
    if (lastFocus) lastFocus.focus();
  }

  /* ── Eventos de conversion ────────────────────────────────────────────────
     Ad Grants exige al menos una conversion al mes y Smart Bidding obligatorio,
     que sin conversiones no funciona. Estos cuatro eventos son esa medicion, y
     se apoyan en elementos que YA existen: no agregan marcado.

     Todo va en try/catch: si un bloqueador impide cargar gtag, la pagina
     tiene que seguir funcionando igual. La analitica nunca puede romper la
     navegacion.

     No se usa beforeunload ni se retrasa la navegacion para "asegurar" el
     envio: GA4 usa sendBeacon y sobrevive al cambio de pagina por si solo. */
  function evento(nombre, params) {
    try {
      if (typeof gtag === "function") gtag("event", nombre, params || {});
    } catch (e) { /* silencio deliberado */ }
  }

  /* Listener independiente del que maneja el modal y el slider: si algo falla
     midiendo, no debe arrastrar al resto de la interfaz. */
  document.addEventListener("click", function (e) {
    var a = e.target.closest("a");
    if (!a) return;
    var href = a.getAttribute("href") || "";

    if (href.indexOf("tel:") === 0) {
      evento("click_telefono", { destino: href.slice(4) });
    } else if (href.indexOf("mailto:") === 0) {
      evento("click_correo", { destino: href.slice(7) });
    } else if (a.classList.contains("cta-registro")) {
      // La misma tarjeta vive en el footer y en el sidebar del articulo. Sin
      // este parametro los dos clics son indistinguibles y no hay forma de
      // saber si el sidebar justifica su espacio.
      // Ojo: depende de los nombres de clase .side y .site-footer. Si se
      // renombran, el evento sigue llegando pero con ubicacion "otra".
      evento("click_registro", {
        ubicacion: a.closest(".side") ? "sidebar_articulo"
                 : a.closest(".site-footer") ? "footer"
                 : "otra"
      });
    } else if (a.classList.contains("panel-cta")) {
      evento("click_donativo");
    }
  });

  document.addEventListener("click", function (e) {
    var abrirBtn = e.target.closest(".card-open");
    if (abrirBtn) { abrir(abrirBtn.dataset.cat, abrirBtn.dataset.slug); return; }

    if (e.target.closest("#modal-close") || e.target === backdrop) { cerrar(); return; }

    var mas = e.target.closest(".mas");
    if (mas) {
      var cat = mas.dataset.cat;
      var skip = parseInt(mas.dataset.skip, 10) || 10;
      mas.disabled = true;
      mas.textContent = "Cargando…";
      fetch("/api/mas/" + encodeURIComponent(cat) + "?skip=" + skip + "&take=10")
        .then(function (r) { return r.text(); })
        .then(function (html) {
          var cont = document.querySelector('.cards[data-cat="' + cat + '"]');
          if (!html.trim()) { mas.remove(); return; }
          cont.insertAdjacentHTML("beforeend", html);
          mas.dataset.skip = skip + 10;
          mas.disabled = false;
          mas.textContent = "Cargar más";
        })
        .catch(function () { mas.remove(); });
    }
  });

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && backdrop.getAttribute("data-open") === "true") cerrar();
  });

  // Estado activo del menu de anclas.
  var links = Array.prototype.slice.call(document.querySelectorAll(".nav a"));
  var secciones = document.querySelectorAll("section[id]");
  if ("IntersectionObserver" in window && secciones.length) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (!en.isIntersecting) return;
        links.forEach(function (l) { l.removeAttribute("aria-current"); });
        var m = links.filter(function (l) {
          return (l.getAttribute("href") || "").endsWith("#" + en.target.id);
        })[0];
        if (m) m.setAttribute("aria-current", "true");
      });
    }, { rootMargin: "-72px 0px -65% 0px" });
    secciones.forEach(function (s) { io.observe(s); });
  }
  /* ── Slider vertical ─────────────────────────────────────────────────────
     Solo desplaza: los cinco paneles ya estan en el HTML. Bajo 980px el CSS
     los apila y este bloque no hace nada. */
  var track = document.getElementById("track");
  if (track) {
    var viewport = track.parentNode;
    var slides   = Array.prototype.slice.call(track.children);
    var total    = slides.length;
    var idx      = 0;
    var rail     = Array.prototype.slice.call(document.querySelectorAll(".rail-item"));
    var prev     = document.getElementById("slide-prev");
    var next     = document.getElementById("slide-next");
    var cuenta   = document.getElementById("slide-count");

    function apilado() { return window.matchMedia("(max-width: 980px)").matches; }

    // El viewport se ajusta al panel ACTUAL, no al mas alto: si no, los paneles
    // cortos arrastran el vacio del mas largo. La altura se anima junto con el
    // desplazamiento, asi que el cambio se lee como una sola transicion.
    function medir() {
      if (apilado()) { viewport.style.height = ""; track.style.transform = ""; return; }
      viewport.style.height = Math.ceil(slides[idx].getBoundingClientRect().height) + "px";
      posicionar();
    }

    // Desplazamiento en pixeles sobre la posicion real del panel dentro del
    // track. En porcentaje NO funciona: el % de translateY se calcula sobre la
    // altura del propio track (la suma de los cinco paneles), no la de uno.
    function posicionar() {
      track.style.transform = "translateY(" + (-slides[idx].offsetTop) + "px)";
    }

    function ir(n) {
      idx = Math.max(0, Math.min(total - 1, n));
      rail.forEach(function (b, i) {
        if (i === idx) b.setAttribute("aria-current", "true");
        else b.removeAttribute("aria-current");
      });
      if (cuenta) cuenta.textContent = (idx + 1) + " / " + total;
      if (prev) prev.disabled = idx === 0;
      if (next) next.disabled = idx === total - 1;
      if (!apilado()) medir();
    }

    rail.forEach(function (b) {
      b.addEventListener("click", function () { ir(parseInt(b.dataset.go, 10) || 0); });
    });
    if (prev) prev.addEventListener("click", function () { ir(idx - 1); });
    if (next) next.addEventListener("click", function () { ir(idx + 1); });

    // Flechas del teclado solo con el foco dentro del slider: secuestrarlas en
    // toda la pagina seria hostil para quien navega sin mouse.
    track.closest(".slider").addEventListener("keydown", function (e) {
      if (e.key === "ArrowDown") { e.preventDefault(); ir(idx + 1); }
      if (e.key === "ArrowUp")   { e.preventDefault(); ir(idx - 1); }
    });

    // Al llegar desde otra pagina con un ancla (/#programas), el navegador salta
    // ANTES de que corra medir(). Como medir() colapsa el slider de los cinco
    // paneles apilados (~2700px) a la altura de uno solo, la pagina se acorta
    // ~2100px despues del salto y el scroll queda apuntando a ninguna parte.
    // Rehacemos el salto cuando la altura ya es la definitiva.
    function reubicarAncla() {
      if (!location.hash || location.hash.length < 2) return;
      var destino = document.getElementById(location.hash.slice(1));
      if (!destino) return;
      requestAnimationFrame(function () {
        destino.scrollIntoView({ block: "start", behavior: "auto" });
      });
    }

    var pendiente;
    window.addEventListener("resize", function () {
      clearTimeout(pendiente);
      pendiente = setTimeout(medir, 120);
    });

    // Las fuentes web cambian la altura al cargar: hay que volver a medir.
    if (document.fonts && document.fonts.ready) {
      document.fonts.ready.then(function () { medir(); reubicarAncla(); });
    }
    window.addEventListener("load", function () { medir(); reubicarAncla(); });

    ir(0);
    medir();
    reubicarAncla();
  }
})();
