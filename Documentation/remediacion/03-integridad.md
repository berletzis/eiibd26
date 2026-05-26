# 03 - Integridad de Dominio

## FUNC-022: Reclamo de perfil médico no persistía estado ni confianza

### Problema
Al usar un token de reclamo válido, `EstatusReclamacion` permanecía en `NoReclamado` y `NivelConfianza = 0` porque `VincularAsync()` no actualizaba el `MedicoDirectorio`.

### Causa raíz
La lógica de actualización de `MedicoDirectorio` estaba ausente en el flujo de reclamo de token.

### Solución
`Activar.cshtml.cs VincularAsync()` ahora:
1. Actualiza `EstatusReclamacion = Reclamado` y `FechaReclamacion`.
2. Llama a `_directorioService.RecalcularNivelConfianzaAsync()` (fuente canónica).
3. Todo en la misma transacción con el marcado del token como usado.
4. Badges en bloque separado con `try/catch` para no bloquear el flujo.

### Archivos modificados
- `eiibd26/Pages/Directorio/Activar.cshtml.cs`

---

## FUNC-023: Dos fuentes de verdad para confirmaciones y NivelConfianza

### Problema
Existían dos flujos: `ConfirmarAtencionAsync` (tabla `ConfirmacionComunitaria`) con un cálculo de confianza; y `OnPostConfirmarSimpleAsync` (tabla `DirectorioMedicoConfirmaciones`) con otro cálculo diferente. El Admin, Dashboard y Badges leían de la tabla nueva pero el servicio calculaba desde la vieja.

### Causa raíz
Evolución del modelo de dominio sin eliminar el flujo antiguo.

### Solución
- `DirectorioMedicoConfirmaciones` declarada como única fuente canónica.
- `MedicoDirectorioService.RecalcularNivelConfianzaAsync()` reescrito para usar la tabla canónica con la fórmula de 4 factores (perfilReclamado, cedulaVerificada, total, tieneEII).
- `GetListadoAsync` y `GetDetalleAsync` usan conteos de la tabla canónica.
- `Detalle.cshtml.cs`: eliminados `RecalcularNivelAsync` y `CalcularNivelVerificacion` privados; delegado al servicio.
- `Activar.cshtml.cs`: inyectado `IMedicoDirectorioService`; recálculo usa el servicio canónico.
- ADR documentado en `docs/adr/ADR-directorio-confirmaciones.md`.

### Archivos modificados
- `eiibd26/Services/Directorio/MedicoDirectorioService.cs`
- `eiibd26/Pages/DirectorioMedicos/Detalle.cshtml.cs`
- `eiibd26/Pages/Directorio/Activar.cshtml.cs`
- `docs/adr/ADR-directorio-confirmaciones.md` (creado)

---

## DB-005: Cascade delete borraba experiencia médica

### Problema
La relación `MedicoAreaEii → Condicion` tenía `DeleteBehavior.Cascade`, permitiendo que eliminar una condición del catálogo borrase en cascada la experiencia clínica de todos los médicos asociados.

### Causa raíz
Comportamiento por defecto de EF Core no revisado para esta relación.

### Solución
Cambiado a `DeleteBehavior.Restrict` en `ApplicationDbContext`. Ahora no es posible eliminar una condición del catálogo si tiene médicos con experiencia asociada.

### Archivos modificados
- `eiibd26/Data/ApplicationDbContext.cs`
