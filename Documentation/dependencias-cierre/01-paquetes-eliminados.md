# 01 · Paquetes eliminados

**Auditoría:** 09dependencias.html  
**Fecha de cierre:** 2025  
**Finding:** DEP-001  

---

## Microsoft.AspNetCore.DataProtection.Extensions 10.0.3

### Problema
El paquete estaba declarado en `eiibd26/eiibd26.csproj` apuntando a .NET 10 (`10.0.3`) mientras el proyecto tiene `<TargetFramework>net8.0</TargetFramework>`. Categorizado como HIGH en la auditoría.

### Diagnóstico
Búsqueda exhaustiva en el código fuente (`*.cs`, `*.cshtml`, `*.json`):

- **Ningún uso** de `ITimeLimitedDataProtector` ni de ninguna API exclusiva de `Microsoft.AspNetCore.DataProtection.Extensions`
- `AddDataProtection()` viene del SDK base de ASP.NET Core — no requiere paquete externo
- `IDataProtector` viene del runtime de ASP.NET Core — incluido en el framework target

### Clasificación
**Paquete fantasma** — declarado en `.csproj` pero sin ninguna referencia activa en el código.

### Acción aplicada
```diff
- <PackageReference Include="Microsoft.AspNetCore.DataProtection.Extensions" Version="10.0.3" />
```

Línea eliminada de `eiibd26/eiibd26.csproj`.

### Impacto
| Área | Impacto |
|------|---------|
| Runtime | Ninguno |
| Build | Menor (menor superficie de dependencias) |
| Publicación | Menor (reduce binarios innecesarios) |
| Riesgo | Muy bajo |

### Validación
Build exitoso tras la eliminación. Sin errores de compilación.

---

_Archivo: `eiibd26/eiibd26.csproj`_
