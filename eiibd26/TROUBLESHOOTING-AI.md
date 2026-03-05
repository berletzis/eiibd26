# 🔧 TROUBLESHOOTING: Respuestas de IA no se generan

## ✅ CAMBIO IMPLEMENTADO

Se ha modificado `PreguntasApiController.cs` para ejecutar el job de IA **directamente** sin necesidad de Hangfire como solución temporal.

### Código agregado:
```csharp
// Fire-and-Forget: ejecuta el job en segundo plano
_ = Task.Run(async () =>
{
    try
    {
        using var scope = _serviceProvider.CreateScope();
        var aiJob = scope.ServiceProvider.GetRequiredService<eiibd26.Jobs.AiAnswerJob>();
        await aiJob.ProcesarPreguntaAsync(pregunta.Id);
        _logger.LogInformation("Job de IA completado para pregunta {PreguntaId}", pregunta.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error ejecutando job de IA para pregunta {PreguntaId}", pregunta.Id);
    }
});
```

---

## 📋 CHECKLIST DE DIAGNÓSTICO

### 1. ✅ Verificar que la aplicación está ejecutándose

```bash
# Reiniciar la aplicación para que tome el nuevo código
cd eiibd26
dotnet build
dotnet run
```

### 2. ✅ Ejecutar script de diagnóstico

```sql
-- En SQL Server Management Studio, ejecuta:
-- eiibd26\DEBUG-AI-STATUS.sql
```

Este script verificará:
- ✅ Usuario sistema existe y está configurado correctamente
- ✅ Campos de migración existen en la base de datos
- ✅ Preguntas pendientes de respuesta IA
- ✅ Respuestas IA ya generadas (si existen)

### 3. ✅ Verificar configuración en appsettings.json

```json
{
  "AiAnswer": {
    "Enabled": true,  // ← DEBE SER true
    "AnthropicApiKey": "sk-ant-api03-...",  // ← Debe tener tu API key real
    "SystemUserId": "50649075-660F-4431-9049-98C9E3AC6D73"  // ← GUID del usuario sistema
  }
}
```

**Verificar:**
- [ ] `Enabled` está en `true`
- [ ] `AnthropicApiKey` NO es el placeholder "ANTHROPIC_API_KEY_AQUI"
- [ ] `SystemUserId` coincide con el GUID en la base de datos (ejecuta `SETUP-SYSTEM-USER.sql` si no lo has hecho)

### 4. ✅ Verificar logs de la aplicación

Busca en la consola o logs:

**✅ LOGS ESPERADOS (Todo bien):**
```
✅ AI Answer Services configured. Enabled: True
✅ Anthropic API Key configured
✅ System User ID configured: 50649075-660F-4431-9049-98C9E3AC6D73
...
[Information] Pregunta creada {PreguntaId}. Job de IA iniciado en segundo plano
[Information] Generando respuesta de IA para pregunta {PreguntaId}
[Metrics] AI Generation Complete: PreguntaId={PreguntaId}, DurationSeconds=3.5, EstimatedTokens=450
[Metrics] Safety Check Passed: PreguntaId={PreguntaId}
[Information] Job de IA completado para pregunta {PreguntaId}
```

**❌ LOGS DE ERROR (Problemas):**
```
⚠️ Anthropic API Key NOT configured or using placeholder
⚠️ System User ID NOT configured (using empty GUID)
[Error] Anthropic API key is not configured
[Error] Error en llamada a Claude API. Status: 401 (Unauthorized)
[Warning] Pregunta {PreguntaId} no encontrada o eliminada
```

### 5. ✅ Probar creando una nueva pregunta

1. **Inicia sesión** en la aplicación
2. **Crea una nueva pregunta**
3. **Espera 10-30 segundos** (el job se ejecuta en segundo plano)
4. **Recarga la página** de la pregunta
5. **Verifica** que aparezca una respuesta marcada como "IA" o con un badge "Asistente IA"

---

## 🐛 PROBLEMAS COMUNES Y SOLUCIONES

### ❌ Problema: "AI Answer service is disabled in configuration"

**Solución:**
```json
// En appsettings.json
"AiAnswer": {
  "Enabled": true  // ← Cambiar a true
}
```

### ❌ Problema: "La clave API de Anthropic no está configurada"

**Solución:**
1. Obtén tu API key de: https://console.anthropic.com/
2. Agrégala en `appsettings.json`:
```json
"AiAnswer": {
  "AnthropicApiKey": "sk-ant-api03-TU_API_KEY_REAL"
}
```

### ❌ Problema: "Usuario sistema no encontrado"

**Solución:**
```sql
-- Ejecuta en SQL Server:
-- eiibd26\SETUP-SYSTEM-USER.sql

-- Copia el GUID que te muestra y agrégalo en appsettings.json:
"AiAnswer": {
  "SystemUserId": "GUID_QUE_COPIASTE"
}
```

### ❌ Problema: "Error 401 Unauthorized" al llamar a Claude API

**Causas:**
- API key inválida o expirada
- API key no tiene permisos

**Solución:**
1. Verifica tu API key en https://console.anthropic.com/
2. Genera una nueva si es necesario
3. Actualiza `appsettings.json`

### ❌ Problema: "Error 429 Too Many Requests"

**Causa:** Has excedido el límite de rate de la API de Anthropic

**Solución:**
- Espera unos minutos antes de crear nuevas preguntas
- Verifica tu plan en https://console.anthropic.com/

### ❌ Problema: La respuesta IA no aparece

**Posibles causas:**
1. **El job aún está ejecutándose** → Espera 10-30 segundos y recarga
2. **La pregunta ya tenía respuestas humanas** → La IA no genera respuesta si ya hay respuestas humanas
3. **Error en el job** → Revisa los logs en la consola

**Verificación:**
```sql
-- Ejecuta en SQL Server:
SELECT TOP 1 
    p.Id,
    p.Titulo,
    p.TieneRespuestaIA,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0) AS TotalRespuestas
FROM Preguntas p
WHERE p.Eliminado = 0
ORDER BY p.FechaCreacion DESC;
```

### ❌ Problema: "Timeout al generar respuesta de IA"

**Solución:**
```json
// En appsettings.json, aumenta el timeout:
"AiAnswer": {
  "TimeoutSeconds": 60  // ← Aumentar de 30 a 60 segundos
}
```

---

## 🔍 VERIFICACIÓN PASO A PASO

### Test 1: Verificar que el servicio está registrado

```bash
# Busca en los logs del startup:
✅ AI Answer Services configured. Enabled: True
✅ Anthropic API Key configured
✅ System User ID configured
```

### Test 2: Verificar base de datos

```sql
-- Ejecuta: eiibd26\DEBUG-AI-STATUS.sql
-- Debe mostrar:
-- ✅ Usuario sistema existe
-- ✅ Campos de migración existen
-- ✅ Lista de preguntas sin respuesta IA
```

### Test 3: Crear pregunta de prueba

```
Título: "¿Qué es la colitis ulcerosa?"
Cuerpo: "Me gustaría entender mejor esta condición y sus síntomas principales."
```

Espera 10-30 segundos y verifica:
- [ ] La pregunta existe en la BD
- [ ] Se creó una respuesta con `EsIA = 1`
- [ ] El campo `Pregunta.TieneRespuestaIA = 1`
- [ ] La respuesta tiene contenido coherente

---

## 📊 LOGS DE MÉTRICAS

El sistema genera logs detallados que puedes usar para monitoreo:

```
[Metrics] AI Job Started: PreguntaId={PreguntaId}, Timestamp={Timestamp}
[Metrics] AI Generation Complete: PreguntaId={PreguntaId}, DurationSeconds=3.5, EstimatedTokens=450
[Metrics] Safety Check Passed: PreguntaId={PreguntaId}
```

O en caso de bloqueo de seguridad:
```
[Metrics] Safety Check BLOCKED: PreguntaId={PreguntaId}, Reason=UnsafeContent
```

---

## 🚀 PRÓXIMO PASO: INSTALAR HANGFIRE (RECOMENDADO)

La solución actual usa `Task.Run` que funciona pero **NO es ideal para producción** porque:
- ❌ No hay reintentos automáticos si falla
- ❌ Se pierde el job si la app se reinicia
- ❌ No hay dashboard para monitorear

### Instalar Hangfire (recomendado):

```bash
cd eiibd26
dotnet add package Hangfire.Core --version 1.8.12
dotnet add package Hangfire.SqlServer --version 1.8.12
dotnet add package Hangfire.AspNetCore --version 1.8.12
```

Luego descomentar el código en:
- `Program.cs` (configuración de Hangfire)
- `PreguntasApiController.cs` (usar `_backgroundJobClient.Enqueue`)

---

## 📞 SOPORTE

Si después de seguir todos estos pasos el problema persiste:

1. Ejecuta `DEBUG-AI-STATUS.sql` y captura el resultado
2. Captura los logs de la aplicación (últimas 50 líneas)
3. Verifica el contenido de `appsettings.json` (sin mostrar la API key completa)
4. Reporta el issue con toda esta información
