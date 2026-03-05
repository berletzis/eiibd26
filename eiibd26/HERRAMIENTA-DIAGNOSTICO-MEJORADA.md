# 🔧 HERRAMIENTA DE DIAGNÓSTICO MEJORADA

## ✅ CAMBIOS IMPLEMENTADOS

He agregado herramientas adicionales para diagnosticar el problema de conexión con la API de Claude.

---

## 🆕 NUEVO: Endpoint de prueba de conexión

### GET `/api/test/ai/test-connection`

Este endpoint hace una petición **directa y simple** a la API de Claude para verificar que la conexión funciona.

**Beneficios:**
- ✅ Prueba la API Key
- ✅ Verifica el modelo configurado
- ✅ Muestra la URL completa
- ✅ Devuelve el error exacto si falla

---

## 🎯 CÓMO USAR

### PASO 1: REINICIAR LA APLICACIÓN

```
1. Detener debug: Shift + F5
2. Iniciar de nuevo: F5
```

### PASO 2: ACCEDER AL PANEL

```
https://localhost:PUERTO/Test/AiTest
```

### PASO 3: PROBAR CONEXIÓN PRIMERO

1. **Presiona el botón: "🔍 Probar Conexión con Claude API"**
2. Esto te mostrará:
   - ✅ Si la conexión funciona
   - ❌ El error exacto si falla
   - La URL completa
   - El modelo que estás usando

### PASO 4: DESPUÉS, FORZAR GENERACIÓN

Si la prueba de conexión funciona (✅), entonces puedes:
- **Presionar: "⚡ Generar Respuesta IA Ahora"**

---

## 📊 POSIBLES RESULTADOS

### ✅ CONEXIÓN EXITOSA

```
✅ Conexión Exitosa
Status: 200
La API de Claude responde correctamente.
```

**Acción:** Ahora puedes generar respuestas IA normalmente.

---

### ❌ ERROR 404 Not Found

```
❌ Error 404: Not Found
URL: https://api.anthropic.com/v1/messages
Model: claude-3-5-sonnet-20241022
```

**Posibles causas:**
1. **El modelo no existe** - Verifica que el nombre sea correcto
2. **La URL está mal** - Debería ser `https://api.anthropic.com/v1`
3. **Falta el `/messages`** - El endpoint correcto es `/v1/messages`

---

### ❌ ERROR 401 Unauthorized

```
❌ Error 401: Unauthorized
```

**Causa:** API Key inválida o expirada

**Solución:**
1. Ve a: https://console.anthropic.com/
2. Genera una nueva API Key
3. Actualiza `appsettings.json`:
```json
"AnthropicApiKey": "tu-nueva-api-key"
```

---

### ❌ ERROR 400 Bad Request

```
❌ Error 400: Bad Request
```

**Posibles causas:**
1. El modelo especificado no existe
2. El formato del request está mal
3. Faltan headers requeridos

**Verifica en el log:**
- Model usado
- Headers enviados
- Request body

---

## 🔍 LOGS ADICIONALES

Ahora el servicio `AiAnswerService` registra más información al iniciar:

```
AiAnswerService initialized:
  BaseAddress: https://api.anthropic.com/v1
  API Version: 2023-06-01
  Model: claude-3-5-sonnet-20241022
  Has API Key: True
  Headers configured: x-api-key=True, anthropic-version=True
```

Si alguno de estos valores está mal, ahí está el problema.

---

## 🐛 TROUBLESHOOTING

### Problema: BaseAddress es null

```
BaseAddress: (null)
```

**Solución:** Verifica que `appsettings.json` tiene:
```json
"ApiBaseUrl": "https://api.anthropic.com/v1"
```

### Problema: Has API Key: False

```
Has API Key: False
```

**Solución:** Verifica que `appsettings.json` tiene:
```json
"AnthropicApiKey": "sk-ant-api03-..."
```

### Problema: Headers configured: x-api-key=False

```
Headers configured: x-api-key=False
```

**Causa:** El HttpClient no se configuró correctamente

**Solución:** Reinicia la aplicación completamente

---

## 📋 CHECKLIST

- [ ] Reiniciaste la aplicación
- [ ] Accediste a `/Test/AiTest`
- [ ] Presionaste "🔍 Probar Conexión"
- [ ] Viste los logs en la sección "Logs"
- [ ] Capturaste el error exacto

---

## 🎯 SIGUIENTE PASO

**REINICIA LA APLICACIÓN AHORA** y prueba el botón de conexión.

El resultado te dirá exactamente qué está fallando.
