# REQ — NINA: agregar CCF al crawler (solo artículos / educación de pacientes)

**Fecha:** 22 JUL 2026
**Proyecto:** `NINA-WorkerService` (crawler). Formalmente fuera del scope de `eiibd26.Web`, pero es config de fuentes del crawler que el usuario dirige (como Educa) — excepción aceptada. **No tocar lógica de NINA, solo `fuentes.json` + su scope.**
**Fuente:** Crohn's & Colitis Foundation — `crohnscolitisfoundation.org`. Tier-1, robots verificado (permite contenido general).

## Objetivo
Indexar SOLO los **artículos y contenido educativo de pacientes** de CCF. No interesa ningún otro tipo de contenido (eventos, donaciones, tienda, capítulos locales, voluntariado, portales profesionales/investigación, formularios).

## fuentes.json
- Agregar CCF con `activo: true`.
- **Seed:** `https://www.crohnscolitisfoundation.org/sitemap.xml` (Drupal XML Sitemap, índice paginado `?page=N` — el crawler sigue la paginación).
- CCF **no tiene blog separado**: el contenido editorial vive bajo `/patientsandcaregivers/...` y páginas educativas de enfermedad. Es un solo sitemap.

## Regla de scope — quedarse con lo importante, descartar el resto
**Incluir (artículos / educación de pacientes):**
- `/patientsandcaregivers/*` (qué es la enfermedad, síntomas, dieta y nutrición, tratamiento/medicación, vivir con EII).
- Páginas educativas tipo `/what-is-crohns-disease`, `/what-is-ulcerative-colitis` (redirigen a la sección de pacientes).

**Excluir (denylist):**
- Por robots: `/gutfriendlyrecipes-list/recipe/*`, `/admin/`, `/user/*`, `/search/`, `/node/add/`, `/comment/`, `/core/`, `/profiles/`, `/temp/*`.
- Por intención (no es artículo): eventos, fundraising/donaciones, tienda, capítulos/regiones locales, voluntariado, ensayos clínicos-community, portales profesionales/investigación, formularios y landings de campaña.

**Paso concreto:** enumerar los sub-sitemaps (`/sitemap.xml?page=0,1,2…`), quedarse con los que traen artículos educativos y descartar los demás. Si un sub-sitemap mezcla, aplicar el filtro de patrones de arriba.

## Reglas que no cambian
- Respetar robots.txt (siempre).
- Guardar **significado + link** (embedding + URL). **Nunca republicar** el contenido de CCF.

## Después de indexar (lado web)
- Agregar `crohnscolitisfoundation.org` a `DominiosConfiables` en appsettings (verificar si ya está; probablemente no, porque hasta ahora no estaba crawleado). Recién ahí la recuperación (Fase 1) ofrece candidatos reales de CCF.

## Verificación
1. Smoke test: crawlear 2-3 URLs conocidas (`/patientsandcaregivers/what-is-crohns-disease`, una de dieta/nutrición, una de tratamiento) → confirmar que **indexan con embedding** y que su scope las acepta.
2. Confirmar que una URL de receta (`/gutfriendlyrecipes-list/recipe/...`) o de eventos/donación **queda excluida**.
3. Generar la nota de un ingrediente con contenido CCF-relevante → confirmar que aparece un **link real de CCF** como candidato en el panel de sugerencias.
