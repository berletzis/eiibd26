# 🔑 CONFIGURACIÓN API KEY DE ANTHROPIC - GUÍA COMPLETA

## ⚠️ ACCIÓN REQUERIDA

Para que la IA funcione, necesitas configurar tu API key de Anthropic (Claude).

---

## 📋 PASO 1: OBTENER API KEY DE ANTHROPIC

### 1.1 Crear Cuenta en Anthropic Console

1. Ve a: https://console.anthropic.com/
2. Click en **"Sign Up"** (o "Sign In" si ya tienes cuenta)
3. Completa el registro con tu email
4. Verifica tu email

### 1.2 Agregar Créditos (Necesario para Uso)

⚠️ **Anthropic requiere que agregues créditos antes de usar la API**

1. Ve a: https://console.anthropic.com/settings/billing
2. Click en **"Add Credit"**
3. Opciones recomendadas:
   - **Para testing:** $5 USD (suficiente para ~500 respuestas)
   - **Para producción:** $20 USD (suficiente para ~2,000 respuestas)
4. Agrega tarjeta de crédito y completa el pago

💰 **Costos estimados:**
- 1 respuesta de IA: ~$0.01 USD
- 100 respuestas/mes: ~$1.00 USD
- 1,000 respuestas/mes: ~$10.00 USD

### 1.3 Crear API Key

1. Ve a: https://console.anthropic.com/settings/keys
2. Click en **"Create Key"** o **"+ Create API Key"**
3. Copia la clave que comienza con: `sk-ant-...`

⚠️ **IMPORTANTE:** 
- La clave solo se muestra UNA VEZ
- Guárdala en un lugar seguro
- No la compartas públicamente
- No la subas a GitHub

---

## 📋 PASO 2: CONFIGURAR EN TU PROYECTO

### 2.1 Ubicar el Archivo de Configuración

Archivo: `eiibd26/appsettings.json`

### 2.2 Reemplazar la API Key

Busca esta línea en el archivo:

```json
"AnthropicApiKey": "ANTHROPIC_API_KEY_AQUI",
```

Reemplázala con tu clave real:

```json
"AnthropicApiKey": "sk-ant-api03-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
```

### 2.3 Verificar la Configuración Completa

Tu sección `AiAnswer` debe verse así:

```json
"AiAnswer": {
  "Enabled": true,
  "AnthropicApiKey": "sk-ant-api03-TU_CLAVE_REAL_AQUI",
  "Model": "claude-sonnet-4.5-20250514",
  "Temperature": 0.3,
  "MaxTokens": 600,
  "TimeoutSeconds": 30,
  "ApiBaseUrl": "https://api.anthropic.com/v1",
  "ApiVersion": "2023-06-01",
  "SystemUserId": "00000000-0000-0000-0000-000000000000",
  "ForbiddenPhrases": [
    "aumenta la dosis",
    "suspende el medicamento",
    // ... más frases
  ]
}
```

---

## 📋 PASO 3: CONFIGURAR USUARIO DEL SISTEMA

La IA necesita un usuario del sistema para publicar respuestas.

### 3.1 Ejecutar Script SQL

Archivo: `eiibd26/SETUP-SYSTEM-USER.sql`

```sql
-- Ejecuta este script en SQL Server Management Studio
USE [eiibd26];
GO

-- Verificar si ya existe el usuario
SELECT * FROM AspNetUsers WHERE Email = 'system-ai@eiibd.com';

-- Si no existe, ejecutar la creación (ver archivo completo)
```

### 3.2 Copiar el User ID Generado

Después de ejecutar el script, copia el `Id` del usuario creado.

### 3.3 Actualizar appsettings.json

Reemplaza el `SystemUserId` con el ID real:

```json
"SystemUserId": "abc-123-def-456-ghi-789",  // ← Tu ID real aquí
```

---

## 📋 PASO 4: EJECUTAR MIGRACIÓN DE BASE DE DATOS

### 4.1 Agregar Campos AI a Tabla Respuestas

Archivo: `eiibd26/MIGRATION-AI-FIELDS.sql`

```sql
-- Ejecuta este script en SQL Server Management Studio
USE [eiibd26];
GO

-- Agregar campos AI a Respuestas
ALTER TABLE Respuestas ADD EsIA bit NOT NULL DEFAULT 0;
ALTER TABLE Respuestas ADD ModeloIA nvarchar(100) NULL;
ALTER TABLE Respuestas ADD EsColapsada bit NOT NULL DEFAULT 0;
ALTER TABLE Respuestas ADD Puntuacion int NOT NULL DEFAULT 0;

-- Agregar campos a Preguntas
ALTER TABLE Preguntas ADD TieneRespuestaIA bit NOT NULL DEFAULT 0;
ALTER TABLE Preguntas ADD FechaGeneracionIA datetimeoffset(7) NULL;
```

### 4.2 Crear Constraint de Duplicados

Archivo: `eiibd26/Migrations/20250104_AddUniqueAIAnswerConstraint.sql`

```sql
-- Ejecuta DESPUÉS de agregar los campos
USE [eiibd26];
GO

-- Crear índice único para prevenir duplicados
CREATE UNIQUE NONCLUSTERED INDEX [UX_Respuestas_OneAIAnswerPerQuestion]
ON [Respuestas]([PreguntaId])
WHERE [EsIA] = 1 AND [Eliminado] = 0;
```

---

## 📋 PASO 5: VERIFICAR QUE FUNCIONA

### 5.1 Reiniciar la Aplicación

```bash
# Detener la aplicación
Ctrl + C (en la terminal)

# Volver a ejecutar
dotnet run
```

### 5.2 Crear una Pregunta de Prueba

1. Ve a: `https://localhost:7002/Preguntas`
2. Click en **"Nueva Pregunta"**
3. Escribe:
   - **Título:** "¿Qué es la Enfermedad de Crohn?"
   - **Cuerpo:** "Recientemente diagnosticado, quisiera entender mejor la enfermedad."
4. Click en **"Publicar"**

### 5.3 Esperar 10-15 Segundos

- La respuesta de IA se genera en segundo plano
- NO es instantánea (es por diseño)

### 5.4 Refrescar la Página

```
F5 o Ctrl + R
```

### 5.5 Verificar que Aparece la Respuesta

Deberías ver:

```
┌─────────────────────────────────────────────────────────────┐
│ 🤖 Respuesta Informativa (IA)                                │
│                                                               │
│ **Enfermedad de Crohn - Información General**                │
│                                                               │
│ La Enfermedad de Crohn es una condición inflamatoria...     │
│ [resto del contenido]                                         │
│                                                               │
│ ⚠️ **Aviso Importante:** Esta es una respuesta informativa   │
│    y NO reemplaza la consulta con un profesional médico.     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔍 TROUBLESHOOTING

### ❌ Problema: No aparece respuesta de IA

**Posibles causas:**

#### 1. API Key no configurada
```bash
# Verificar logs
tail -f /var/log/app.log | grep "AI Answer"

# Debería mostrar:
[Error] Anthropic API key is not configured
```

**Solución:** Configura tu API key en `appsettings.json`

---

#### 2. Sin créditos en Anthropic
```bash
# Verificar logs
tail -f /var/log/app.log | grep "401"

# Error común:
[Error] HTTP 401: Insufficient credits
```

**Solución:** Agrega créditos en https://console.anthropic.com/settings/billing

---

#### 3. Usuario del sistema no existe
```bash
# Verificar logs
[Error] System user not found: 00000000-0000-0000-0000-000000000000
```

**Solución:** 
1. Ejecuta `SETUP-SYSTEM-USER.sql`
2. Copia el ID del usuario creado
3. Actualiza `SystemUserId` en `appsettings.json`

---

#### 4. Campos de BD no existen
```bash
# Verificar logs
[Error] Invalid column name 'EsIA'
```

**Solución:** Ejecuta `MIGRATION-AI-FIELDS.sql`

---

#### 5. Hangfire no está configurado
```bash
# Verificar logs
[Error] No job processor found
```

**Solución:** Hangfire está comentado para desarrollo. Ver `INSTALLATION-GUIDE.md` para configurarlo.

---

### ✅ Verificación Manual de API Key

Ejecuta este comando en PowerShell/Terminal:

```powershell
# Test básico de API
$headers = @{
    "x-api-key" = "sk-ant-api03-TU_CLAVE_AQUI"
    "anthropic-version" = "2023-06-01"
    "content-type" = "application/json"
}

$body = @{
    model = "claude-sonnet-4.5-20250514"
    max_tokens = 100
    messages = @(
        @{
            role = "user"
            content = "Hello, world!"
        }
    )
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://api.anthropic.com/v1/messages" -Method POST -Headers $headers -Body $body
```

**Resultado esperado:**
```json
{
  "id": "msg_...",
  "type": "message",
  "content": [
    {
      "type": "text",
      "text": "Hello! How can I help you today?"
    }
  ]
}
```

---

## 📊 VERIFICAR EN BASE DE DATOS

### Ver si hay respuestas de IA generadas

```sql
SELECT 
    p.Titulo AS Pregunta,
    p.TieneRespuestaIA,
    p.FechaGeneracionIA,
    r.Cuerpo AS RespuestaIA,
    r.ModeloIA,
    r.FechaCreacion
FROM Preguntas p
LEFT JOIN Respuestas r ON r.PreguntaId = p.Id AND r.EsIA = 1 AND r.Eliminado = 0
WHERE p.TieneRespuestaIA = 1
ORDER BY p.FechaCreacion DESC;
```

---

## 📈 MONITOREO

### Ver métricas en logs

```bash
# Success rate
grep -c "\[Metrics\] AI Answer SUCCESS" app.log

# Failures
grep -c "\[Metrics\] AI Job FAILED" app.log

# Safety blocks
grep -c "\[Metrics\] Safety Check BLOCKED" app.log

# Tiempo promedio
grep "DurationSeconds=" app.log | awk -F'DurationSeconds=' '{print $2}' | awk '{print $1}' | awk '{sum+=$1; count++} END {print "Promedio:", sum/count, "segundos"}'
```

---

## ✅ CHECKLIST FINAL

Antes de considerar que la IA está funcionando, verifica:

- [ ] API key de Anthropic configurada
- [ ] Créditos agregados en Anthropic Console
- [ ] Usuario del sistema creado (`SETUP-SYSTEM-USER.sql`)
- [ ] `SystemUserId` actualizado en `appsettings.json`
- [ ] Campos AI agregados a BD (`MIGRATION-AI-FIELDS.sql`)
- [ ] Constraint de duplicados creado (`20250104_AddUniqueAIAnswerConstraint.sql`)
- [ ] Aplicación reiniciada
- [ ] Pregunta de prueba creada
- [ ] Respuesta de IA aparece después de 10-15 segundos
- [ ] Badge "🤖 Respuesta Informativa (IA)" visible
- [ ] Disclaimer de seguridad presente

---

## 🎯 RESUMEN RÁPIDO

```bash
# 1. Obtén API key
https://console.anthropic.com/settings/keys

# 2. Agrega créditos (mínimo $5)
https://console.anthropic.com/settings/billing

# 3. Actualiza appsettings.json
"AnthropicApiKey": "sk-ant-api03-TU_CLAVE"

# 4. Ejecuta migraciones SQL
SETUP-SYSTEM-USER.sql
MIGRATION-AI-FIELDS.sql
20250104_AddUniqueAIAnswerConstraint.sql

# 5. Actualiza SystemUserId en appsettings.json
"SystemUserId": "ID-DEL-USUARIO-CREADO"

# 6. Reinicia app
dotnet run

# 7. Crea pregunta de prueba

# 8. Espera 10-15 seg y refresca página

# 9. ¡Verifica respuesta con badge 🤖!
```

---

## 📞 SOPORTE

Si después de seguir estos pasos la IA no funciona:

1. Revisa los logs: `tail -f /var/log/app.log`
2. Busca errores con: `grep "\[Error\]" app.log`
3. Verifica costos en: https://console.anthropic.com/settings/usage
4. Consulta documentación: https://docs.anthropic.com/

---

**Última actualización:** 2025-01-04  
**Versión:** 1.0 - Implementación Frontend + Verificación
