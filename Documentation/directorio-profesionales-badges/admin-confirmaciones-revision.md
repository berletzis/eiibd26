# Gestión de Confirmaciones Comunitarias en Admin

## Contexto

El administrador del directorio médico necesita poder moderar las confirmaciones comunitarias, permitiendo marcarlas como "En revisión" cuando hay duda sobre su validez, y posteriormente reactivarlas.

## Implementación

### 1. Modelo de datos

Se reutiliza el campo `ConfirmacionComunitaria.Eliminado` para representar el estado de revisión:
- `Eliminado = false` → Confirmación **activa** (cuenta para badges y nivel de confianza)
- `Eliminado = true` → Confirmación **en revisión** (no cuenta, pero se preserva para auditoría)

### 2. Interfaz de administración

**Ubicación**: `/Identity/Admin/DirectorioMedicos/Index` → Panel lateral → Sección "Comunidad / Confirmaciones"

**Tabla de confirmaciones**:
- **Email**: email del usuario que confirmó
- **Fecha**: fecha de la confirmación
- **Tipo**: tipo de confirmación (experiencia reportada)
- **Estado**: badge visual
  - Verde "Activa" → confirmación que cuenta
  - Amarillo "En revisión" → confirmación suspendida temporalmente
- **Acción**: botón toggle
  - Si está activa → botón amarillo con icono de advertencia ("Marcar en revisión")
  - Si está en revisión → botón verde con icono de check ("Marcar como activa")

**Contador inteligente**:
```
Total: 12 (10 activas, 2 en revisión)
```

### 3. Flujo de moderación

1. Admin abre el detalle de un médico en el panel lateral
2. Navega a la sección "Comunidad / Confirmaciones"
3. Identifica una confirmación sospechosa
4. Click en el botón de toggle
5. Confirmación modal: "¿Confirmar marcar en revisión esta confirmación comunitaria?"
6. Al confirmar:
   - Se actualiza `ConfirmacionComunitaria.Eliminado = true`
   - Se recalcula el nivel de confianza del médico (contador de confirmaciones baja)
   - Se re-evalúan badges automáticos (el badge "Validado por Pacientes" podría perderse si cae bajo 5)
   - El panel se refresca automáticamente
   - La fila aparece con fondo amarillo y badge "En revisión"

7. Para reactivar:
   - Click en el botón verde
   - Confirmación modal: "¿Confirmar activar esta confirmación comunitaria?"
   - Se actualiza `ConfirmacionComunitaria.Eliminado = false`
   - Se recalcula nivel y badges (el contador sube, podría recuperar badge comunidad)

### 4. Código relevante

**Backend** (`Index.cshtml.cs`):
```csharp
public async Task<IActionResult> OnPostToggleConfirmacionAsync(int confirmacionId)
{
	var conf = await _db.ConfirmacionesComunitarias.IgnoreQueryFilters()
		.FirstOrDefaultAsync(c => c.Id == confirmacionId);

	if (conf is null)
		return new JsonResult(new { success = false, message = "Confirmación no encontrada." });

	conf.Eliminado = !conf.Eliminado;
	await _db.SaveChangesAsync();

	await _dirService.RecalcularNivelConfianzaAsync(conf.MedicoDirectorioId);
	try { await _badgeService.EvaluarBadgesAutomaticosAsync(conf.MedicoDirectorioId); } catch { }

	return new JsonResult(new { success = true });
}
```

**Frontend** (`Index.cshtml`):
```javascript
async function toggleConfirmacion(confirmacionId, estaEnRevision) {
	const accion = estaEnRevision ? 'activar' : 'marcar en revisión';
	if (!confirm(`¿Confirmar ${accion} esta confirmación comunitaria?`)) return;
	const r = await postJson('@Url.Page(null, "ToggleConfirmacion")', { confirmacionId });
	if (r.success) {
		const medicoId = document.getElementById('ep_id')?.value;
		if (medicoId) await abrirEditar(parseInt(medicoId));
		tabla.ajax.reload(null, false);
	} else {
		alert(r.message || 'Error al cambiar estado de confirmación.');
	}
}
```

### 5. Impacto en badges

**Badge "Validado por Pacientes"** (`activo_comunidad`):
- Requiere ≥5 confirmaciones **activas** (no en revisión)
- Si un médico tiene 6 confirmaciones y el admin marca 2 en revisión → quedan 4 activas → pierde el badge
- Si posteriormente se reactivan → recupera el badge automáticamente

### 6. Semántica canónica

Los badges en el admin ahora muestran **tooltips con nombres canónicos**:
- 🟢 Verde → "Validado por Pacientes (≥5 confirmaciones)"
- 🔵 Azul → "Cédula Verificada"
- 🟢 Verde → "Perfil Reclamado"

### 7. Query de confirmaciones

El DTO ahora usa `.IgnoreQueryFilters()` para mostrar **todas** las confirmaciones (activas + en revisión):

```csharp
var confs = await _db.ConfirmacionesComunitarias.IgnoreQueryFilters().AsNoTracking()
	.Include(c => c.TipoConfirmacion)
	.Where(c => c.MedicoDirectorioId == id)
	.ToListAsync();
```

Y el contador distingue:
```csharp
totalConfirmaciones = confs.Count(c => !c.Eliminado),
totalConfirmacionesIncRevision = confs.Count,
```

## Ventajas

✅ **No destructivo**: las confirmaciones en revisión se preservan, no se borran  
✅ **Reversible**: el admin puede reactivarlas en cualquier momento  
✅ **Automático**: el nivel y badges se recalculan inmediatamente  
✅ **Auditable**: todas las confirmaciones quedan en la base de datos con su estado  
✅ **Transparente**: el admin ve el desglose exacto (activas vs en revisión)

## Casos de uso

- **Spam**: usuario crea múltiples confirmaciones falsas → admin marca en revisión hasta investigar
- **Disputa**: paciente se retracta → admin marca en revisión temporalmente
- **Error**: confirmación duplicada por bug → admin marca en revisión la duplicada
- **Reactivación**: después de verificar, admin reactiva confirmaciones legítimas
