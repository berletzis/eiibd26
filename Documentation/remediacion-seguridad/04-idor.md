# Bloque 4 – IDOR (SEC-009, SEC-010, SEC-011)

## Análisis

### SEC-009: Dashboard — tracking de síntomas sin verificación de ownership

**Estado verificado:** `DashboardController.AddSymptom(...)` ya contiene:
```csharp
if (!await _ownership.OwnsSintomaAsync(sintomaUsuarioId, userGuid))
	return Forbid();
```
Antes de insertar cualquier registro de seguimiento, verifica que `sintomaUsuarioId` le pertenece al usuario autenticado. ✅

### SEC-010: EstadoAnimoUsuario — relaciones con IDs de otro paciente

**Estado verificado:** `EstadoAnimoUsuarioController.Nuevo(...)` ya llama a:
```csharp
var invalidField = await _ownership.ValidateEstadoAnimoRelationsAsync(
	guid,                        // userId del JWT
	dto.CondicionIds,
	dto.SintomaIds,
	dto.TratamientoIds
);
if (invalidField != null)
	return BadRequest(new { error = $"Recurso no autorizado: {invalidField}" });
```
El método `ValidateEstadoAnimoRelationsAsync` de `ClinicalOwnershipValidator` valida que cada ID de relación (condición/síntoma/tratamiento) pertenezca al `userId` extraído del JWT, no al que el cliente envía. ✅

### SEC-011: EstadoAnimoUsuario — eliminación sin verificación de ownership

**Estado verificado:** `EstadoAnimoUsuarioController.Eliminar(int id)` ya contiene:
```csharp
var entry = await _db.EstadosAnimo.FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == guid);
if (entry == null) return NotFound();
```
La consulta filtra por `id AND UsuarioId == guid` — un usuario nunca puede eliminar registros de otro usuario porque el `guid` proviene del JWT. ✅

## Conclusión

Los tres issues IDOR ya estaban remediados antes de este backlog. El `ClinicalOwnershipValidator` es el servicio centralizado de validación de pertenencia y está correctamente inyectado y usado en los controllers relevantes.

**No se requieren cambios de código.** Solo documentación.

## Estado

| Issue | Estado |
|---|---|
| SEC-009 | ✅ YA REMEDIADO — `OwnsSintomaAsync` en `DashboardController` |
| SEC-010 | ✅ YA REMEDIADO — `ValidateEstadoAnimoRelationsAsync` en `EstadoAnimoUsuarioController` |
| SEC-011 | ✅ YA REMEDIADO — filtro `id AND UsuarioId == guid` en `Eliminar` |
