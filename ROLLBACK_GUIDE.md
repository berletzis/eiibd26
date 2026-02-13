# 🔒 ROLLBACK GUIDE - Performance Optimizations

**Fecha:** $(Get-Date)  
**Proyecto:** eiibd26  
**Branch:** master (performance-optimizations pendiente)

---

## 📋 Cambios Aplicados (en orden)

### ✅ Fase 1: Compresión y Caching (YA APLICADO)
**Archivos modificados:**
- `eiibd26/Program.cs`

**Cambios:**
1. Agregado `AddResponseCompression` con Brotli y Gzip
2. Agregado `AddResponseCaching` y `AddMemoryCache`
3. Agregado caching headers a archivos estáticos (1 año)

**Cómo revertir:**
```powershell
# Opción 1: Revertir último commit (si ya hiciste commit)
git revert HEAD

# Opción 2: Restaurar archivo específico desde último commit
git checkout HEAD -- eiibd26/Program.cs

# Opción 3: Usar backup manual (ver abajo)
Copy-Item ".\BACKUPS\Program.cs.backup" -Destination ".\eiibd26\Program.cs" -Force
```

---

### 🔄 Fase 2: Memory Cache en Homepage (PENDIENTE)
**Archivos a modificar:**
- `eiibd26/Pages/Home/Index.cshtml.cs`

**Cambios planeados:**
1. Inyectar `IMemoryCache` en constructor
2. Cachear BlogList por 5 minutos
3. Cachear Featured1042/Featured1043 por 10 minutos

**Rollback:**
```powershell
# Restaurar desde backup
Copy-Item ".\BACKUPS\Index.cshtml.cs.backup" -Destination ".\eiibd26\Pages\Home\Index.cshtml.cs" -Force
```

---

### 🔄 Fase 3: DbContext Optimizations (PENDIENTE)
**Archivos a modificar:**
- `eiibd26/Program.cs` (línea ~18-25)

**Cambios planeados:**
1. Agregar `CommandTimeout(30)`
2. Agregar `MaxBatchSize(100)`

**Rollback:**
```powershell
# Restaurar desde backup
Copy-Item ".\BACKUPS\Program.cs.phase2.backup" -Destination ".\eiibd26\Program.cs" -Force
```

---

## 🚨 Rollback Completo (Todos los cambios)

### Opción A: Git Reset (Recomendado si hiciste commits)
```powershell
# Ver últimos commits
git log --oneline -5

# Volver al commit anterior a las optimizaciones
git reset --hard <commit-id-antes-de-optimizaciones>

# O revertir todo sin perder historial
git revert <commit-id-optimizaciones>
```

### Opción B: Restaurar desde Backups Manuales
```powershell
# Ir al directorio del proyecto
cd "D:\Users\berletzis\Source\Repos\eiibd\eiibd26"

# Restaurar TODOS los archivos modificados
Copy-Item ".\BACKUPS\Program.cs.original" -Destination ".\eiibd26\Program.cs" -Force
Copy-Item ".\BACKUPS\Index.cshtml.cs.original" -Destination ".\eiibd26\Pages\Home\Index.cshtml.cs" -Force

# Reconstruir proyecto
dotnet build

# Reiniciar aplicación
```

### Opción C: Stash de Git (Si no hiciste commit)
```powershell
# Guardar cambios sin commit
git stash push -m "performance optimizations backup"

# Recuperar después
git stash pop
```

---

## 📁 Ubicación de Backups

Todos los backups están en: `D:\Users\berletzis\Source\Repos\eiibd\eiibd26\BACKUPS\`

**Estructura:**
```
BACKUPS/
├── Program.cs.original            ← Antes de CUALQUIER cambio
├── Program.cs.phase1.backup       ← Después de Fase 1 (compresión)
├── Program.cs.phase2.backup       ← Después de Fase 2 (DbContext opts)
├── Index.cshtml.cs.original       ← Antes de Memory Cache
└── Index.cshtml.cs.phase2.backup  ← Después de Memory Cache
```

---

## ⚙️ Verificación Post-Rollback

Después de revertir cambios, verifica que todo funcione:

```powershell
# 1. Compilar proyecto
dotnet build .\eiibd26\eiibd26.csproj

# 2. Buscar errores de compilación
dotnet build .\eiibd26\eiibd26.csproj 2>&1 | Select-String "error"

# 3. Ejecutar tests (si existen)
dotnet test

# 4. Iniciar aplicación en modo desarrollo
dotnet run --project .\eiibd26\eiibd26.csproj
```

**Checklist visual:**
- [ ] Página principal carga correctamente
- [ ] Imágenes se muestran bien
- [ ] No hay errores en la consola del navegador (F12)
- [ ] Blog cards funcionan
- [ ] Login/Register funcionan

---

## 🆘 Si Algo Sale Mal

### Error: "Services not registered"
**Síntoma:** `InvalidOperationException: No service for type 'IMemoryCache'`

**Solución:**
```csharp
// En Program.cs, asegúrate de tener:
builder.Services.AddMemoryCache();
```

### Error: "Response compression not working"
**Síntoma:** HTML aún pesa mucho en Network tab

**Solución:**
1. Verifica que `app.UseResponseCompression()` esté ANTES de `app.UseStaticFiles()`
2. Verifica que el browser acepte compresión: `Accept-Encoding: gzip, br`

### Error: "Static files return 404"
**Síntoma:** CSS/JS no cargan después de cambios

**Solución:**
```csharp
// Reemplaza UseStaticFiles complejo por simple:
app.UseStaticFiles();
```

### Error de Compilación
**Si el proyecto no compila:**
```powershell
# Limpiar y reconstruir
dotnet clean
dotnet restore
dotnet build
```

---

## 📞 Comandos de Emergencia

```powershell
# DESHACER TODO (Nuclear Option)
git reset --hard origin/master
dotnet restore
dotnet build

# Verificar estado del repositorio
git status
git diff

# Ver últimos cambios aplicados
git log --oneline --graph --decorate -5

# Comparar archivo actual con versión anterior
git diff HEAD~1 eiibd26/Program.cs
```

---

## 📊 Métricas para Validar Rollback Exitoso

Después de revertir, compara estas métricas con las originales:

| Métrica | Valor Original | Post-Rollback | ✅/❌ |
|---------|----------------|---------------|------|
| **Tiempo de carga homepage** | ~2.5s | ? | |
| **HTML size (Network)** | ~180KB | ? | |
| **Errores de compilación** | 0 | ? | |
| **Tests pasando** | 100% | ? | |

**Comando para medir:**
```powershell
# Tiempo de respuesta
Measure-Command { Invoke-WebRequest -Uri "https://localhost:7001" -UseBasicParsing }

# Tamaño de respuesta (bytes)
(Invoke-WebRequest -Uri "https://localhost:7001" -UseBasicParsing).RawContentLength
```

---

## 📝 Notas Importantes

1. **Siempre haz backup antes de aplicar cambios**
2. **Prueba cada fase antes de pasar a la siguiente**
3. **Si algo falla, revierte INMEDIATAMENTE**
4. **Documenta cualquier error nuevo en este archivo**
5. **Usa Git para rastrear cambios (commit frecuente)**

---

## ✅ Checklist de Seguridad Pre-Cambios

Antes de aplicar CUALQUIER optimización:
- [ ] Código compila sin errores
- [ ] Tests pasan (si existen)
- [ ] Backup creado en `BACKUPS/`
- [ ] Git commit del estado actual
- [ ] Documentación actualizada en este archivo
- [ ] Plan de rollback específico definido

---

**Última actualización:** Script de PowerShell para crear backups automáticamente está en `create-backups.ps1`
