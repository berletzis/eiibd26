# Findings Cerrados — Auditoría 05modelos

Fecha de cierre: 2026-05-26  
Fase: Modelos de Datos (05modelos.html)  
Ejecutado por: Arquitecto Senior / Auditoría Asistida

---

## MDL-006 — [Key] explícito en Voto.Id y AIRequestLog.Id

**Severidad original:** HIGH  
**Estado:** ✅ CERRADO

**Problema:** Las entidades `Voto` y `AIRequestLog` tenían `public Guid Id` sin el atributo `[Key]` explícito. EF Core resuelve la PK por convención de nombre, pero la ausencia de `[Key]` genera ambigüedad para herramientas de scaffolding y generadores de DTO.

**Causa raíz:** Las entidades fueron creadas siguiendo convención implícita en lugar del patrón explícito del resto del codebase.

**Solución aplicada:**
- `Models/Voto.cs` — Agregado `[Key]` sobre `public Guid Id`; agregado `using System.ComponentModel.DataAnnotations`
- `Models/AIRequestLog.cs` — Agregado `[Key]` sobre `public Guid Id`; agregado `using System.ComponentModel.DataAnnotations`

**Impacto del cambio:** Solo metadato EF. Sin migración. Sin cambio de comportamiento runtime. Sin impacto en BD.

**Archivos modificados:**
- `eiibd26/Models/Voto.cs`
- `eiibd26/Models/AIRequestLog.cs`

---

## MDL-007 — [Required] en string? Nombre en Perfil.cs

**Severidad original:** HIGH  
**Estado:** ✅ CERRADO

**Problema:** `Perfil.Nombre` estaba declarado como `[Required] public string? Nombre { get; set; }`. El atributo `[Required]` y el operador `?` son contradictorios: `[Required]` indica que el campo es obligatorio para ModelState, pero `string?` permite null al compilador y en runtime.

**Causa raíz:** Código escrito antes de activar nullable reference types en el proyecto, sin actualizar los atributos de validación para ser coherentes. Este conflicto es la causa raíz del bug de binding documentado: cuando `Perfil` se usa como `[BindProperty]`, ModelState puede fallar inesperadamente.

**Solución aplicada:**
```csharp
// Antes:
[Required]
[StringLength(256)]
public string? Nombre { get; set; }

// Después:
[Required]
[StringLength(256)]
public string Nombre { get; set; } = string.Empty;
```

**Impacto del cambio:** Elimina la contradicción. La propiedad ahora es no-nullable con valor por defecto `string.Empty`. ModelState valida correctamente. Sin impacto en BD (columna ya era NOT NULL en SQL Server por el `[Required]`).

**Archivos modificados:**
- `eiibd26/Models/Perfil.cs`

---

## MDL-012 — Pregunta.UsuarioId sin FK en DbContext

**Severidad original:** MEDIUM  
**Estado:** ✅ STALE — Resuelto en sesión anterior (DB-06)

**Evidencia:** `ApplicationDbContext.cs` contiene:
```csharp
// DB-008: FK explícita a AspNetUsers — evita comportamiento OnDelete indefinido
b.HasOne<ApplicationUser>()
 .WithMany()
 .HasForeignKey(p => p.UsuarioId)
 .OnDelete(DeleteBehavior.Restrict);
```

**Migración aplicada:** `20260526010111_DB_06_Indices_FK_Explicitas` — aplicada a producción el 2026-05-26.

---

## MDL-013 — Respuesta.UsuarioId sin FK en DbContext

**Severidad original:** MEDIUM  
**Estado:** ✅ STALE — Resuelto en sesión anterior (DB-06)

**Evidencia:** `ApplicationDbContext.cs` contiene configuración FK `Respuesta.UsuarioId → ApplicationUser` con `OnDelete(DeleteBehavior.Restrict)`.

**Migración aplicada:** `20260526010111_DB_06_Indices_FK_Explicitas` — misma migración que MDL-012.
