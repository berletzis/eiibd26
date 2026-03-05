# 🚀 GUÍA DE INSTALACIÓN - SISTEMA AI FIRST ANSWER

## ✅ CHECKLIST DE INSTALACIÓN

### PASO 1: Instalar paquetes NuGet
```bash
cd eiibd26
dotnet add package Hangfire.Core --version 1.8.12
dotnet add package Hangfire.SqlServer --version 1.8.12
dotnet add package Hangfire.AspNetCore --version 1.8.12
```

### PASO 2: Ejecutar migraciones de base de datos

**Opción A: Usando EF Core Migrations**
```bash
dotnet ef migrations add AddAiAnswerFields
dotnet ef database update
```

**Opción B: Ejecutar SQL manualmente**
1. Abre SQL Server Management Studio
2. Ejecuta el contenido de: `MIGRATION-AI-FIELDS.sql`

### PASO 3: Crear usuario sistema

1. Ejecuta el script: `SETUP-SYSTEM-USER.sql`
2. Copia el GUID generado (ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`)

### PASO 4: Configurar appsettings.json

Agrega la configuración de AI (ver `INSTALL-APPSETTINGS-AI.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  
  "AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "tu-api-key-de-anthropic",
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "TimeoutSeconds": 30,
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ApiVersion": "2023-06-01",
    "SystemUserId": "PEGA-AQUI-EL-GUID-DEL-PASO-3",
    "ForbiddenPhrases": [
      "aumenta la dosis",
      "suspende el medicamento",
      "tienes cáncer"
    ]
  }
}
```

**IMPORTANTE:** 
- Reemplaza `tu-api-key-de-anthropic` con tu API key real
- Reemplaza `SystemUserId` con el GUID del Paso 3

### PASO 5: Modificar Program.cs

Agrega las líneas de configuración de AI (ver `INSTALL-AI-PROGRAM-CS.txt`):

Busca la línea: `var app = builder.Build();`

**ANTES de esa línea**, agrega:

```csharp
// ===== AI SERVICES CONFIGURATION =====
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.SqlServer;

// Configurar opciones de IA
builder.Services.Configure<eiibd26.Configuration.AiAnswerConfiguration>(
    builder.Configuration.GetSection("AiAnswer"));

// Registrar HttpClient para Anthropic
builder.Services.AddHttpClient("AnthropicClient", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IOptions<eiibd26.Configuration.AiAnswerConfiguration>>().Value;
    client.BaseAddress = new Uri(config.ApiBaseUrl);
    client.DefaultRequestHeaders.Add("x-api-key", config.AnthropicApiKey);
    client.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion);
    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
});

// Registrar servicios de IA
builder.Services.AddScoped<eiibd26.Services.AI.IAiAnswerService, eiibd26.Services.AI.AiAnswerService>();
builder.Services.AddScoped<eiibd26.Services.AI.IAiSafetyService, eiibd26.Services.AI.AiSafetyService>();
builder.Services.AddScoped<eiibd26.Services.AI.IAiPromptBuilder, eiibd26.Services.AI.AiPromptBuilder>();
builder.Services.AddScoped<eiibd26.Jobs.AiAnswerJob>();

// Configurar Hangfire
builder.Services.AddHangfire(config => config
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.Queues = new[] { "default", "ai" };
});
// ===== FIN AI CONFIGURATION =====
```

**DESPUÉS de:** `app.UseAuthorization();`

Agrega:

```csharp
// Dashboard de Hangfire (solo en desarrollo o con autenticación)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
```

### PASO 6: Compilar y verificar

```bash
dotnet build
```

Si hay errores, verifica que todos los using estén correctos.

### PASO 7: Ejecutar aplicación

```bash
dotnet run
```

---

## 🧪 PRUEBAS

### TEST 1: Verificar configuración

1. Abre: `https://localhost:7002/hangfire` (en desarrollo)
2. Deberías ver el dashboard de Hangfire
3. Ve a "Servers" → Deberías ver 2 workers activos

### TEST 2: Crear pregunta de prueba

**Vía API:**
```bash
curl -X POST https://localhost:7002/api/preguntas \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TU_TOKEN" \
  -d '{
    "Titulo": "¿Qué es la Enfermedad de Crohn?",
    "Cuerpo": "Recién me diagnosticaron Crohn y tengo muchas dudas"
  }'
```

**Respuesta esperada:**
```json
{
  "ok": true,
  "id": "guid-de-pregunta",
  "slug": "que-es-la-enfermedad-de-crohn"
}
```

### TEST 3: Verificar job en Hangfire

1. Ve a: `https://localhost:7002/hangfire`
2. Click en "Jobs" → "Enqueued"
3. Deberías ver un job: `AiAnswerJob.ProcesarPreguntaAsync`
4. Espera 5-10 segundos
5. Ve a "Succeeded" → El job debería completarse exitosamente

### TEST 4: Verificar respuesta generada

```bash
curl https://localhost:7002/api/preguntas/{GUID}/respuestas
```

**Respuesta esperada:**
```json
{
  "ok": true,
  "preguntaId": "guid",
  "tieneRespuestaIA": true,
  "totalRespuestas": 1,
  "respuestas": [
    {
      "id": "guid-respuesta",
      "cuerpo": "**Entendiendo la Enfermedad de Crohn**...",
      "esAceptada": false,
      "esIA": true,
      "modeloIA": "claude-sonnet-4.5-20250514",
      ...
    }
  ]
}
```

---

## 🐛 TROUBLESHOOTING

### Error: "The type or namespace name 'Hangfire' could not be found"

**Solución:** Instala los paquetes NuGet (Paso 1)

### Error: "Table 'Respuestas' doesn't have column 'EsIA'"

**Solución:** Ejecuta las migraciones (Paso 2)

### Error: "Cannot insert NULL into column 'SystemUserId'"

**Solución:** 
1. Ejecuta `SETUP-SYSTEM-USER.sql` (Paso 3)
2. Copia el GUID en appsettings.json (Paso 4)

### Error: "Anthropic API returned 401 Unauthorized"

**Solución:** Verifica que tu API key sea válida en appsettings.json

### Los jobs no se ejecutan

**Verifica:**
1. Hangfire está configurado correctamente en Program.cs
2. El servidor de Hangfire está corriendo (ve al dashboard)
3. Revisa los logs en Visual Studio Output

### La respuesta de IA no aparece

**Verifica:**
1. El job completó exitosamente en Hangfire dashboard
2. Revisa los logs para errores
3. Verifica que `SystemUserId` en appsettings.json sea válido
4. Ejecuta: `SELECT * FROM Respuestas WHERE EsIA = 1`

---

## 📊 MONITOREO

### Hangfire Dashboard

Accede a: `https://localhost:7002/hangfire`

**Métricas importantes:**
- **Enqueued:** Jobs esperando procesarse
- **Processing:** Jobs ejecutándose
- **Succeeded:** Jobs completados exitosamente
- **Failed:** Jobs que fallaron (revisar logs)
- **Recurring:** Jobs programados (ninguno en este caso)

### Logs

Los logs se escriben en:
- Visual Studio → Output → Debug
- O configurar Serilog/NLog para persistencia

**Busca logs como:**
```
[INFO] Iniciando procesamiento de IA para pregunta {guid}
[INFO] Generando respuesta de IA para pregunta {guid}
[INFO] Respuesta de IA creada exitosamente
```

### Base de Datos

**Queries útiles:**

```sql
-- Ver respuestas de IA generadas
SELECT 
    r.Id,
    p.Titulo,
    r.EsIA,
    r.ModeloIA,
    r.FechaCreacion,
    LEN(r.Cuerpo) AS LongitudRespuesta
FROM Respuestas r
JOIN Preguntas p ON r.PreguntaId = p.Id
WHERE r.EsIA = 1
ORDER BY r.FechaCreacion DESC;

-- Ver preguntas sin respuesta humana
SELECT 
    p.Id,
    p.Titulo,
    p.TieneRespuestaIA,
    COUNT(r.Id) AS TotalRespuestas
FROM Preguntas p
LEFT JOIN Respuestas r ON p.Id = r.PreguntaId AND r.Eliminado = 0
WHERE p.Eliminado = 0
GROUP BY p.Id, p.Titulo, p.TieneRespuestaIA
HAVING COUNT(r.Id) <= 1;

-- Estadísticas de uso de IA
SELECT 
    COUNT(*) AS TotalRespuestasIA,
    AVG(LEN(Cuerpo)) AS PromedioCaracteres,
    MIN(FechaCreacion) AS PrimeraRespuesta,
    MAX(FechaCreacion) AS UltimaRespuesta
FROM Respuestas
WHERE EsIA = 1;
```

---

## 💰 COSTOS ESTIMADOS

### Claude Sonnet 4.5 Pricing (a marzo 2024)

- **Input:** $3.00 por millón de tokens
- **Output:** $15.00 por millón de tokens

### Estimación por respuesta:

**Prompt aproximado:**
- System prompt: ~400 tokens
- User prompt: ~150 tokens (depende de pregunta)
- **Total Input:** ~550 tokens

**Respuesta aproximada:**
- Output: ~600 tokens (configurado en MaxTokens)

**Costo por respuesta:**
```
Input:  550 tokens × $3.00 / 1M = $0.00165
Output: 600 tokens × $15.00 / 1M = $0.00900
TOTAL: ~$0.01065 por respuesta
```

### Estimación mensual:

| Preguntas/mes | Costo Mensual |
|---------------|---------------|
| 100           | $1.07         |
| 500           | $5.33         |
| 1,000         | $10.65        |
| 5,000         | $53.25        |
| 10,000        | $106.50       |

**IMPORTANTE:** 
- Estos costos son SOLO para preguntas sin respuestas humanas
- Una vez que un humano responde, no se generan más respuestas de IA
- Puedes ajustar `MaxTokens` para reducir costos

---

## ⚙️ CONFIGURACIÓN AVANZADA

### Cambiar temperatura (creatividad)

En `appsettings.json`:
```json
"Temperature": 0.2  // Más determinista (recomendado para medicina)
"Temperature": 0.5  // Balance
"Temperature": 0.8  // Más creativo (NO recomendado)
```

### Limitar tokens para reducir costos

```json
"MaxTokens": 400  // Respuestas más cortas (~$0.007 por respuesta)
"MaxTokens": 600  // Default (~$0.011 por respuesta)
"MaxTokens": 800  // Respuestas más largas (~$0.015 por respuesta)
```

### Deshabilitar IA temporalmente

```json
"Enabled": false
```

### Agregar palabras prohibidas

```json
"ForbiddenPhrases": [
  "aumenta la dosis",
  "nueva frase prohibida aquí"
]
```

---

## 🔮 PRÓXIMOS PASOS (OPCIONAL)

### 1. RAG (Retrieval Augmented Generation)

Prepara contexto desde tu base de conocimiento:

```csharp
var contextoDinamico = await ObtenerContextoRelevante(pregunta);
var userPrompt = _promptBuilder.BuildUserPrompt(pregunta, contextoDinamico);
```

### 2. Feedback de usuarios

Agrega sistema de votos para respuestas de IA:

```sql
ALTER TABLE Respuestas ADD VotosPositivos INT DEFAULT 0;
ALTER TABLE Respuestas ADD VotosNegativos INT DEFAULT 0;
```

### 3. Regenerar respuesta

Endpoint para regenerar si la respuesta no fue útil:

```csharp
[HttpPost("{id:guid}/regenerar-ia")]
public async Task<IActionResult> RegenerarRespuestaIA(Guid id)
{
    // Marcar respuesta anterior como eliminada
    // Generar nueva respuesta
}
```

### 4. Analytics

Track métricas:
- Tiempo promedio de generación
- Tasa de aceptación de respuestas IA vs humanas
- Preguntas más comunes sin respuestas
- Eficacia del filtro de seguridad

---

## 📞 SOPORTE

Si encuentras problemas no cubiertos aquí:

1. Revisa los logs en Hangfire dashboard
2. Verifica configuration en appsettings.json
3. Ejecuta queries de diagnóstico en SQL
4. Verifica que System User existe y tiene GUID correcto

**Logs clave para compartir:**
- Output de Visual Studio (Debug)
- Failed jobs en Hangfire con stack trace
- Resultado de queries de diagnóstico SQL

---

✅ **INSTALACIÓN COMPLETADA**

Tu sistema AI First Answer está listo para:
- Generar respuestas automáticas educativas
- Filtrar contenido médico peligroso
- Procesar jobs en segundo plano
- Minimizar costos con cache y configuración optimizada
- Escalar según demanda

¡Buena suerte! 🚀
