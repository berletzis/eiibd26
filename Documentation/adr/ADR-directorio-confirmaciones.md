# ADR-002: Fuente Canónica de Confirmaciones del Directorio Médico

**Estado:** Aceptado  
**Fecha:** 2025-07  
**Issues relacionados:** FUNC-023

---

## Contexto

El sistema tenía dos flujos de confirmación paralelos con lógicas de cálculo distintas:

| Flujo | Tabla | Dónde se usaba | Lógica NivelConfianza |
|---|---|---|---|
| `ConfirmarAtencionAsync` | `ConfirmacionComunitaria` | `MedicoDirectorioService` | Pacientes únicos; escala lineal |
| `OnPostConfirmarSimpleAsync` | `DirectorioMedicoConfirmacion` | `Detalle.cshtml.cs` (inline) | Total + áreas EII + cédula + reclamo |
| `RecalcularNivelAsync` (privado) | `DirectorioMedicoConfirmacion` | `Detalle.cshtml.cs` | Fórmula con 4 factores |
| `RecalcularNivelConfianzaAsync` | `ConfirmacionComunitaria` | Servicio público | Pacientes únicos; escala lineal |

Consecuencias del estado anterior:
- El nivel de confianza calculado dependía de qué flujo lo había disparado.
- Admin, Dashboard y Badges leían `DirectorioMedicoConfirmaciones` pero el recálculo del servicio usaba `ConfirmacionesComunitarias`.
- `Activar.cshtml.cs` (reclamo de token) calculaba nivel de confianza con una tercera lógica distinta.

---

## Decisión

**`DirectorioMedicoConfirmacion` es la única fuente de verdad para confirmaciones y cálculo de NivelConfianza.**

Razones:
1. Admin, Dashboard y Badges ya la consumían; era la tabla activa de facto.
2. Contiene datos ricos (10 áreas EII específicas) vs. la tabla vieja (solo tipo genérico).
3. Es la que los usuarios crean al confirmar médicos en `Detalle.cshtml.cs`.

`ConfirmacionComunitaria` se mantiene en el modelo para preservar datos históricos y para `ConfirmacionesAgregadas` (confirmaciones por tipo/rol comunitario — semántica diferente a las de experiencia EII).

---

## Fórmula canónica de NivelConfianza

Implementada en `MedicoDirectorioService.CalcularNivelVerificacion()`:

```
PerfilReclamado              → Establecido (3)
CedulaVerificada || total>=5 → Reconocido  (2)
total>=3 && tieneEII         → Confirmado  (1)
default                      → Identificado (0)
```

---

## Cambios aplicados

| Archivo | Cambio |
|---|---|
| `MedicoDirectorioService.cs` | `RecalcularNivelConfianzaAsync` usa `DirectorioMedicoConfirmaciones` + fórmula canónica |
| `MedicoDirectorioService.cs` | `GetListadoAsync` / `GetDetalleAsync` — `TotalConfirmaciones` desde tabla canónica |
| `Detalle.cshtml.cs` | Eliminados `RecalcularNivelAsync` y `CalcularNivelVerificacion` privados |
| `Detalle.cshtml.cs` | `OnPostConfirmarSimpleAsync` delega a `_service.RecalcularNivelConfianzaAsync` |
| `Activar.cshtml.cs` | Inyectado `IMedicoDirectorioService`; recálculo en reclamo de token usa servicio canónico |

---

## Consecuencias

### Positivas
- Un único punto de cálculo; cualquier cambio a la fórmula se propaga a todos los flujos.
- Coherencia entre lo que el admin/dashboard muestran y lo que el servicio calcula.
- El reclamo de token (Activar) eleva correctamente el nivel porque `PerfilReclamado = true` tras `SaveChangesAsync`.

### Consideraciones
- `ConfirmacionComunitaria` sigue existiendo para `ConfirmacionesAgregadas` (vista pública, tipos como "atendido", "recomendado"). No se elimina pero tampoco afecta NivelConfianza.
- Si en el futuro se decide migrar `ConfirmacionesAgregadas` a `DirectorioMedicoConfirmacion`, se requiere un ADR adicional y script SQL de migración de datos.
