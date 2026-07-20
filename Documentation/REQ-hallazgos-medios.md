# REQ — Hallazgos Medios (auditoría Fable, 15 JUL 2026)

**Triage MVP:** hacer los baratos y claros ahora, uno es decisión del owner, uno se difiere como deuda consciente. Reglas del repo de siempre (sin migraciones EF, `string?`/`DateTime?` en handlers, rebuild en VS para `.cs`, diff antes de aplicar, no tocar worker/Conectar3eros).

---

## Hacer ahora — código (Claude Code)

### M-1 · Handlers con params no-nullable (regla `string?`)
Mismo bug de 400 que ya conoces. Pasar a `string?` + validación explícita al inicio del handler. **Confirmar cada línea contra el código antes de cambiar** (números de la auditoría, pueden haber corrido):
- `Admin/Platillos/NotaClinicaDetalle.cshtml.cs:161,171,180` — `string tipo` (módulo nuevo).
- `Usuario/UsuarioSintomas.cshtml.cs:244` — `string estado`.
- `Usuario/UsuarioSintomasSeguimiento.cshtml.cs:156` — `string estado, string fecha`.
- `Medico/Dashboard.cshtml.cs:190` — `string tituloSolicitud`.
- `Admin/ShortUrls/Index.cshtml.cs:41` — `string urlDestino`.
- `Admin/DirectorioMedicos/Index.cshtml.cs:404,427` — `string codigo`.
Riesgo bajo, mecánico. Probar que cada handler afectado acepta el caso "vacío" sin 400.

### M-7 · Sync-over-async en `OnGet` (`Admin/Contenidos/ContenidosCategorias.cshtml.cs:37-40`)
Verificado: `OnGet()` hace `LoadSelectList().GetAwaiter().GetResult()` (bloquea thread pool, riesgo de deadlock bajo carga). Cambiar a `OnGetAsync` con `await`:
```csharp
public async Task OnGetAsync()
{
    await LoadSelectList();
}
```
Riesgo bajo.

### M-5a · Query a BD en CADA request autenticado (`Program.cs:907-943`) — HECHO
Verificado: `db.Users.AnyAsync(...)` por request contra SQL remoto. El `SecurityStampValidator` (`:129-132`) ya corta sesiones de usuarios eliminados/suspendidos en ≤2 min. **NO borrar el middleware** (defiende del error "Unable to load user"); se **cachea** el chequeo: `IMemoryCache` por `userId`, 3 min (coherente con el SecurityStampValidator). Aplicado; falta prueba en vivo (usuario eliminado sigue cortándose en ≤3 min).

### M-5b · Caché de slugs del middleware SEO (`:714-897`) — DIFERIDO (deuda consciente)
**Reclasificado.** Ese middleware resuelve categorías/hijas/contenidos y emite `Redirect(..., permanent: true)` — 301s que cachean navegador y Google, irreversibles con un revert. Cachear los slugs cambia la conducta pública (una categoría nueva no rutea hasta 5 min) y `CLAUDE.md` prohíbe explícitamente cambiar rutas públicas por SEO. La ganancia (evitar un query en paths no reconocidos) no justifica el riesgo en un MVP. Si algún día se hace: medir primero, e **invalidar la caché al crear/editar categoría**, no un TTL ciego. Por ahora, deuda consciente.

### M-3 · Antiforgery inconsistente en API controllers con cookie — SU PROPIO CICLO
Uniformar con `[AutoValidateAntiforgeryToken]` en los controllers API que usan cookie (`PreguntasApiController`, `PlatCalificacionesApiController`). **OJO:** si un POST hoy NO valida y su JS no manda el token, agregar el atributo lo **rompe**. Va **después del baseline commiteado**, en su propio ciclo: verificar endpoint por endpoint que el JS mande el token (o agregarlo), y **probar cada POST**. Riesgo hoy bajo (`SameSite=Lax` mitiga); si un endpoint se enreda, **anótalo en vez de romperlo**.

---

## Hacer ahora — SQL (owner)

### M-2 · Datos de prueba en producción (A-01 del backlog)
Correr el SQL ya redactado en `Documentation/TODO_SIGUIENTE_SESION.md` (sección A-01): el `SELECT` para verificar, luego `UPDATE GlossaryValidations SET Approved = 0 WHERE Comment IN ('validar descripcion usuario prueba','xxxx')`. Ya no se ven en público (filtro por badge de médico verificado, confirmado), pero limpia métricas y PII latente. SQL directo, sin migración.

---

## Decisión del owner

### M-4 · Ratings anónimos sin límite (spam de métricas)
`ArticleRatingsApiController:87` y `GlossaryRatingsApiController:75` aceptan ratings anónimos sin rate-limit → un loop de `curl` infla/deforma los ratings de contenido médico. Dos caminos, es llamada de producto:
- **(a) Exigir sesión para calificar** (como ya hace `PlatCalificacionesApiController`). Simple, mata el vector. Se pierden los ratings de visitantes anónimos.
- **(b) Mantener anónimo + rate-limit por IP + dedupe por cookie.** Conserva ratings de visitantes, más trabajo.

Pendiente de decisión antes de tocar.

---

## Diferir — deuda consciente (MVP)

### M-6 · CSP con `unsafe-inline` / `unsafe-eval` (`Program.cs:540`)
Endurecer la CSP de verdad es un refactor grande (handlers inline por todo el sitio) con riesgo real de regresión; Fable mismo lo puso Medio por eso. Para un MVP es deuda consciente aceptable. **Experimento barato opcional:** quitar SOLO `unsafe-eval` y smoke-test TinyMCE/Chart.js; si nada truena, es mejora gratis. Lo demás (`unsafe-inline` → nonces) queda para el rewrite.

---

## Resumen

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| M-1 | Handlers `string?` (6 puntos) | Claude Code | hecho, sin probar |
| M-7 | Sync-over-async → `OnGetAsync` | Claude Code | hecho, sin probar |
| M-5a | Cachear chequeo user-exists (3 min) | Claude Code | hecho, sin probar |
| M-2 | Limpiar datos de prueba en prod | Owner (SQL) | ahora |
| M-3 | Antiforgery uniforme (prueba por endpoint) | Claude Code | su propio ciclo, tras baseline |
| M-4 | Ratings anónimos: sesión vs rate-limit | Owner (decide) | pendiente |
| M-5b | Caché slugs SEO (301s) | — | diferido (deuda consciente) |
| M-6 | Endurecer CSP | — | diferido (deuda consciente) |
