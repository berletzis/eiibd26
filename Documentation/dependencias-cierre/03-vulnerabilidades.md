# 03 · Vulnerabilidades

**Auditoría:** 09dependencias.html  
**Fecha de análisis:** 2025  

---

## Resultado del análisis

```
dotnet list package --vulnerable
```

### Salida
```
The given project `eiibd26` has no vulnerable packages given the current sources.
```

### Paquetes auditados
24 paquetes NuGet declarados en `eiibd26/eiibd26.csproj`.

### Conclusión
**0 vulnerabilidades conocidas** en la fecha de análisis según las fuentes NuGet configuradas.

---

## Alcance cubierto

| Categoría | Estado |
|-----------|--------|
| CVEs conocidos en NuGet Advisory DB | ✅ Sin hallazgos |
| Paquetes con versiones comprometidas | ✅ Ninguno |
| Paquetes ghost eliminados | ✅ DEP-001 eliminado |
| Paquetes de tooling desalineados | ✅ DEP-002 alineado |

---

## Nota

Este análisis cubre la base de datos de vulnerabilidades de NuGet en la fecha indicada.  
Se recomienda repetir `dotnet list package --vulnerable` en cada ciclo de auditoría o ante avisos de seguridad de Microsoft.

---

_Comando ejecutado en: `D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26\`_
