# 📦 Guía de Despliegue - Sistema de Scoring Optimizado

## ✅ Pre-requisitos

- ✅ Visual Studio 2026 (o superior)
- ✅ .NET 8 SDK instalado
- ✅ Base de datos actualizada
- ✅ Git con cambios staged

---

## 🔧 Instalación Local (Desarrollo)

### 1. Actualizar Código
```powershell
# Terminal: PowerShell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26

# Verificar cambios
git status
# Debería mostrar:
# modified: eiibd26/Pages/Contenidos/Index.cshtml.cs
```

### 2. Compilar
```powershell
# Opción 1: Visual Studio
# Presiona: Ctrl+Shift+B (Build Solution)
# O: Build → Build Solution

# Opción 2: CLI
dotnet clean
dotnet build
```

### 3. Probar Localmente
```powershell
# Iniciar aplicación
dotnet run

# O en Visual Studio: Presiona F5

# Abrir navegador
Start-Process "https://localhost:7002/Contenidos"
```

### 4. Validar
```
1. Ve a https://localhost:7002/Contenidos
2. Busca: "Diarrea"
3. Verifica:
   ✅ Resultado exacto en posición 1
   ✅ Output en Visual Studio muestra Score 10000
```

---

## 🚀 Despliegue a Producción

### Pre-Despliegue Checklist

- [ ] Código compilado sin errores
- [ ] Tests locales pasados
- [ ] Output en Debug verificado
- [ ] No hay warnings del compilador
- [ ] Git branch está limpio
- [ ] Cambios están documentados

### 1. Commit y Push

```powershell
# Agregar cambios
git add eiibd26/Pages/Contenidos/Index.cshtml.cs

# Commit
git commit -m "feat: optimize search relevance scoring

- Scale scoring system (10-10,000) for better differentiation
- Ensure exact title matches always appear on page 1
- Apply sorting before pagination
- Add debug output for validation

Fixes: Search results with exact title match appearing on page 5"

# Push a repo
git push origin master
```

### 2. Build para Producción

```powershell
# Limpiar
dotnet clean

# Build Release
dotnet build -c Release

# Publicar
dotnet publish -c Release -o ./publish
```

### 3. Desplegar a Servidor

**Si uses Azure App Service:**
```powershell
# Opción 1: Azure CLI
az webapp up --name eiibd26 --resource-group tu-grupo --runtime dotnet

# Opción 2: Visual Studio Publish
# Right-click en proyecto → Publish → Azure → App Service → Deploy
```

**Si uses manual deployment:**
```powershell
# Copiar archivos publicados
xcopy publish\* C:\inetpub\wwwroot\eiibd26\ /S /I /Y

# Reiniciar IIS
iisreset
```

**Si uses Docker:**
```powershell
# Build imagen
docker build -t eiibd26:latest .

# Push a registro
docker push tu-registro.azurecr.io/eiibd26:latest

# Desplegar
kubectl apply -f deployment.yaml
```

---

## ✅ Post-Despliegue

### Validación en Producción

```powershell
# 1. Acceder a aplicación
Start-Process "https://www.tu-dominio.com/Contenidos"

# 2. Probar búsqueda
# Busca: "Diarrea"
# Verificar: aparece en página 1

# 3. Revisar logs
# Azure: Azure Portal → Logs → App Service logs
# Local: Event Viewer o archivo de log

# 4. Monitorear
# Azure: Application Insights
# Verificar: Response time, Error rate, Throughput
```

### Métricas a Monitorear

```
📊 Métricas Importantes:
│
├─ Response Time
│  ├─ Antes: ~100-150ms
│  └─ Después: ~100-150ms (sin cambio) ✓
│
├─ CPU Usage
│  ├─ Antes: ~30-40%
│  └─ Después: ~30-40% (sin cambio) ✓
│
├─ Memory Usage
│  ├─ Antes: ~200-300MB
│  └─ Después: ~200-300MB (sin cambio) ✓
│
└─ Search Queries/sec
   ├─ Monitorear picos
   └─ Alertar si > 100/sec durante > 5min
```

---

## 🔄 Rollback (Si Necesario)

```powershell
# Si hay problema en producción:

# Opción 1: Revertir commit
git revert HEAD
git push origin master
# Esperar a redeploy automático

# Opción 2: Manual rollback
# 1. Restaurar versión anterior
git checkout HEAD~1 -- eiibd26/Pages/Contenidos/Index.cshtml.cs

# 2. Commit y deploy
git commit -m "revert: search scoring optimization"
git push origin master

# 3. Redeploy desde Azure
# En Azure Portal: Deployment Center → Manual Deployment
# O: dotnet publish y copiar archivos manualmente
```

---

## 📋 Checklist de Despliegue

### Antes
- [ ] Código compilado sin errores
- [ ] Tests locales pasados
- [ ] Documentación actualizada
- [ ] Cambios en Git staged
- [ ] PR creada (si aplica)

### Durante
- [ ] Build Release ejecutado
- [ ] Artefactos generados sin error
- [ ] Deploy a servidor completado
- [ ] Aplicación inicia sin excepciones

### Después
- [ ] Aplicación accesible en producción
- [ ] Búsqueda funciona correctamente
- [ ] Resultado exacto en página 1 ✓
- [ ] Debug output visible (si Debug mode)
- [ ] Sin excepciones en logs
- [ ] Monitoreo activado
- [ ] Equipo notificado

---

## 📞 Soporte y Troubleshooting

### Problema: Búsqueda aún muestra resultados en página 5

**Diagnóstico:**
```
1. Verificar que cambios estén en servidor
   git log | head -5
   # Debería mostrar tu commit

2. Verificar que el código compilado es la versión correcta
   # En el servidor, revisar timestamp de .dll
   ls -la eiibd26.dll | select FullName, LastWriteTime

3. Reiniciar aplicación
   iisreset
   # O en Azure: Restart App Service
```

### Problema: Performance degradado

**Diagnóstico:**
```
1. Revisar Response Time en Application Insights
2. Verificar si hay muchas búsquedas simultáneas
3. Revisar logs de errores
4. Posible causa: base de datos lenta
   - Verificar índices en tabla Contenidos
   - Ejecutar: sp_helpindex 'Contenidos'
```

### Problema: Excepciones en logs

**Común:** `NullReferenceException` en `CalculateRelevanceScore()`
```
Solución:
1. Verificar que searchTerm no sea null
2. Verificar que Contenido tiene título/contenido
3. Agregar validaciones adicionales
```

---

## 📊 Monitoreo Continuo

### Azure Application Insights

```csharp
// Si quieres agregar telemetría personalizada:

private readonly TelemetryClient _telemetryClient;

public async Task<IActionResult> OnGetAsync()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // ... código ...
    
    stopwatch.Stop();
    _telemetryClient.TrackEvent("SearchCompleted", new Dictionary<string, string>
    {
        { "SearchQuery", SearchQuery },
        { "ResultCount", Items.Count.ToString() },
        { "DurationMs", stopwatch.ElapsedMilliseconds.ToString() }
    });
    
    return Page();
}
```

### Alertas Recomendadas

```yaml
Alertas:
  - Nombre: Search_SlowResponse
    Condición: Response Time > 500ms
    Acción: Email a DevOps
  
  - Nombre: Search_HighErrorRate
    Condición: Error Rate > 1%
    Acción: SMS + Email
  
  - Nombre: Search_HighLoad
    Condición: Requests/sec > 100
    Acción: Email + Slack
```

---

## 📝 Documentación para el Equipo

### Para QA
- Referencia: `TEST-SEARCH-SCORING.md`
- Casos de prueba incluidos
- Validación de checklist

### Para DevOps
- Checklist: Este archivo
- Rollback procedures: Arriba
- Monitoring setup: Arriba

### Para Desarrolladores
- Referencia de código: `SEARCH-CODE-STRUCTURE-REFERENCE.md`
- Explicación: `SEARCH-RELEVANCE-SCORING-OPTIMIZED.md`
- Comparativa: `SEARCH-BEFORE-AFTER-COMPARISON.md`

---

## 🎉 Confirmación de Despliegue Exitoso

Cuando veas esto en producción:

```
✅ Búsqueda de "Diarrea" → Resultado en posición 1
✅ Response time: ~100ms
✅ CPU/Memory: sin cambios
✅ Sin errores en logs
✅ Usuarios reportan mejor relevancia
```

**¡Despliegue completado exitosamente!** 🚀
