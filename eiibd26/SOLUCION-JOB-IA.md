# Solución: Job de IA no se ejecutaba

## 📋 Problema Identificado

El sistema de respuestas automáticas de IA (Claude/Anthropic) estaba configurado correctamente, pero **no se ejecutaba** al crear una nueva pregunta.

### Causa Raíz

El código para iniciar el job de IA (`AiAnswerJob`) solo existía en `PreguntasApiController.CrearPregunta`, pero **las preguntas se estaban creando a través de otro endpoint**: `usuarioPreguntasRespuestas.OnPostCrearPreguntaAsync`.

Este segundo endpoint NO tenía implementado el inicio del job de IA.

## ✅ Cambios Realizados

### 1. **PreguntasApiController.cs** (mejorado)

Se agregaron logs más detallados y se cambió de `Task.Run` a `Task.Factory.StartNew` con `LongRunning` para asegurar un thread separado:

```csharp
// Logs más detallados en cada paso
_logger.LogInformation("🚀 [CONTROLLER] Iniciando Job de IA...");
_logger.LogInformation("⚡ [TASK.RUN] Scope creado exitosamente");
_logger.LogInformation("✅ [TASK.RUN] AiAnswerJob obtenido del DI");

// Cambio de Task.Run a Task.Factory.StartNew
Task.Factory.StartNew(async () =>
{
    // ... código del job
}, TaskCreationOptions.LongRunning).Unwrap();
```

### 2. **usuarioPreguntasRespuestas.cshtml.cs** (NUEVO)

Se agregó la funcionalidad de iniciar el job de IA cuando se crea una pregunta desde esta página:

**Cambios en el constructor:**
```csharp
private readonly IServiceProvider _serviceProvider;

public PreguntasRespuestasModel(
    ApplicationDbContext db, 
    ILogger<PreguntasRespuestasModel> logger,
    IServiceProvider serviceProvider) // ← NUEVO
{
    _db = db;
    _logger = logger;
    _serviceProvider = serviceProvider; // ← NUEVO
}
```

**Cambios en OnPostCrearPreguntaAsync:**
```csharp
_db.Preguntas.Add(p);
await _db.SaveChangesAsync();

// ===== NUEVO: ENCOLAR JOB DE IA EN SEGUNDO PLANO =====
_logger.LogInformation("🚀 [PAGE MODEL] Iniciando Job de IA...");

var preguntaIdCapture = p.Id;
Task.Factory.StartNew(async () =>
{
    using var scope = _serviceProvider.CreateScope();
    var aiJob = scope.ServiceProvider.GetRequiredService<eiibd26.Jobs.AiAnswerJob>();
    await aiJob.ProcesarPreguntaAsync(preguntaIdCapture);
}, TaskCreationOptions.LongRunning).Unwrap();
// =============================================

return new JsonResult(new { ok = true, id = p.Id, slug = p.Slug });
```

### 3. **Program.cs** (logs mejorados)

Se agregaron logs detallados durante la configuración del HttpClient para Anthropic:

```csharp
builder.Services.AddHttpClient("AnthropicClient", (serviceProvider, client) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("🔧 [HTTP CLIENT] Configurando AnthropicClient...");
    logger.LogInformation("🔧 [HTTP CLIENT] BaseAddress: {BaseUrl}", baseUrl);
    logger.LogInformation("🔧 [HTTP CLIENT] API Key configurado (primeros 10 chars)...");
    logger.LogInformation("🔧 [HTTP CLIENT] Timeout configurado: {Timeout}s", timeout);
});
```

## 🔍 Cómo Verificar

### 1. Reiniciar la aplicación

Como la aplicación está en modo Debug, necesitas:
- **Detener el debugging** (Shift+F5)
- **Iniciar de nuevo** (F5)

O usar **Hot Reload** (Ctrl+F5) si está disponible.

### 2. Crear una pregunta

1. Ve a la página de preguntas
2. Crea una nueva pregunta
3. Observa los logs en la consola de Output

### 3. Logs esperados

Deberías ver una secuencia de logs como esta:

```
✅ [PAGE MODEL] Pregunta creada {PreguntaId} con slug '{Slug}'
🚀 [PAGE MODEL] Iniciando Job de IA para pregunta {PreguntaId}...
⚡ [TASK.RUN] Inicio del Task.Run para pregunta {PreguntaId}
⚡ [TASK.RUN] Creando scope para AiAnswerJob...
⚡ [TASK.RUN] Scope creado exitosamente
✅ [TASK.RUN] AiAnswerJob obtenido del DI, tipo: eiibd26.Jobs.AiAnswerJob
✅ [TASK.RUN] Ejecutando ProcesarPreguntaAsync para {PreguntaId}...
🎯 [AI Job] INICIADO para PreguntaId={PreguntaId}
🔍 [AI Job] Verificando si servicio está habilitado...
✅ [AI Job] Servicio habilitado
🔍 [AI Job] Cargando pregunta desde BD...
✅ [AI Job] Pregunta cargada: '{Titulo}'
🤖 [AI Job] Llamando a Claude API para generar respuesta...
Enviando solicitud a Claude API para pregunta {PreguntaId}
✅ [AI Job] Respuesta generada en X.XXs, ~XXX tokens
🛡️ [AI Job] Validando seguridad del contenido...
✅ [AI Job] Validación de seguridad APROBADA
💾 [AI Job] Guardando respuesta en BD...
🎉 [AI Job] COMPLETADO EXITOSAMENTE en X.XXs
✅ [TASK.RUN] Job de IA completado exitosamente
```

### 4. Verificar en la base de datos

Después de crear una pregunta, verifica:

1. **Tabla `Preguntas`**: debe tener `TieneRespuestaIA = 1` y `FechaGeneracionIA` con timestamp
2. **Tabla `Respuestas`**: debe haber una nueva respuesta con `EsIA = 1` y `ModeloIA = 'claude-haiku-4-5-20251001'`

## 🔧 Configuración Verificada

### appsettings.json

```json
"AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "sk-ant-api03-...",
    "Model": "claude-haiku-4-5-20251001",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "TimeoutSeconds": 30,
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ApiVersion": "2023-06-01",
    "SystemUserId": "50649075-660f-4431-9049-98c9e3ac6d73"
}
```

## 🚀 Próximos Pasos Recomendados

### 1. Instalar Hangfire (Producción)

La solución actual usa `Task.Factory.StartNew` que funciona pero no es ideal para producción. Se recomienda instalar Hangfire:

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
```

Descomentar las líneas marcadas con `// TODO: Uncomment after installing Hangfire packages`

### 2. Monitoreo

Considera agregar:
- **Application Insights** para monitoreo en Azure
- **Serilog** para logs estructurados
- **Dashboard de Hangfire** para ver jobs ejecutándose

### 3. Manejo de errores mejorado

- Implementar reintentos exponenciales
- Alertas cuando falla el job
- Fallback cuando Claude API está caída

## 📝 Notas Técnicas

### ¿Por qué Task.Factory.StartNew con LongRunning?

- `Task.Run` usa el thread pool, que puede causar bloqueos si hay muchos requests
- `TaskCreationOptions.LongRunning` crea un thread dedicado
- `.Unwrap()` es necesario porque `async` dentro de `Task.Factory.StartNew` devuelve `Task<Task>`

### ¿Por qué IServiceProvider en lugar de inyectar AiAnswerJob?

- `AiAnswerJob` es **Scoped** (necesita `ApplicationDbContext`)
- El controlador/page model es **Scoped** y dura solo durante el request
- El background task necesita su **propio scope** independiente del request

## 🐛 Troubleshooting

### Si no aparecen logs del job:

1. Verifica que `appsettings.json` tiene `"AiAnswer": { "Enabled": true }`
2. Revisa que el `SystemUserId` existe en la tabla `AspNetUsers`
3. Comprueba que la API key de Anthropic es válida

### Si aparece error "User not found":

Ejecuta este SQL para crear el usuario del sistema si no existe:

```sql
-- Verificar si existe
SELECT * FROM AspNetUsers WHERE Id = '50649075-660f-4431-9049-98c9e3ac6d73'

-- Si no existe, crearlo
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed)
VALUES 
('50649075-660f-4431-9049-98c9e3ac6d73', 'ia-claude@eiibd.com', 'IA-CLAUDE@EIIBD.COM', 
 'ia-claude@eiibd.com', 'IA-CLAUDE@EIIBD.COM', 1)
```

### Si aparece error 401 Unauthorized de Anthropic:

- Verifica que la API key es correcta en `appsettings.json`
- Comprueba que la cuenta de Anthropic tiene créditos disponibles
- Revisa que no estás en una IP bloqueada

---

**Fecha de implementación:** 2025-01-XX
**Desarrollador:** GitHub Copilot
**Status:** ✅ Completado
