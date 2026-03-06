# NINA Router - Guía de Pruebas y Testing

## 🧪 Endpoints de Prueba

El sistema incluye un controlador de pruebas para administradores: `NinaRouterTestController`

**Base URL**: `/api/nina-test`
**Autenticación**: Requiere rol `Administrador`

---

## 📋 Endpoints Disponibles

### 1. Clasificar Pregunta (Sin generar respuesta)

**Endpoint**: `GET /api/nina-test/classify?q={pregunta}`

**Descripción**: Clasifica una pregunta y detecta riesgo sin generar respuesta completa.

**Ejemplo**:
```bash
curl -X GET "https://localhost:5001/api/nina-test/classify?q=¿Qué%20es%20el%20VIH?" \
  -H "Authorization: Bearer {token}"
```

**Respuesta**:
```json
{
  "question": "¿Qué es el VIH?",
  "level": "Simple",
  "highRisk": false,
  "recommendedModel": "Modelo Base EIIBD"
}
```

**Casos de prueba sugeridos**:
```
Simple:
  ¿Qué es el VIH?
  ¿Cómo se transmite el VIH?
  
Media:
  ¿Por qué es importante el tratamiento antirretroviral?
  ¿Cuál es la diferencia entre VIH y SIDA?
  
Compleja:
  Estoy tomando Truvada y tengo náuseas, ¿qué hago?
  ¿Debo cambiar mi medicamento si tengo efectos secundarios?
  
Alto Riesgo:
  Tengo fiebre y sangre en la orina, ¿es urgente?
  Vomito sangre, ¿debo ir al hospital?
```

---

### 2. Obtener Estadísticas de Uso

**Endpoint**: `GET /api/nina-test/stats`

**Descripción**: Retorna métricas de uso del sistema NINA Router.

**Ejemplo**:
```bash
curl -X GET "https://localhost:5001/api/nina-test/stats" \
  -H "Authorization: Bearer {token}"
```

**Respuesta**:
```json
{
  "totalRequests": 150,
  "byModel": [
    {
      "model": "Claude Haiku",
      "count": 68,
      "percentage": 45.33,
      "avgTimeMs": 1250.5
    },
    {
      "model": "Claude Sonnet",
      "count": 60,
      "percentage": 40.0,
      "avgTimeMs": 2100.2
    },
    {
      "model": "Modelo Base EIIBD",
      "count": 22,
      "percentage": 14.67,
      "avgTimeMs": 10.1
    }
  ],
  "byLevel": [
    {
      "level": "Medium",
      "count": 68,
      "percentage": 45.33
    },
    {
      "level": "Complex",
      "count": 60,
      "percentage": 40.0
    },
    {
      "level": "Simple",
      "count": 22,
      "percentage": 14.67
    }
  ],
  "highRisk": {
    "count": 5,
    "percentage": 3.33
  },
  "costOptimization": {
    "sonnetUsage": "40%",
    "economicModelsUsage": "60%",
    "estimatedSavings": "60% vs 100% Sonnet"
  }
}
```

**Métricas clave**:
- **byModel**: Distribución por modelo usado
- **byLevel**: Distribución por nivel de complejidad
- **highRisk**: Cantidad de preguntas de alto riesgo
- **costOptimization**: Ahorro estimado vs usar siempre Sonnet

---

### 3. Ver Solicitudes Recientes

**Endpoint**: `GET /api/nina-test/recent`

**Descripción**: Muestra las últimas 20 solicitudes procesadas.

**Ejemplo**:
```bash
curl -X GET "https://localhost:5001/api/nina-test/recent" \
  -H "Authorization: Bearer {token}"
```

**Respuesta**:
```json
{
  "count": 20,
  "requests": [
    {
      "id": "guid-1",
      "preguntaId": "guid-pregunta",
      "questionPreview": "¿Qué es el VIH? Es un virus que...",
      "level": "Simple",
      "highRisk": false,
      "modelUsed": "Modelo Base EIIBD",
      "processingTimeSeconds": 0.01,
      "success": true,
      "errorMessage": null,
      "timestamp": "2025-01-20T10:30:00Z"
    }
  ]
}
```

---

### 4. Simular Pregunta (Sin guardar en BD)

**Endpoint**: `POST /api/nina-test/simulate`

**Descripción**: Procesa una pregunta completa sin guardar en base de datos. Útil para testing.

**Body**:
```json
{
  "titulo": "¿Qué es el VIH?",
  "cuerpo": "Necesito información básica sobre el VIH",
  "contexto": "Condiciones: ninguna, Síntomas: ninguno"
}
```

**Ejemplo**:
```bash
curl -X POST "https://localhost:5001/api/nina-test/simulate" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "titulo": "Tengo fiebre alta",
    "cuerpo": "Llevo 3 días con fiebre de 39 grados",
    "contexto": null
  }'
```

**Respuesta**:
```json
{
  "simulation": true,
  "input": {
    "titulo": "Tengo fiebre alta",
    "cuerpo": "Llevo 3 días con fiebre de 39 grados",
    "contexto": null
  },
  "decision": {
    "level": "Complex",
    "highRisk": true,
    "modelUsed": "claude-sonnet-4.5-20250514",
    "processingTimeSeconds": 2.35,
    "totalTimeSeconds": 2.37
  },
  "response": {
    "author": "NINA",
    "contentPreview": "Es importante que consultes con un profesional...",
    "contentLength": 850
  }
}
```

---

## 🧪 Casos de Prueba Completos

### Test 1: Pregunta Simple
```bash
POST /api/nina-test/simulate
{
  "titulo": "¿Qué es el VIH?",
  "cuerpo": "Necesito información básica"
}

# Esperado:
# - level: Simple
# - highRisk: false
# - modelUsed: Modelo Base EIIBD
# - processingTime: < 0.1s
```

### Test 2: Pregunta Media
```bash
POST /api/nina-test/simulate
{
  "titulo": "¿Cómo funcionan los antirretrovirales?",
  "cuerpo": "Quiero entender el mecanismo de acción"
}

# Esperado:
# - level: Medium
# - highRisk: false
# - modelUsed: Claude Haiku
# - processingTime: 1-3s
```

### Test 3: Pregunta Compleja
```bash
POST /api/nina-test/simulate
{
  "titulo": "Efectos secundarios de mi medicamento",
  "cuerpo": "Estoy tomando Truvada y tengo náuseas constantes"
}

# Esperado:
# - level: Complex
# - highRisk: false
# - modelUsed: Claude Sonnet
# - processingTime: 2-5s
```

### Test 4: Alto Riesgo (Palabra Clave: "sangre")
```bash
POST /api/nina-test/simulate
{
  "titulo": "Sangre en la orina",
  "cuerpo": "He notado sangre al orinar desde ayer"
}

# Esperado:
# - level: Complex (puede variar)
# - highRisk: true
# - modelUsed: Claude Sonnet (obligatorio)
# - Detecta keyword: "sangre"
```

### Test 5: Alto Riesgo (Múltiples Keywords)
```bash
POST /api/nina-test/simulate
{
  "titulo": "Urgencia médica",
  "cuerpo": "Tengo fiebre muy alta y dolor en el pecho, ¿debo ir al hospital?"
}

# Esperado:
# - highRisk: true (fiebre, pecho, hospital)
# - modelUsed: Claude Sonnet
```

---

## 📊 Consultas SQL Útiles

### Ver distribución de modelos
```sql
SELECT 
    ModelUsed,
    COUNT(*) as Total,
    AVG(ProcessingTimeMs) as AvgTimeMs
FROM AI_Request_Log
WHERE Success = 1
GROUP BY ModelUsed
ORDER BY Total DESC
```

### Calcular ahorro vs 100% Sonnet
```sql
SELECT 
    COUNT(*) as TotalRequests,
    SUM(CASE WHEN ModelUsed LIKE '%sonnet%' THEN 1 ELSE 0 END) as SonnetCount,
    SUM(CASE WHEN ModelUsed LIKE '%sonnet%' THEN 0 ELSE 1 END) as EconomicCount,
    CAST(SUM(CASE WHEN ModelUsed LIKE '%sonnet%' THEN 0 ELSE 1 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as SavingsPercentage
FROM AI_Request_Log
WHERE Success = 1
```

### Preguntas de alto riesgo
```sql
SELECT 
    QuestionText,
    ModelUsed,
    Level,
    ProcessingTimeMs,
    Timestamp
FROM AI_Request_Log
WHERE HighRisk = 1
ORDER BY Timestamp DESC
```

### Errores y fallos
```sql
SELECT 
    QuestionText,
    ErrorMessage,
    Timestamp
FROM AI_Request_Log
WHERE Success = 0
ORDER BY Timestamp DESC
```

---

## 🔍 Testing con Postman

### Colección de Postman

1. **Configurar variable de entorno**:
   - `baseUrl`: `https://localhost:5001`
   - `token`: Token JWT de administrador

2. **Request: Classify Simple**
   ```
   GET {{baseUrl}}/api/nina-test/classify?q=¿Qué es el VIH?
   Headers:
     Authorization: Bearer {{token}}
   ```

3. **Request: Get Stats**
   ```
   GET {{baseUrl}}/api/nina-test/stats
   Headers:
     Authorization: Bearer {{token}}
   ```

4. **Request: Simulate Question**
   ```
   POST {{baseUrl}}/api/nina-test/simulate
   Headers:
     Authorization: Bearer {{token}}
     Content-Type: application/json
   Body:
     {
       "titulo": "Test question",
       "cuerpo": "Test body",
       "contexto": null
     }
   ```

---

## 🎯 Checklist de Validación

- [ ] Pregunta simple → Usa Modelo Base EIIBD
- [ ] Pregunta media → Usa Claude Haiku
- [ ] Pregunta compleja → Usa Claude Sonnet
- [ ] Palabra clave de riesgo → Fuerza Claude Sonnet
- [ ] Autoría NINA presente en todas las respuestas
- [ ] Registro correcto en AI_Request_Log
- [ ] Estadísticas muestran ahorro > 60%
- [ ] Tiempos de respuesta aceptables:
  - Simple: < 0.1s
  - Medium: 1-3s
  - Complex: 2-5s

---

## 🐛 Troubleshooting

### Error: "401 Unauthorized"
**Causa**: No tienes rol de Administrador
**Solución**: Asegúrate de estar autenticado con un usuario que tenga rol "Administrador"

### Error: "No hay datos todavía" en /stats
**Causa**: No se han procesado preguntas con NINA Router aún
**Solución**: Crea algunas preguntas y espera a que `AiAnswerJob` las procese

### Todas las preguntas usan Claude Sonnet
**Causa**: Clasificación está fallando
**Solución**: 
1. Revisar logs de aplicación
2. Verificar que Claude Haiku API está funcionando
3. Revisar configuración de `AnthropicApiKey`

### Respuestas simples no están siendo detectadas
**Causa**: Prompt de clasificación necesita ajuste
**Solución**: Revisar y ajustar prompt en `NinaModelRouterService.ClassifyQuestionAsync()`

---

## 📈 Monitoreo en Producción

### Logs importantes
```bash
# Clasificaciones
grep "Nivel detectado" app.log | tail -20

# Modelos usados
grep "usando" app.log | grep "NINA Router" | tail -20

# Errores
grep "ERROR" app.log | grep "NINA" | tail -20
```

### Alertas recomendadas
1. **Alto uso de Sonnet**: Si > 60%, investigar por qué no se está optimizando
2. **Errores de clasificación**: Si > 5%, ajustar lógica
3. **Tiempo de respuesta alto**: Si promedio > 5s, investigar

---

## 🎉 Conclusión

Con estos endpoints de testing puedes:
- ✅ Validar clasificación de preguntas
- ✅ Ver métricas de ahorro en tiempo real
- ✅ Probar escenarios sin afectar BD
- ✅ Monitorear rendimiento del sistema

**Próximo paso**: Realizar pruebas con casos reales y ajustar según necesidad.
