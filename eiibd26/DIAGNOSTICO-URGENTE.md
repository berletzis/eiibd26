# 🚨 DIAGNÓSTICO URGENTE: No se generan respuestas IA

## ⚡ ACCIÓN INMEDIATA

He creado herramientas de diagnóstico para identificar el problema exacto.

---

## 🔧 PASO 1: EJECUTAR DIAGNÓSTICO SQL

```sql
-- En SQL Server Management Studio, ejecuta:
-- D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26\DIAGNOSTICO-RAPIDO.sql
```

Este script te dirá EXACTAMENTE qué está fallando:
- ❌ Usuario sistema no existe → Ejecutar `SETUP-SYSTEM-USER.sql`
- ❌ Campos de migración faltan → Ejecutar `MIGRATION-AI-FIELDS.sql`
- ⏳ Pregunta muy reciente (< 60 segundos) → Esperar y recargar
- ❌ Problema en la aplicación → Ver PASO 2

---

## 🖥️ PASO 2: PANEL DE DIAGNÓSTICO WEB

1. **Reinicia la aplicación** (importante!)
   ```
   Detener debug (Shift+F5) y ejecutar de nuevo (F5)
   ```

2. **Compila el proyecto:**
   ```powershell
   cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
   dotnet build
   ```

3. **Accede al panel de diagnóstico:**
   ```
   https://localhost:PUERTO/Test/AiTest
   ```

   El panel te mostrará:
   - ✅/❌ Estado de la configuración
   - ✅/❌ Usuario sistema existe
   - ✅/❌ API Key configurada
   - Lista de preguntas recientes
   - Botón para **FORZAR** generación de IA

4. **Presiona el botón "⚡ Generar Respuesta IA Ahora"**
   - Esto ejecutará el job INMEDIATAMENTE
   - Verás el resultado en tiempo real
   - Los logs mostrarán el error exacto si falla

---

## 🔍 PASO 3: USAR API DE TEST

Si prefieres usar Postman/Thunder Client:

### 3.1. Ver estado del sistema
```http
GET https://localhost:PUERTO/api/test/ai/status
```

Respuesta esperada:
```json
{
  "status": "ok",
  "configuration": {
    "enabled": true,  // ← Debe ser true
    "hasApiKey": true,  // ← Debe ser true
    "model": "claude-sonnet-4.5-20250514",
    "hasSystemUserId": true,  // ← Debe ser true
    "systemUserId": "50649075-660F-4431-9049-98C9E3AC6D73"
  },
  "database": {
    "systemUserExists": true,  // ← Debe ser true
    "totalPreguntas": 10,
    "preguntasConIA": 0,  // ← Aquí está el problema
    "respuestasIA": 0
  }
}
```

Si alguno es `false`, ahí está el problema.

### 3.2. Forzar generación para la última pregunta
```http
POST https://localhost:PUERTO/api/test/ai/force-latest
Content-Type: application/json
```

Esto ejecutará el job **INMEDIATAMENTE** y te dará el error exacto si falla.

---

## 🐛 PROBLEMAS COMUNES Y SOLUCIONES

### ❌ Problema: `systemUserExists: false`

**Solución:**
```sql
-- Ejecuta en SQL Server:
-- D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26\SETUP-SYSTEM-USER.sql
```

Luego copia el GUID y verifica que esté en `appsettings.json`:
```json
"AiAnswer": {
  "SystemUserId": "GUID_AQUÍ"
}
```

### ❌ Problema: `hasApiKey: false`

**Solución:**
```json
// En appsettings.json
"AiAnswer": {
  "AnthropicApiKey": "sk-ant-api03-TU_API_KEY_REAL"  // ← NO dejar el placeholder
}
```

### ❌ Problema: `enabled: false`

**Solución:**
```json
// En appsettings.json
"AiAnswer": {
  "Enabled": true  // ← Cambiar a true
}
```

### ❌ Problema: Error en campo de BD

**Solución:**
```sql
-- Ejecuta en SQL Server:
-- D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26\MIGRATION-AI-FIELDS.sql
```

---

## 📋 CHECKLIST RÁPIDO

Marca cada item:

- [ ] ✅ Aplicación **REINICIADA** (no en modo debug antiguo)
- [ ] ✅ `dotnet build` sin errores
- [ ] ✅ `DIAGNOSTICO-RAPIDO.sql` ejecutado
- [ ] ✅ Usuario sistema existe en BD
- [ ] ✅ Campos de migración existen
- [ ] ✅ `appsettings.json` tiene `Enabled: true`
- [ ] ✅ `appsettings.json` tiene API key real (no placeholder)
- [ ] ✅ SystemUserId en appsettings coincide con BD
- [ ] ✅ Panel `/Test/AiTest` accesible
- [ ] ✅ Botón "Generar IA" presionado
- [ ] ✅ Ver logs del resultado

---

## 🎯 SI TODO ESTÁ ✅ PERO NO FUNCIONA

Si todos los checks están en ✅ pero sigue sin generar respuestas, entonces el problema está en:

1. **La API de Anthropic**
   - Error 401: API key inválida
   - Error 429: Rate limit excedido
   - Error 500: Problema en el servicio de Anthropic

2. **El contenido de la pregunta**
   - El sistema de seguridad bloquea contenido médico específico
   - El prompt no genera respuesta válida

**Solución:** Usa el panel `/Test/AiTest` para forzar la generación y **ver el error exacto en los logs**.

---

## 📞 SIGUIENTE PASO

1. **Ejecuta `DIAGNOSTICO-RAPIDO.sql`** y envíame el resultado
2. **Accede a `/Test/AiTest`** y presiona el botón "Generar IA"
3. **Captura los logs** del panel web o de la consola de Visual Studio
4. **Envíame** los resultados para diagnosticar el problema exacto

---

## 🔥 SOLUCIÓN EXPRESS (SI TIENES PRISA)

```powershell
# 1. Compilar
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet build

# 2. Ejecutar
dotnet run
```

Luego:
1. Abre `https://localhost:PUERTO/Test/AiTest`
2. Presiona "⚡ Generar Respuesta IA Ahora"
3. Lee el error en pantalla
4. Envíame el error para ayudarte a resolverlo

---

**Los archivos creados:**
- ✅ `Controllers/AiTestController.cs` - API de diagnóstico
- ✅ `Pages/Test/AiTest.cshtml` - Panel web de diagnóstico
- ✅ `Pages/Test/AiTest.cshtml.cs` - Code-behind
- ✅ `DIAGNOSTICO-RAPIDO.sql` - Script SQL de diagnóstico
- ✅ `DIAGNOSTICO-URGENTE.md` - Este archivo

**REINICIA LA APLICACIÓN AHORA Y USA EL PANEL DE DIAGNÓSTICO.**
