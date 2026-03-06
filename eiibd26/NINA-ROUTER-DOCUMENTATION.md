# Sistema NINA Router - Documentación Técnica

## Resumen Ejecutivo

El **NINA Router** es un sistema de decisión inteligente que selecciona automáticamente el modelo de IA más apropiado para responder cada pregunta, optimizando costos sin comprometer la calidad.

### Objetivo Principal
Reducir el uso de Claude Sonnet (modelo premium) en al menos un **60%**, utilizando modelos más económicos para preguntas simples y medias.

---

## Arquitectura del Sistema

### Componentes Principales

#### 1. **QuestionLevel** (Enum)
Define los niveles de complejidad de preguntas:
- `Simple`: Pregunta informativa general
- `Medium`: Requiere explicación contextual
- `Complex`: Incluye síntomas personales, medicamentos o decisiones médicas

**Ubicación**: `eiibd26/Models/AI/QuestionLevel.cs`

#### 2. **AIResponse** (Modelo)
Respuesta enriquecida con metadata completa:
```csharp
{
    Content: string,           // Contenido de la respuesta
    Author: "NINA",            // Autor fijo
    Source: string,            // Modelo utilizado
    Level: QuestionLevel,      // Nivel detectado
    HighRisk: bool,           // Riesgo médico alto
    ProcessingTimeSeconds: double,
    GeneratedAt: DateTimeOffset
}
```

**Ubicación**: `eiibd26/Models/AI/AIResponse.cs`

#### 3. **IAIModelRouter** (Interfaz)
Contrato del servicio de enrutamiento:
- `AskAsync()`: Procesa pregunta y retorna respuesta con modelo apropiado
- `ClassifyQuestionAsync()`: Clasifica complejidad de pregunta
- `DetectHighRisk()`: Detecta riesgo médico por palabras clave

**Ubicación**: `eiibd26/Services/AI/IAIModelRouter.cs`

#### 4. **NinaModelRouterService** (Implementación)
Servicio principal que implementa la lógica de decisión.

**Ubicación**: `eiibd26/Services/AI/NinaModelRouterService.cs`

**Modelos soportados**:
- `Claude Sonnet 4.5` (Premium) - Preguntas complejas y alto riesgo
- `Claude Haiku 3.5` (Económico) - Preguntas de complejidad media
- `Modelo Base EIIBD` (Local/Gratis) - Preguntas simples con respuestas pre-programadas

#### 5. **AIRequestLog** (Modelo de BD)
Tabla de auditoría para análisis de métricas.

**Ubicación**: `eiibd26/Models/AIRequestLog.cs`

**Campos principales**:
- `PreguntaId`, `QuestionText`, `Level`, `HighRisk`, `ModelUsed`, `ProcessingTimeMs`, `Success`, `ErrorMessage`

---

## Flujo de Decisión

```
Usuario envía pregunta
      ↓
NINA Router recibe pregunta
      ↓
[1] Detectar Riesgo Médico (Local - Palabras clave)
      ↓
¿Alto Riesgo? → Sí → Claude Sonnet (obligatorio)
      ↓ No
[2] Clasificar Complejidad (Claude Haiku - 10 tokens)
      ↓
┌─────────────┬──────────────┬──────────────┐
│   SIMPLE    │    MEDIA     │   COMPLEJA   │
│  Respuestas │ Claude Haiku │Claude Sonnet │
│Pre-programas│  (económico) │  (premium)   │
└─────────────┴──────────────┴──────────────┘
      ↓
[3] Generar respuesta con modelo seleccionado
      ↓
[4] Validar seguridad (IAiSafetyService)
      ↓
[5] Enriquecer con autoría NINA
      ↓
[6] Guardar en BD
      ↓
[7] Registrar en AI_Request_Log
```

---

## Detección de Alto Riesgo

### Palabras Clave (Lista Extensible)
```csharp
"sangre", "fiebre", "dolor fuerte", "urgencias", "hospital",
"efecto secundario", "empeoró", "grave", "mortal", "muerte",
"suicidio", "emergencia", "intoxicación", "sobredosis",
"convulsión", "desmayo", "inconsciente", "pecho", "corazón",
"respirar", "ahogo", "asfixia", "mareo severo", "vomito sangre"
```

**Regla**: Si se detecta cualquier palabra clave → `HighRisk = true` → **Claude Sonnet obligatoriamente**

---

## Clasificación de Preguntas

### Prompt para Claude Haiku
```
Analiza la siguiente pregunta y clasifícala según su complejidad:

SIMPLE: pregunta informativa general sobre VIH (¿Qué es...? ¿Cómo se define...?)
MEDIA: requiere explicación contextual (¿Por qué...? ¿Cómo funciona...?)
COMPLEJA: incluye síntomas personales, medicamentos específicos, decisiones médicas

Responde SOLO con una palabra: SIMPLE, MEDIA o COMPLEJA.

Pregunta: "{question}"
```

**Configuración**:
- Modelo: `claude-3-5-haiku-20241022`
- MaxTokens: `10` (solo necesitamos una palabra)
- Temperature: `0.0` (determinista)

---

## Generación de Respuestas por Nivel

### SIMPLE (Modelo Base EIIBD)
Respuestas pre-programadas almacenadas localmente en `NinaModelRouterService.GenerarRespuestaSimple()`.

**Ejemplos**:
- "¿Qué es el VIH?"
- "¿Cómo se transmite el VIH?"

**Ventajas**:
- ⚡ Respuesta instantánea
- 💰 Costo cero
- ✅ Respuestas consistentes y verificadas

### MEDIA (Claude Haiku)
Modelo económico para explicaciones contextuales.

**Configuración**:
- MaxTokens: `500`
- Temperature: `0.3`

**Costo estimado**: ~60% más barato que Sonnet

### COMPLEJA (Claude Sonnet)
Modelo premium para casos complejos.

**Configuración**:
- MaxTokens: `600`
- Temperature: `0.3`

---

## Autoría NINA

Todas las respuestas incluyen metadata al final:

```markdown
---
**Autor:** NINA  
**Fuente:** {Modelo Utilizado}
```

**Ejemplos**:
```
Autor: NINA
Fuente: Claude Sonnet

Autor: NINA
Fuente: Claude Haiku

Autor: NINA
Fuente: Modelo Base EIIBD
```

---

## Logging y Métricas

### Tabla: AI_Request_Log

Registra cada solicitud procesada para análisis posterior.

**Consultas útiles**:

#### Distribución de modelos usados
```sql
SELECT ModelUsed, COUNT(*) as Total
FROM AI_Request_Log
WHERE Success = 1
GROUP BY ModelUsed
ORDER BY Total DESC
```

#### Tasa de reducción de Claude Sonnet
```sql
SELECT 
    COUNT(CASE WHEN ModelUsed = 'claude-sonnet-4.5-20250514' THEN 1 END) * 100.0 / COUNT(*) as PorcentajeSonnet,
    COUNT(CASE WHEN ModelUsed != 'claude-sonnet-4.5-20250514' THEN 1 END) * 100.0 / COUNT(*) as PorcentajeEconomico
FROM AI_Request_Log
WHERE Success = 1
```

#### Tiempo promedio por modelo
```sql
SELECT ModelUsed, AVG(ProcessingTimeMs) as AvgTimeMs
FROM AI_Request_Log
WHERE Success = 1
GROUP BY ModelUsed
```

#### Preguntas de alto riesgo
```sql
SELECT QuestionText, ModelUsed, Level
FROM AI_Request_Log
WHERE HighRisk = 1
ORDER BY Timestamp DESC
```

---

## Configuración

### appsettings.json

No requiere cambios adicionales. Usa la configuración existente de `AiAnswer`:

```json
{
  "AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "TimeoutSeconds": 30,
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ApiVersion": "2023-06-01",
    "SystemUserId": "guid-del-usuario-sistema"
  }
}
```

### Registro en Program.cs

```csharp
// NINA Router: Sistema de decisión inteligente de modelo IA
builder.Services.AddSingleton<IAIModelRouter, NinaModelRouterService>();
```

---

## Integración con AiAnswerJob

El job ahora usa `IAIModelRouter` en lugar de `IAiAnswerService` directamente:

**Antes**:
```csharp
var contenido = await _aiAnswerService.GenerarRespuestaAsync(pregunta, ...);
```

**Ahora**:
```csharp
AIResponse aiResponse = await _ninaRouter.AskAsync(pregunta, ...);
// aiResponse contiene: Content, Source, Level, HighRisk, ProcessingTimeSeconds
```

---

## Migración de Base de Datos

Crear la tabla `AI_Request_Log`:

```bash
dotnet ef migrations add AddNinaRouterLogging --project eiibd26
dotnet ef database update --project eiibd26
```

---

## Métricas Esperadas

### Antes de NINA Router
- **100%** de preguntas → Claude Sonnet
- Costo alto
- Sin visibilidad de complejidad

### Después de NINA Router (Estimado)
- **15%** preguntas simples → Modelo Base EIIBD (gratis)
- **45%** preguntas medias → Claude Haiku (60% más barato)
- **40%** preguntas complejas/riesgo → Claude Sonnet

**Reducción total de costos**: ~65%

---

## Extensibilidad

### Agregar nuevos modelos

1. Agregar constante en `NinaModelRouterService`:
```csharp
private const string ModelNuevo = "nuevo-modelo-id";
```

2. Agregar caso en switch de `AskAsync()`:
```csharp
case QuestionLevel.Nueva:
    contenidoRespuesta = await GenerarRespuestaConNuevoModelo(...);
    modelUsado = ModelNuevo;
    break;
```

### Agregar palabras clave de riesgo

Modificar `HighRiskKeywords` en `NinaModelRouterService.cs`:
```csharp
private static readonly HashSet<string> HighRiskKeywords = new(StringComparer.OrdinalIgnoreCase)
{
    "sangre", "fiebre", ..., "nueva-palabra-clave"
};
```

### Mejorar respuestas simples

Editar método `GenerarRespuestaSimple()` en `NinaModelRouterService.cs`.

---

## Testing

### Probar clasificación manual
```csharp
var router = serviceProvider.GetRequiredService<IAIModelRouter>();

// Pregunta simple
var nivel1 = await router.ClassifyQuestionAsync("¿Qué es el VIH?");
// Esperado: QuestionLevel.Simple

// Pregunta compleja
var nivel2 = await router.ClassifyQuestionAsync("Tengo fiebre y dolor, ¿debo ir al hospital?");
// Esperado: QuestionLevel.Complex (por "fiebre" y "hospital")
```

### Probar detección de riesgo
```csharp
var router = serviceProvider.GetRequiredService<IAIModelRouter>();

bool riesgo1 = router.DetectHighRisk("Tengo sangre en la orina");
// Esperado: true

bool riesgo2 = router.DetectHighRisk("¿Qué es el VIH?");
// Esperado: false
```

---

## Mantenimiento

### Revisar logs
```bash
# Buscar decisiones de NINA Router en logs
grep "NINA Router" /logs/app.log

# Ver distribución de modelos
grep "usando" /logs/app.log | sort | uniq -c
```

### Ajustar clasificación
Si la clasificación automática no es precisa:
1. Revisar tabla `AI_Request_Log`
2. Identificar casos mal clasificados
3. Ajustar prompt de clasificación en `ClassifyQuestionAsync()`
4. Considerar agregar palabras clave específicas

---

## Troubleshooting

### Problema: Todas las preguntas usan Claude Sonnet
**Causa**: Clasificación falla y defaultea a `Complex`
**Solución**: Revisar logs de Claude Haiku, verificar API key

### Problema: Respuestas simples no son suficientes
**Causa**: Pocas respuestas pre-programadas
**Solución**: Agregar más casos en `GenerarRespuestaSimple()`

### Problema: Alto porcentaje de "alto riesgo"
**Causa**: Palabras clave muy sensibles
**Solución**: Revisar `HighRiskKeywords`, considerar hacer la detección más específica

---

## Conclusión

El sistema **NINA Router** implementa una arquitectura extensible, auditable y de bajo costo que:

✅ Optimiza automáticamente costos de IA  
✅ Mantiene calidad en respuestas complejas  
✅ Proporciona transparencia total (autoría NINA)  
✅ Registra métricas para análisis continuo  
✅ Es fácilmente extensible para nuevos modelos  

**Objetivo cumplido**: Reducción de uso de Claude Sonnet ≥ 60%
