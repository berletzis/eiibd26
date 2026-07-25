# REQ — NINA: agregar Fundación MAS VIDA al crawler

**Fecha:** 24 JUL 2026
**Proyecto:** `NINA-WorkerService` (crawler). Formalmente fuera del scope de `eiibd26.Web`, pero es config de fuentes que el usuario dirige (como Educa/CCF) — excepción aceptada. **No tocar lógica de NINA, solo `fuentes.json` + su scope.**
**Fuente:** Fundación MAS VIDA — `masvida.org.ar`. ONG argentina dedicada por completo a la EII (qué es la EII, preguntas y respuestas, novedades, información). En **español**. Tier fundación de pacientes (como Educa Inflamatoria / funeiico).

## Verificado
- **robots.txt vacío / sin restricciones** → crawleable. La home responde 200 y el contenido es accesible.
- **Idioma español** (argentino) — encaja con la audiencia LATAM, sin el problema de idioma de Mayo.
- **Sitio chico y 100% EII** → no necesita allowlist quirúrgico; crawl amplio + denylist ligera.
- Es WordPress. El seed que dio el usuario es `https://masvida.org.ar/sitemap.xml`. **Confirmar la URL real del sitemap desde el entorno del crawler** (mi herramienta no renderiza el XML); si `/sitemap.xml` no trae URLs, probar `/sitemap_index.xml` o `/wp-sitemap.xml` (variantes de plugins WP).

## fuentes.json
- Agregar MAS VIDA con `activo: true`, `idioma: "es"`.
- **Seed:** `https://masvida.org.ar/sitemap.xml` (con las variantes de arriba como fallback).
- **Scope:** crawl amplio del contenido de EII. NO hace falta allowlist estrecho (todo el sitio es EII).
- **Denylist (exclusionesUrl):** rutas no-contenido y de sistema — `/wp-admin`, `/wp-login`, `/wp-json`, `/feed`, `/donar`, `/donac`, `/contacto`, `/carrito`, `/checkout`, política de privacidad/legal, y formularios. (robots está vacío, así que estos se ponen explícitos.)

## Reglas que no cambian
- Respetar robots.txt (aunque esté vacío).
- Guardar **significado + link** (embedding + URL). **Nunca republicar** el contenido.

## Después de indexar (lado web)
- Agregar `masvida.org.ar` a `DominiosConfiables` en appsettings → recién ahí la recuperación de referencias (Fase 1) ofrece candidatos de MAS VIDA.

## Runtime (tuyo)
- Correr el worker NINA con la Voyage key contra prod para indexar. Hasta entonces, configurada pero sin indexar.

## Verificación
1. Smoke test: crawlear 2-3 URLs de contenido conocidas (qué es la EII, preguntas y respuestas, una novedad) → confirmar que indexan con embedding.
2. Confirmar que una URL de sistema/no-contenido (`/wp-admin`, `/contacto`, `/donar`) queda excluida.
3. Generar la nota de un ingrediente/término EII-relevante → confirmar que puede aparecer un link real de MAS VIDA como candidato (una vez en `DominiosConfiables`).
