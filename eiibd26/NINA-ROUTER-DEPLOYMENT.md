# 🚀 NINA Router - Instrucciones de Despliegue

## ✅ Estado Actual
- **Código**: ✅ Implementado y compilado
- **Base de Datos**: ⏳ Migración creada, pendiente aplicar

---

## 📋 Pasos para Activar el Sistema

### 1. Verificar Compilación (Ya hecho)
```bash
cd eiibd26
dotnet build
```
**Status**: ✅ **Build successful**

---

### 2. Aplicar Migración de Base de Datos

#### Opción A: Ambiente Local/Desarrollo
```bash
cd eiibd26
dotnet ef database update
```

#### Opción B: Ambiente de Producción
```bash
cd eiibd26

# Con connection string específica
dotnet ef database update --connection "Server=...;Database=...;User Id=...;Password=..."

# O usando appsettings.Production.json
dotnet ef database update --environment Production
```

#### Verificar migración aplicada
Ejecutar en SQL Server:
```sql
-- Verificar que la tabla existe
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'AI_Request_Log'

-- Si existe, debería mostrar 1 fila
```

---

### 3. Verificar Configuración

Revisar que `appsettings.json` (o `appsettings.Production.json`) tenga:

```json
{
  "AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "sk-ant-...",  // ⚠️ Debe estar configurado
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "TimeoutSeconds": 30,
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ApiVersion": "2023-06-01",
    "SystemUserId": "guid-valido"  // ⚠️ GUID del usuario sistema
  }
}
```

**IMPORTANTE**: 
- `AnthropicApiKey`: Debe tener API key válida de Anthropic
- `SystemUserId`: Debe ser GUID de un usuario existente en la BD

---

### 4. Iniciar Aplicación

```bash
cd eiibd26
dotnet run
```

O en Visual Studio:
- F5 (Run with Debugging)
- Ctrl+F5 (Run without Debugging)

---

### 5. Verificar que NINA Router está Activo

#### Test 1: Logs de inicio
Al iniciar la app, buscar en consola:
```
✅ AI Answer Services configured. Enabled: True
✅ Anthropic API Key configured
🔧 [HTTP CLIENT] Configurando AnthropicClient...
```

#### Test 2: Crear una pregunta de prueba
1. Iniciar sesión como usuario registrado
2. Crear una pregunta: "¿Qué es el VIH?"
3. Esperar 3 segundos
4. Verificar que aparece respuesta de IA con autoría NINA

#### Test 3: Verificar logging en BD
```sql
-- Ver últimas solicitudes procesadas
SELECT TOP 10 
    QuestionText,
    Level,
    HighRisk,
    ModelUsed,
    ProcessingTimeMs,
    Timestamp
FROM AI_Request_Log
ORDER BY Timestamp DESC
```

---

### 6. Testing con API (Solo Administradores)

#### Clasificar una pregunta
```bash
curl -X GET "https://localhost:5001/api/nina-test/classify?q=¿Qué%20es%20el%20VIH?" \
  -H "Authorization: Bearer {admin-token}"
```

**Respuesta esperada**:
```json
{
  "question": "¿Qué es el VIH?",
  "level": "Simple",
  "highRisk": false,
  "recommendedModel": "Modelo Base EIIBD"
}
```

#### Ver estadísticas
```bash
curl -X GET "https://localhost:5001/api/nina-test/stats" \
  -H "Authorization: Bearer {admin-token}"
```

**Respuesta esperada**:
```json
{
  "totalRequests": 5,
  "byModel": [...],
  "costOptimization": {
    "sonnetUsage": "40%",
    "economicModelsUsage": "60%",
    "estimatedSavings": "60% vs 100% Sonnet"
  }
}
```

---

## 🔍 Troubleshooting

### Problema: "Table 'AI_Request_Log' doesn't exist"
**Causa**: Migración no aplicada
**Solución**:
```bash
dotnet ef database update
```

### Problema: "401 Unauthorized" en /api/nina-test/*
**Causa**: No tienes rol de Administrador
**Solución**: Iniciar sesión con usuario que tenga rol "Administrador"

### Problema: Todas las preguntas usan Claude Sonnet
**Causa**: Clasificación está fallando
**Solución**:
1. Verificar logs: buscar `[NINA Router]`
2. Verificar API key de Anthropic está configurada
3. Verificar conexión a internet/API de Anthropic

### Problema: Error "Anthropic API key is not configured"
**Causa**: `AnthropicApiKey` no está en appsettings
**Solución**: Agregar clave válida en `appsettings.json`:
```json
"AiAnswer": {
  "AnthropicApiKey": "sk-ant-api03-..."
}
```

---

## ✅ Checklist de Validación

Post-despliegue, verificar:

- [ ] ✅ Build exitoso sin errores
- [ ] ✅ Migración aplicada (tabla `AI_Request_Log` existe)
- [ ] ✅ App inicia sin errores
- [ ] ✅ Logs muestran "AI Answer Services configured"
- [ ] ✅ Crear pregunta genera respuesta automática
- [ ] ✅ Respuesta incluye "**Autor:** NINA"
- [ ] ✅ Respuesta incluye "**Fuente:** {modelo}"
- [ ] ✅ Tabla `AI_Request_Log` se está poblando
- [ ] ✅ Endpoint `/api/nina-test/stats` retorna datos
- [ ] ✅ Pregunta simple usa Modelo Base EIIBD
- [ ] ✅ Pregunta con "sangre" o "fiebre" usa Claude Sonnet

---

## 📊 Monitoreo Post-Despliegue

### Primeras 24 horas
```sql
-- Ver distribución de modelos
SELECT 
    ModelUsed,
    COUNT(*) as Total,
    AVG(ProcessingTimeMs) as AvgTime
FROM AI_Request_Log
WHERE Success = 1
  AND Timestamp >= DATEADD(hour, -24, GETDATE())
GROUP BY ModelUsed

-- Calcular ahorro real
SELECT 
    COUNT(*) as TotalRequests,
    SUM(CASE WHEN ModelUsed LIKE '%sonnet%' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) as SonnetPercentage,
    100 - (SUM(CASE WHEN ModelUsed LIKE '%sonnet%' THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) as SavingsPercentage
FROM AI_Request_Log
WHERE Success = 1
  AND Timestamp >= DATEADD(hour, -24, GETDATE())
```

### Primera semana
- Revisar logs diariamente
- Ajustar palabras clave de riesgo si necesario
- Agregar respuestas pre-programadas para preguntas frecuentes
- Calcular ROI real vs proyectado

---

## 🎯 Éxito del Despliegue

El despliegue es exitoso si:

✅ **Funcionalidad**: Preguntas son respondidas automáticamente  
✅ **Optimización**: Uso de Sonnet < 60%  
✅ **Transparencia**: Autoría NINA visible en todas las respuestas  
✅ **Logging**: Tabla AI_Request_Log se está poblando  
✅ **Sin errores**: No hay errores en logs relacionados a NINA Router  

---

## 📞 Soporte

Si encuentras problemas:
1. Revisar logs: buscar `[NINA Router]` o `[AI Job]`
2. Consultar documentación: `NINA-ROUTER-DOCUMENTATION.md`
3. Revisar testing guide: `NINA-ROUTER-TESTING-GUIDE.md`
4. Verificar tabla SQL: `SELECT * FROM AI_Request_Log WHERE Success = 0`

---

## 🚀 Comando Único de Despliegue

Si todo está configurado correctamente:

```bash
# Desde el directorio raíz del proyecto
cd eiibd26
dotnet ef database update
dotnet run
```

---

**¡Listo para producción!** 🎉

El sistema NINA Router comenzará a optimizar automáticamente el uso de modelos de IA desde la primera pregunta procesada.
