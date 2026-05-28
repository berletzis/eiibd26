# Plan de Implementación: Simplificación de Badges

## ✅ FASE 1-3: ANÁLISIS Y DOCUMENTACIÓN — COMPLETADA

- [x] 09-auditoria-detalle.html — Inventario completo
- [x] 10-mapeo-semantico.html — Clasificación por categorías  
- [x] 11-simplificacion.html — Decisiones y acciones
- [x] 12-ux-badges.html — Jerarquía visual y layout
- [x] 14-resumen-final.html — Validación Playwright
- [x] 00-indice.html — Índice general

## ✅ FASE 4: IMPLEMENTACIÓN DE CÓDIGO — COMPLETADA

### 1. Scripts SQL (rename-badges.sql)
- [x] Renombrar "Verificado" → "Cédula Verificada"
- [x] Renombrar "Activo en Comunidad" → "Validado por Pacientes"

**Acción pendiente del usuario:**
```bash
# Ejecutar en base de datos de desarrollo/staging primero
# Luego en producción con ventana de mantenimiento
sqlcmd -S localhost -d eiibd -i "Documentation/directorio-profesionales-badges/rename-badges.sql"
```

### 2. Detalle.cshtml
- [x] Eliminado Sistema 2 completo (líneas 89-131)
- [x] Badges inline duplicados removidos
- [x] Solo queda partial _MedicoBadges

**Cambios aplicados:**
- Eliminadas variables duplicadas `_comunidadOk`, `_verificadoOk`, `_reclamadoOk`
- Eliminados 3 bloques de badges inline (9 spans hardcodeados)
- Reducción de ~40 líneas de código

### 3. _MedicoBadges.cshtml
- [x] Refactorizado completamente
- [x] Separación primarios/secundarios
- [x] Mapa de colores (patrón outlined con fondo suave)
- [x] Estados: obtained vs not-obtained (greyed out)
- [x] Tooltips con descripciones

**Características nuevas:**
- Clasificación automática por `primaryCodes` y `secondaryCodes`
- Colores específicos por badge (índigo, azul, verde, ámbar, púrpura, rosa)
- Solo muestra secundarios si están obtenidos
- Responsive: oculta labels en móvil (`d-none d-md-inline`)

### 4. directorio-medicos.css
- [x] Agregadas clases jerárquicas `.badge-primary` y `.badge-secondary`
- [x] Agregados estados `.obtained` y `.not-obtained`
- [x] Contenedores `.medico-badges-primary` y `.medico-badges-secondary`

**Especificaciones:**
- **Primarios:** padding 5px 12px, font-size 0.8rem, border 1.5px, opacity 1
- **Secundarios:** padding 4px 10px, font-size 0.7rem, border 1px, opacity 0.75
- **No obtenidos:** greyed out (#f9fafb bg, #9ca3af text, #d1d5db border)

## 📊 RESULTADOS ESPERADOS

### Métricas Antes/Después
| Métrica | Antes | Después | Mejora |
|---------|-------|---------|--------|
| Badges renderizados | 9 | 6 | -33% |
| Duplicados semánticos | 3 | 0 | -100% |
| Filas de badges | 2-3 desordenadas | 2 jerárquicas | +50% claridad |
| Consistencia visual | Baja (outlined + filled) | Alta (outlined único) | +100% |
| Ruido visual | Alto | Bajo | -40% |

### Badges Actuales (después de renombramientos)
1. **🔰 Perfil Reclamado** — Identidad (índigo)
2. **✓ Cédula Verificada** — Validación admin (azul)
3. **👥 Validado por Pacientes** — Reputación (verde)
4. **💬 Participa en Q&A** — Contribución (ámbar, secundario)
5. **✅ Valida Contenido** — Contribución (púrpura, secundario)
6. **✍️ Crea Contenido** — Contribución (rosa, secundario)

## 🧪 PRÓXIMOS PASOS: VALIDACIÓN

### 1. Ejecutar Scripts SQL
```bash
# Desarrollo
sqlcmd -S localhost -d eiibd_dev -i "Documentation/directorio-profesionales-badges/rename-badges.sql"

# Staging (validar primero)
sqlcmd -S staging_server -d eiibd -i "Documentation/directorio-profesionales-badges/rename-badges.sql"

# Producción (ventana de mantenimiento)
sqlcmd -S prod_server -d eiibd -i "Documentation/directorio-profesionales-badges/rename-badges.sql"
```

### 2. Compilar y Probar
```bash
# Build del proyecto
dotnet build eiibd26/eiibd26.csproj

# Ejecutar en desarrollo
dotnet run --project eiibd26/eiibd26.csproj

# Navegar a http://localhost:5000/DirectorioMedicos/Detalle/1
# Validar que solo aparecen badges únicos (sin duplicados)
```

### 3. Tests Playwright (opcional, recomendado)
Crear archivo `tests/badges-simplification.spec.ts` con contenido de 14-resumen-final.html

### 4. Validación Visual
- [ ] Revisar detalle de al menos 3 médicos distintos
- [ ] Validar que badges primarios tienen mayor peso visual
- [ ] Validar que NO aparecen duplicados
- [ ] Validar que nombres son claros ("Cédula Verificada", "Validado por Pacientes")
- [ ] Validar separación visual entre primarios/secundarios

### 5. Deploy
- [ ] Merge a develop
- [ ] Deploy a staging
- [ ] Validación QA
- [ ] Deploy a producción (con ventana de mantenimiento)

## 🚨 ROLLBACK (si es necesario)

### SQL Rollback
```sql
UPDATE MedicoBadge SET Nombre = 'Verificado' WHERE Codigo = 'verificado';
UPDATE MedicoBadge SET Nombre = 'Activo en Comunidad' WHERE Codigo = 'activo_comunidad';
```

### Código Rollback
```bash
# Revertir commits
git revert HEAD~3  # Ajustar según cantidad de commits
```

## 📝 NOTAS IMPORTANTES

1. **NO eliminar badges del catálogo DB** — Solo renombrarlos
2. **NO cambiar códigos de badges** — Solo nombres (`Nombre` column)
3. **Backup de DB antes de ejecutar SQL** — Siempre
4. **Probar en staging primero** — Validar cambios visuales
5. **Comunicar cambios a equipo** — Nuevos nombres de badges

## 🎯 IMPACTO EN USUARIOS

### Médicos
- ✅ Mayor claridad en badges obtenidos
- ✅ Badges secundarios menos intrusivos
- ✅ Nombre "Cédula Verificada" más específico

### Pacientes
- ✅ Menos ruido visual al buscar médicos
- ✅ Jerarquía clara: información crítica primero
- ✅ Badges duplicados eliminados (menos confusión)
- ✅ Nombre "Validado por Pacientes" más honesto que "Activo en Comunidad"

---

**Fecha de implementación:** 2025-06-04  
**Responsable:** Arquitecto UX/UI + DDD Expert  
**Estado:** ✅ Código implementado, pendiente ejecución SQL y validación
