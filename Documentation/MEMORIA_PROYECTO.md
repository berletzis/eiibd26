# MEMORIA_PROYECTO.md
**Proyecto:** EIIBD — Plataforma de pacientes con enfermedad inflamatoria intestinal  
**Stack:** ASP.NET Core 8 Razor Pages · EF Core 8 · SQL Server · Hangfire · Identity  

---

## Convenciones del proyecto

### Esquema de BD
- **NUNCA usar `dotnet ef database update`** en producción. Todos los cambios de esquema se aplican con SQL directo.
- Las migraciones EF Core existen pero solo como referencia histórica.
- Columnas nuevas se añaden con `ALTER TABLE ... ADD ... NULL` primero, luego se rellena con UPDATE, luego se agrega la constraint NOT NULL si aplica.

### Roles del sistema
- `Paciente` — usuario base, acceso a su propio perfil/datos
- `Medico` — acceso a dashboard médico, puede validar términos del glosario
- `Administrador` — acceso total
- Los roles se verifican con `User.IsInRole()` o `[Authorize(Roles = "...")]`

### Patrones aprobados

#### Handlers de Razor Pages con parámetros opcionales
```csharp
// ✅ CORRECTO — siempre usar nullable para strings y tipos de valor en handlers
public async Task<IActionResult> OnPostMiHandlerAsync(
	string? campo1, DateTime? fecha, int? idOpcional)

// ❌ INCORRECTO — string y DateTime sin ? causan 400 cuando el campo llega vacío
public async Task<IActionResult> OnPostMiHandlerAsync(
	string campo1, DateTime fecha)
```
**Razón:** Con nullable reference types habilitado (.NET 8), ASP.NET Core trata `string` y tipos de valor no nullable en parámetros de handler como implícitamente `[Required]`. Si el campo llega vacío desde el formulario/fetch, el model binder devuelve `null` → `ModelState` inválido → 400 antes de entrar al handler.

#### Fetch desde JS con antiforgery token
```javascript
// Patrón estándar aprobado en el proyecto
function getAntiforgeryToken() {
	const el = document.querySelector('input[name="__RequestVerificationToken"]');
	return el ? el.value : '';
}
function addAntiforgeryToken(formData) {
	const token = getAntiforgeryToken();
	if (token) formData.append('__RequestVerificationToken', token);
	return formData;
}
```

#### Soft delete
Todos los modelos de usuario usan `Eliminado = true` + `FechaEliminado`. Nunca DELETE físico.

#### Queries de seguridad — datos del usuario
Siempre verificar que el registro pertenece al usuario autenticado:
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var rel = await _db.Tabla
	.FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == id && !x.Eliminado);
if (rel == null) return NotFound(); // no BadRequest — evita enumeration
```

#### Mostrar contenido público con datos de médicos
Solo mostrar texto/nombre si el médico tiene badge `perfil_reclamado` o `verificado`. Usar `FilterCommentsByVerifiedDoctorAsync` (en `GlossaryService`) como referencia del pipeline.

---

## Errores repetidos — NO volver a cometer

### ERROR-01: Parámetros no nullable en handlers → 400
**Archivos afectados hasta ahora:** UsuarioLaboratorios, UsuarioCondiciones, UsuarioTratamientos, UsuarioSintomas  
**Patrón de error:** `string param` o `DateTime param` en `OnPost*Async`  
**Regla:** SIEMPRE `string?` y `DateTime?`. Validar `HasValue` o `IsNullOrWhiteSpace` dentro del handler.

### ERROR-02: Mostrar texto público sin verificar badge de médico
**Ocurrió en:** `GlossaryService.GetValidationCountsAsync` → `MeaningComments`  
**Síntoma:** Texto de cuentas de prueba visible sin login  
**Regla:** Todo texto generado por usuarios con rol Medico que se muestre al público debe pasar por el filtro de badge verificado.

### ERROR-03: Paquetes NuGet con versión mayor al TFM del proyecto
**Ocurrió en:** `Microsoft.AspNetCore.DataProtection.Extensions 10.0.3` en proyecto `net8.0`  
**Regla:** Siempre alinear la versión de paquetes Microsoft.* con el TFM. Si el paquete no tiene uso en código fuente → eliminar.

### ERROR-04: Tooling packages con versión desalineada
**Ocurrió en:** `Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0` en `net8.0`  
**Regla:** Los paquetes `*.Design` y `*.CodeGeneration.*` son solo tooling. Su versión major debe coincidir con el TFM.

### ERROR-05: Encoding corrupto en archivos .cs
**Ocurrió en:** `UsuarioSintomas.cshtml.cs` — string con `\uFFFD` sustituyendo la `í` de "Síntoma"  
**Causa:** El archivo fue guardado con encoding incorrecto en algún momento  
**Regla:** Al editar archivos con acentos, verificar el resultado. Si `replace_string_in_file` falla en una línea con tilde, es señal de encoding corrupto.

---

## Decisiones arquitectónicas

### ADR-001: Sin EF migrations en producción
Decidido desde el inicio del proyecto. Toda la BD se gestiona con SQL directo para tener control total.

### ADR-002: Badge system para confianza médica
Los médicos necesitan badge `perfil_reclamado` o `verificado` para que su contenido sea visible al público. Esto separa el rol Identity (`Medico`) de la confianza pública (badge). Un médico puede tener el rol pero no tener badge si aún no reclamó su perfil del directorio.

### ADR-003: Glosario desacoplado del dominio médico
`GlossaryService` lee datos de síntomas/tratamientos a través de `IMedicalDataAdapter`, no directamente. Esto permite cambiar el origen de datos médicos sin tocar el glosario.

### ADR-004: Validaciones con `Approved = true` como gate
Las validaciones de médicos pasan por un campo `Approved`. Solo las aprobadas son visibles. Esto permite moderación futura sin cambiar el modelo de datos.

---

## Lecciones aprendidas

1. **Los archivos de auditoría HTML a veces describen problemas ya resueltos** (MDL-012/013 eran stale). Siempre verificar contra el código fuente antes de aplicar cambios.

2. **Un 400 en handler POST de Razor Pages casi siempre es un parámetro no nullable** con campo vacío en el formulario. Revisar la firma del método antes de buscar causas más complejas.

3. **`dotnet list package --vulnerable` es evidencia de auditoría de seguridad.** Documentar el resultado en cada auditoría de dependencias.

4. **Los paquetes fantasma (declarados en csproj pero sin uso en código) deben eliminarse**, no downgradearse. Verificar siempre con grep antes de decidir.

5. **El service worker puede enmascarar errores de red.** El mensaje `[SW] Fetch failed, trying cache` aparece porque el service worker intenta recuperar la respuesta 400 desde caché. El error real es el 400, no el service worker.

---

## Componentes creados en este proyecto

| Componente | Ubicación | Descripción |
|-----------|-----------|-------------|
| `FilterCommentsByVerifiedDoctorAsync` | `GlossaryService.cs` | Filtra comentarios de médicos a mostrar públicamente |
| Badge system | `MedicosPerfilBadge`, `MedicosBadge` | Confianza pública de médicos |
| `LaboratoryUnitCatalog` | Models + DbSet | Catálogo de unidades de medida para laboratorios |
| `PatientLaboratoryResult` | Models | Resultados de lab del paciente (con soft delete) |
| `GlossaryValidationCountsDto` | DTOs | Conteos de confianza del glosario (IA + humanos) |
| `dependencias-cierre/` | Documentation | Set de closure docs para auditoría de dependencias |
| `modelos-cierre/` | Documentation | Set de closure docs para auditoría de modelos |
