# 🎯 NOTIFICATION ACTIONS: RESPONDER SIN ABRIR LA APP

## ✅ **IMPLEMENTADO - ESTADO DE ÁNIMO DESDE NOTIFICACIÓN**

### **¿QUÉ SE IMPLEMENTÓ?**

Ahora las notificaciones push pueden tener **botones de acción** que permiten al usuario responder **SIN abrir la app**.

---

## 📱 **EJEMPLO VISUAL:**

```
┌────────────────────────────────────────────┐
│ 🔔 EIIBD - Comunidad EII                   │
│                                             │
│ ¿Cómo te sientes hoy?                       │
│ Comparte tu estado de ánimo                 │
│                                             │
│ [😊 Bien]  [😐 Normal]  [😢 Mal]           │
└────────────────────────────────────────────┘
```

Usuario hace clic en "😊 Bien" → Se guarda en BD → Confirmación

---

## 🚀 **CÓMO USARLO:**

### **1. CREAR NOTIFICACIÓN CON ACCIONES:**

En `/Identity/Admin/Notifications/Create`:

```
Título: ¿Cómo te sientes hoy?
Mensaje: Comparte tu estado de ánimo con la comunidad
URL: /Identity/Usuario/EstadoAnimo (opcional)
```

**✨ AUTOMÁTICO:** Si el mensaje contiene "cómo te sientes" o "estado de ánimo", 
se agregan botones automáticamente!

### **2. ENVIAR:**
- Click "Crear Notificación"
- Usuarios reciben notificación con 3 botones
- Click en botón → Guarda en BD sin abrir app

---

## 🔍 **CÓMO FUNCIONA TÉCNICAMENTE:**

### **Flow completo:**

```
1. Admin crea notificación con texto "¿Cómo te sientes hoy?"
   ↓
2. PushNotificationService detecta palabras clave
   ↓
3. Agrega automáticamente acciones al payload:
   {
     "title": "¿Cómo te sientes hoy?",
     "body": "Comparte tu estado de ánimo",
     "actions": [
       { "action": "mood-bien", "title": "😊 Bien" },
       { "action": "mood-normal", "title": "😐 Normal" },
       { "action": "mood-mal", "title": "😢 Mal" }
     ]
   }
   ↓
4. Service Worker muestra notificación con botones
   ↓
5. Usuario hace clic en "😊 Bien"
   ↓
6. Service Worker captura event.action = "mood-bien"
   ↓
7. Hace POST a /api/estado-animo con { estado: "bien" }
   ↓
8. API guarda en tabla EstadoAnimoUsuario
   ↓
9. Service Worker muestra confirmación:
   "✅ ¡Guardado! Tu estado de ánimo ha sido registrado"
```

---

## 📋 **PALABRAS CLAVE QUE ACTIVAN ACCIONES:**

### **Estado de Ánimo:**
Cualquiera de estas frases en el mensaje:
- "¿Cómo te sientes"
- "estado de ánimo"
- "cómo estás"

**Resultado:** Botones [😊 Bien] [😐 Normal] [😢 Mal]

### **Encuestas (Futuro):**
- "encuesta"
- "pregunta"

**Resultado:** Botones [✓ Sí] [✗ No] [Ver más]

---

## 🎯 **CASOS DE USO:**

### **1. Recordatorio diario de estado de ánimo:**
```
Título: ¡Buenos días! ☀️
Mensaje: ¿Cómo te sientes hoy? Tu seguimiento es importante
→ Usuario responde sin abrir app
```

### **2. Check-in después de tratamiento:**
```
Título: Seguimiento de tratamiento
Mensaje: ¿Cómo te sientes después de tu última dosis?
→ Usuario responde directamente
```

### **3. Encuesta rápida (futuro):**
```
Título: Encuesta rápida
Mensaje: ¿Te gustó el nuevo artículo sobre alimentación?
→ [👍 Sí] [👎 No] [Ver más]
```

---

## 💡 **EXPANSIÓN FUTURA:**

### **Ya implementado el sistema para:**

1. **Encuestas:**
   ```javascript
   actions: [
     { action: "survey-si", title: "✓ Sí" },
     { action: "survey-no", title: "✗ No" },
     { action: "view", title: "Ver más" }
   ]
   ```

2. **Respuestas personalizadas:**
   - Agregar más opciones de estado
   - Escala del 1 al 5
   - Emojis personalizados

3. **Input de texto (limitado en algunas plataformas):**
   - Android permite "reply" actions
   - iOS no soporta input directo

---

## 🔧 **PERSONALIZAR ACCIONES:**

### **Opción 1: Automático (actual)**
El sistema detecta palabras clave y agrega acciones

### **Opción 2: Manual (próximamente)**
Admin puede especificar acciones personalizadas en el formulario:

```
Acciones personalizadas:
- Acción 1: [Texto: "Responder" | Action: "reply"]
- Acción 2: [Texto: "Ver" | Action: "view"]
- Acción 3: [Texto: "Más tarde" | Action: "dismiss"]
```

---

## 📊 **VERIFICAR QUE FUNCIONA:**

### **1. Crear notificación:**
```
Título: Prueba de acciones
Mensaje: ¿Cómo te sientes hoy?
Destinatarios: ✓ Todos
Enviar: ✓ Inmediatamente
```

### **2. Recibir notificación:**
- Deberías ver 3 botones
- Si NO los ves → Verificar en Console (F12)

### **3. Hacer clic en botón:**
- Click "😊 Bien"
- Esperar 1-2 segundos
- Nueva notificación: "✅ ¡Guardado!"

### **4. Verificar en BD:**
```sql
SELECT TOP 1 * 
FROM EstadoAnimoUsuario 
WHERE IdUsuario = 'tu-guid'
ORDER BY FechaRegistro DESC

-- Debería mostrar:
-- EstadoMood: Bien
-- Texto: "Registrado desde notificación push"
-- FechaRegistro: [fecha actual]
```

---

## 🐛 **DEBUGGING:**

### **Si los botones NO aparecen:**

1. **Verificar Service Worker actualizado:**
   ```javascript
   // En Console:
   navigator.serviceWorker.getRegistration().then(reg => {
     console.log('SW:', reg.active.scriptURL);
     reg.update(); // Forzar actualización
   });
   ```

2. **Verificar payload en Network:**
   - F12 → Network → WS (WebSocket)
   - Ver payload de push message
   - Debe tener: `"actions": [...]`

3. **Verificar plataforma:**
   - ✅ Chrome Desktop: Soportado
   - ✅ Chrome Android: Soportado
   - ⚠️ iOS Safari: Limitado (requiere PWA instalada)
   - ✅ Edge: Soportado

---

## ⚡ **LIMITACIONES:**

### **Desktop:**
- ✅ Hasta 2-3 acciones recomendadas
- ✅ Texto corto (máx ~20 caracteres por botón)

### **Android:**
- ✅ Hasta 3 acciones
- ✅ Soporta íconos
- ✅ Puede incluir "Reply" action (texto libre)

### **iOS:**
- ⚠️ Solo funciona en PWA instalada (no en Safari browser)
- ⚠️ Limitado a 2 acciones
- ❌ No soporta "Reply" action

---

## 📝 **PRÓXIMOS PASOS:**

### **A corto plazo:**
- [ ] Agregar más tipos de preguntas rápidas
- [ ] Dashboard para ver respuestas de notificaciones
- [ ] Analytics de engagement (cuántos responden)

### **A medio plazo:**
- [ ] UI para personalizar acciones en formulario
- [ ] Notificaciones ricas con imágenes
- [ ] Notificaciones con inputs de texto (Android)

### **A largo plazo:**
- [ ] Conversaciones bidireccionales
- [ ] Chatbot desde notificaciones
- [ ] Notificaciones de grupo

---

## ✅ **ARCHIVOS MODIFICADOS/CREADOS:**

1. ✅ `service-worker.js`
   - Agregado handler para `notificationclick` con acciones
   - Función `handleNotificationAction()`
   - Cache version → v4

2. ✅ `PushNotificationService.cs`
   - Método `ParseActions()` para detectar palabras clave
   - Payload incluye `actions` array

3. ✅ `EstadoAnimoApiController.cs` ⭐ NUEVO
   - Endpoint `/api/estado-animo` POST
   - Guarda respuesta desde notificación

---

## 🎉 **¡LISTO PARA PROBAR!**

**Hot Reload está activo pero necesitas:**
1. Hard refresh (Ctrl+Shift+R)
2. O desregistrar SW y recargar

**Luego:**
1. Crea notificación con "¿Cómo te sientes hoy?"
2. Recíbela en tu dispositivo
3. ✅ Click en botón
4. ✅ Se guarda automáticamente!

---

**¿Quieres agregar más tipos de acciones?** 🚀

Ejemplos:
- "¿Tomaste tu medicamento?" → [Sí] [No] [Más tarde]
- "¿Tuviste síntomas hoy?" → [Sí, leves] [Sí, graves] [No]
- "¿Necesitas ayuda?" → [Llamar médico] [Ver consejos] [Estoy bien]
