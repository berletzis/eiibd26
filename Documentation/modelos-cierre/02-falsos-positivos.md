# Falsos Positivos — Auditoría 05modelos

Fecha de verificación: 2026-05-26  
Fase: Modelos de Datos (05modelos.html)

---

## MDL-019 — GlossaryValidation.UserId como string

**Severidad original:** MEDIUM  
**Estado:** ⚠️ FALSE POSITIVE

**Afirmación de la auditoría:** "GlossaryValidation.UserId es string — único caso en todo el sistema (resto son Guid). El proyecto usa IdentityUser<Guid> por lo que los IDs son Guids."

**Verificación:**

El archivo `Models/Glossary/GlossaryValidation.cs` contiene el campo:
```csharp
/// <summary>ASP.NET Identity UserId (nvarchar 450)</summary>
public string UserId { get; set; } = "";
```

El comentario `nvarchar 450` es la clave: ASP.NET Core Identity almacena el ID de usuario en `AspNetUsers.Id` como `NVARCHAR(450)`. El tipo en C# es `string` — es el tipo nativo de `IdentityUser.Id`.

**Por qué la auditoría se equivoca:** La auditoría asume que el proyecto usa `IdentityUser<Guid>`, lo que haría que `Id` fuera `Guid`. Sin embargo, el campo está correctamente marcado con el tipo `string` que corresponde al `IdentityUser.Id` estándar de ASP.NET Identity. El comentario explícito en el código confirma que es intencional.

**Conclusión:** El campo es correcto tal como está. Cambiar a `Guid` requeriría una migración de datos en producción y podría romper la relación con Identity si el tipo de `ApplicationUser.Id` es efectivamente `string` en la cadena de Identity de este proyecto.

**Acción:** Ninguna. No se toca.
