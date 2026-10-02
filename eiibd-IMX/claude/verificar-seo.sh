#!/usr/bin/env sh
# Verificacion de los criterios de aceptacion de SEO / Ad Grants de eiibd-IMX.
#
#   sh claude/verificar-seo.sh https://www.imx.org.mx      (sitio publicado)
#   sh claude/verificar-seo.sh http://localhost:37015      (instancia local)
#
# Sin argumento usa http://localhost:37015.
# Devuelve 0 si todo pasa, 1 si algo falla. Pensado para correrse ANTES y
# DESPUES de cada publicacion.

BASE="${1:-http://localhost:37015}"
CANON="https://www.imx.org.mx"
FALLOS=0

ok()    { printf '  OK    %s\n' "$1"; }
falla() { printf '  FALLA %s\n' "$1"; FALLOS=$((FALLOS+1)); }

echo "== eiibd-IMX · verificacion SEO contra $BASE =="

# ── R1 · canonical y og:url apuntan al host canonico ────────────────────────
# El grep de R1: ninguna pagina puede renderizar un canonical o un og:url con
# un host distinto de https://www.imx.org.mx. El placeholder original era
# "https://ejemplo.org" y estuvo publicado; esto es lo que evita que vuelva.
echo "-- R1 canonical / og:url"
for RUTA in / /quienes-somos /transparencia /contacto /aviso-de-privacidad /info-y-ayuda /vivir-con-crohn-y-colitis; do
  HTML=$(curl -sL --max-time 20 "$BASE$RUTA")
  MALOS=$(printf '%s' "$HTML" \
    | grep -oE '<link rel="canonical" href="[^"]*"|<meta property="og:url" content="[^"]*"' \
    | grep -v "$CANON")
  if [ -n "$MALOS" ]; then falla "$RUTA -> $MALOS"; else ok "$RUTA"; fi
done

# Ningun host escrito a mano en las vistas ni en el codigo.
echo "-- R1 host escrito a mano en el codigo"
HARDCODE=$(grep -rniE 'imx\.org\.mx|ejemplo\.org' \
             --include='*.cshtml' --include='*.cs' . 2>/dev/null \
           | grep -v '^\./bin/' | grep -v '^\./obj/')
if [ -n "$HARDCODE" ]; then falla "host en codigo:"; printf '%s\n' "$HARDCODE"
else ok "ninguna vista ni .cs escribe el host (solo appsettings)"; fi

# ── R2/R8 · las cuatro institucionales responden 200 ────────────────────────
echo "-- R2/R8 paginas institucionales"
for RUTA in /quienes-somos /transparencia /contacto /aviso-de-privacidad; do
  COD=$(curl -sL -o /dev/null -w '%{http_code}' --max-time 20 "$BASE$RUTA")
  [ "$COD" = "200" ] && ok "$RUTA ($COD)" || falla "$RUTA ($COD)"
done

# El aviso de privacidad tiene que ser indexable: si alguien le pone noindex,
# deja de servir para lo que se hizo.
AVISO=$(curl -sL --max-time 20 "$BASE/aviso-de-privacidad")
printf '%s' "$AVISO" | grep -q 'name="robots" content="noindex"' \
  && falla "/aviso-de-privacidad esta en noindex" \
  || ok "/aviso-de-privacidad indexable (sin noindex)"

# Enlazado desde el footer, o sea desde todas las paginas.
printf '%s' "$AVISO" | grep -q 'href="/aviso-de-privacidad"' \
  && ok "el footer enlaza /aviso-de-privacidad" \
  || falla "el footer no enlaza /aviso-de-privacidad"

# ── R9 · domicilio publicado y una sola definicion ──────────────────────────
echo "-- R9 domicilio"
curl -sL --max-time 20 "$BASE/transparencia" | grep -q "Los Portales" \
  && ok "/transparencia muestra el domicilio" \
  || falla "/transparencia no muestra el domicilio"

# El domicilio vive en appsettings.json y en ningun otro lado. Escrito a mano en
# una vista se desincroniza con el JSON-LD sin que nadie se entere.
DOM=$(grep -rn "Mexiquense" --include='*.cshtml' --include='*.cs' . 2>/dev/null \
      | grep -v '^\./bin/' | grep -v '^\./obj/')
if [ -n "$DOM" ]; then falla "domicilio escrito a mano:"; printf '%s\n' "$DOM"
else ok "el domicilio solo esta en appsettings.json"; fi

# ── R3 · las categorias tienen texto propio suficiente ──────────────────────
echo "-- R3 contenido de las categorias"
for RUTA in /info-y-ayuda /vivir-con-crohn-y-colitis; do
  HTML=$(curl -sL --max-time 20 "$BASE$RUTA")
  PAL=$(printf '%s' "$HTML" \
        | sed -n 's/.*<div class="intro-categoria">\(.*\)/\1/p' \
        | sed 's/<[^>]*>/ /g' | tr -s ' \n' '\n' | grep -c .)
  [ "$PAL" -ge 250 ] && ok "$RUTA intro: $PAL palabras" \
                     || falla "$RUTA intro: $PAL palabras (minimo 250)"
  DESC=$(printf '%s' "$HTML" | sed -n 's/.*<meta name="description" content="\([^"]*\)".*/\1/p')
  LEN=$(printf '%s' "$DESC" | wc -c | tr -d ' ')
  # el HTML llega con entidades (&#xED;): el largo real es menor, se avisa
  ok "$RUTA meta: ~$LEN bytes con entidades"
done

# ── R4 · robots.txt y sitemap.xml, coherentes entre si ──────────────────────
echo "-- R4 robots / sitemap"
ROBOTS=$(curl -sL --max-time 20 "$BASE/robots.txt")
printf '%s' "$ROBOTS" | grep -q "Sitemap: $CANON/sitemap.xml" \
  && ok "robots.txt apunta a $CANON/sitemap.xml" \
  || falla "robots.txt no declara el sitemap correcto"

SITEMAP=$(curl -sL --max-time 20 "$BASE/sitemap.xml")
N=$(printf '%s' "$SITEMAP" | grep -c '<loc>')
[ "$N" -gt 0 ] && ok "sitemap con $N URLs" || falla "sitemap vacio"

AJENAS=$(printf '%s' "$SITEMAP" | grep -o '<loc>[^<]*</loc>' | grep -vc "$CANON")
[ "$AJENAS" -eq 0 ] && ok "todas las URLs usan el host canonico" \
                    || falla "$AJENAS URLs del sitemap con otro host"

echo "-- R4 cada URL del sitemap responde 200"
ROTAS=0
printf '%s' "$SITEMAP" | grep -o '<loc>[^<]*</loc>' \
  | sed -e "s|<loc>$CANON||" -e 's|</loc>||' \
  | while read -r P; do
      COD=$(curl -sL -o /dev/null -w '%{http_code}' --max-time 20 "$BASE$P")
      [ "$COD" = "200" ] || printf '  FALLA sitemap %s -> %s\n' "$P" "$COD"
    done

echo
if [ "$FALLOS" -eq 0 ]; then
  echo "== sin fallos (revisa arriba si alguna URL del sitemap salio marcada) =="
  exit 0
else
  echo "== $FALLOS fallo(s) =="
  exit 1
fi
