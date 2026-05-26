# 02 · Tooling alineado

**Auditoría:** 09dependencias.html  
**Fecha de cierre:** 2025  
**Finding:** DEP-002  

---

## Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0 → 8.0.23

### Problema
El paquete estaba en versión `9.0.0` (tooling de .NET 9) en un proyecto `net8.0`. Categorizado como HIGH en la auditoría.

### Verificación previa (condición de autorización)

**1. Herramienta activa:**
```
dotnet aspnet-codegenerator
```
Salida: tool instalada, generadores disponibles: `area`, `blazor-identity`, `blazor`, `controller`, `identity`, `minimalapi`, `razorpage`, `view`.

**2. Dependencia explícita en código fuente:**
```
grep -r "aspnet-codegenerator|CodeGeneration|scaffolding" --include=*.cs --include=*.cshtml --include=*.json
```
Resultado: **sin referencias en código de aplicación**. Solo en artefactos generados (`*.deps.json`).

**3. Versiones 8.x disponibles en NuGet:**
Verificado vía NuGet flat-container index: `8.0.7`, `8.0.22`, `8.0.23`.  
Versión elegida: **`8.0.23`** (última patch disponible en la serie 8.x).

### Acción aplicada
```diff
- <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="9.0.0" />
+ <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="8.0.23" />
```

### Por qué es seguro
- Es un paquete **solo de desarrollo** (scaffolding)
- No genera código en runtime ni se publica con la aplicación
- La versión 8.0.23 es compatible con `net8.0` y con `Microsoft.EntityFrameworkCore.Design 8.0.21`

### Impacto
| Área | Impacto |
|------|---------|
| Runtime | Ninguno |
| Build | Alineación correcta con TFM net8.0 |
| Scaffolding | Sin cambio funcional |
| Riesgo | Muy bajo |

### Validación
Build exitoso tras el cambio. Sin errores de compilación.

---

_Archivo: `eiibd26/eiibd26.csproj`_
