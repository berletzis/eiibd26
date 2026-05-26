# Bloque 1 – Catálogos Públicos (SEC-001, SEC-002, SEC-003)

## Análisis de uso real

### ¿Son públicos estos endpoints?

| Controlador | Endpoint | UI que lo consume | Requiere login en UI |
|---|---|---|---|
| `SintomasApiController` | `GET /api/sintomas/autocomplete` | Formulario "Registro de síntoma" (Mi Salud) + Registro de estado de ánimo | Sí — solo usuarios autenticados acceden a esas páginas |
| `TratamientosApiController` | `GET /api/tratamientos/autocomplete` | Formulario "Mis tratamientos" (Mi Salud) | Sí — solo usuarios autenticados |
| `CondicionesApiController` | `GET /api/condiciones/autocomplete` | Formulario "Mis condiciones" + Registro médico | Sí — solo usuarios autenticados |

### Decisión

Los tres catálogos son consumidos **exclusivamente desde páginas que requieren login**. Sin embargo, los datos en sí (nombres de síntomas, tratamientos, condiciones EII como Crohn/Colitis) son información médica genérica, no datos de pacientes. En el contexto de una plataforma de salud para EII, esta información es considerada **pública educativa** (similar a cualquier enciclopedia médica).

**Decisión:** Declarar `[AllowAnonymous]` explícito con comentario justificativo + agregar rate limiting en los endpoints de autocomplete para prevenir scraping masivo.

**Justificación documentada:** Los catálogos no contienen datos de pacientes, solo taxonomía médica genérica de EII. El acceso anónimo es intencional para soportar potencial uso futuro en páginas públicas. La intencionalidad queda explícita en código.

## Implementación

### [AllowAnonymous] + comentario
Agregado en `SintomasApiController`, `TratamientosApiController`, `CondicionesApiController`.

### Rate Limiting
Implementado con `Microsoft.AspNetCore.RateLimiting` (nativo .NET 8+, sin paquete adicional).

Política aplicada: `"catalogos-autocomplete"`
- Ventana fija: **30 requests / 60 segundos por IP**
- Status 429 con mensaje en español al superar límite
- Atributo `[EnableRateLimiting("catalogos-autocomplete")]` en cada método de autocomplete

Registrado en `Program.cs` antes del pipeline.

## Estado

| Issue | Estado |
|---|---|
| SEC-001 | ✅ RESUELTO — `[AllowAnonymous]` explícito + rate limiting |
| SEC-002 | ✅ RESUELTO — `[AllowAnonymous]` explícito + rate limiting |
| SEC-003 | ✅ RESUELTO — `[AllowAnonymous]` explícito + rate limiting |
