# 🧪 CÓMO PROBAR NOTIFICACIONES PUSH

## ✅ PASO A PASO:

### **1. VERIFICAR ESTADO ACTUAL**
```
1. Ve a: /Identity/Admin/Notifications/Debug
2. Verifica cuántas suscripciones hay en la BD
3. Si hay 0 → Nadie está suscrito
```

---

### **2. SUSCRIBIR USUARIOS MANUALMENTE**

#### **Usuario 1:**
```
1. Cierra sesión (si estás como admin)
2. Inicia sesión como USUARIO NORMAL (no admin)
3. Ve a: /Identity/Usuario/Dashboard
4. Deberías ver una card "Notificaciones Push"
5. Estado debería decir: "Activa las notificaciones..."
6. Haz clic en: "Activar notificaciones"
7. El navegador pedirá permiso → Clic "Permitir"
8. Debería decir: "✅ ¡Notificaciones activadas correctamente!"
```

#### **Usuario 2:**
```
1. Abre una ventana de incógnito
2. Inicia sesión con otro usuario
3. Ve a: /Identity/Usuario/Dashboard
4. Haz clic en: "Activar notificaciones"
5. Permitir notificaciones
```

---

### **3. VERIFICAR SUSCRIPCIONES**
```
1. Ve a (como admin): /Identity/Admin/Notifications/Debug
2. Ahora deberías ver:
   - Total: 2
   - Activas: 2
   - Tabla con los 2 usuarios
```

---

### **4. CREAR Y ENVIAR NOTIFICACIÓN**

```
1. Ve a: /Identity/Admin/Notifications/Create
2. Completa:
   - Título: "¡Hola desde EIIBD!"
   - Mensaje: "Esta es una prueba de notificaciones push"
   - URL: /Preguntas (opcional)
   - Destinatarios: ✓ Todos los usuarios (debería decir "2 suscriptores activos")
   - Envío: ✓ Enviar inmediatamente
3. Clic: "Crear Notificación"
4. Debería enviar y mostrar: "Enviadas: 2, Fallidas: 0"
```

---

### **5. VERIFICAR RECEPCIÓN**

#### **En el navegador de Usuario 1:**
```
1. Deberías ver una notificación del sistema
2. Título: "¡Hola desde EIIBD!"
3. Mensaje: "Esta es una prueba..."
4. Haz clic → Te lleva a /Preguntas
```

#### **En el navegador de Usuario 2 (incógnito):**
```
1. También debería recibir la notificación
```

---

## 🔍 DEBUGGING:

### **Si NO aparece la card en Dashboard:**
```javascript
// Console del navegador (F12):
console.log('Notification' in window); // debe ser true
console.log(Notification.permission); // debe ser "default", "granted" o "denied"
```

### **Si el botón "Activar" no funciona:**
```javascript
// Console:
console.log(typeof window.subscribeToPush); // debe ser "function"

// Ejecutar manualmente:
window.subscribeToPush();
```

### **Si dice "0 suscriptores" después de activar:**
```sql
-- En SQL Server:
SELECT * FROM NotificationSubscriptions;

-- Debería mostrar filas
```

### **Si las notificaciones no llegan:**
```
1. Verifica en Admin/Notifications/Index
2. Click en el registro enviado
3. Verifica TotalSent y TotalFailed
4. Si TotalFailed > 0 → Problema con VAPID keys o subscription
```

---

## 📊 ESTADOS POSIBLES:

### **Card en Dashboard:**

| Permiso | Suscrito | Mensaje | Botón |
|---------|----------|---------|-------|
| `default` | No | "Activa las notificaciones..." | "Activar notificaciones" |
| `granted` | No | "Permiso concedido pero no suscrito..." | "Suscribirse ahora" |
| `granted` | Sí | "✅ Estás suscrito..." | "Desactivar notificaciones" |
| `denied` | - | "❌ Has bloqueado..." | (ninguno) |

---

## 🎯 TROUBLESHOOTING COMÚN:

### **Problema: Botón no hace nada**
**Causa:** Service Worker no registrado
**Solución:**
```javascript
// Console:
navigator.serviceWorker.getRegistration().then(reg => {
  console.log('SW:', reg);
});

// Si es null:
navigator.serviceWorker.register('/service-worker.js');
```

### **Problema: Error "User not authenticated"**
**Causa:** Usuario no está logueado
**Solución:** Iniciar sesión primero

### **Problema: VAPID key error**
**Causa:** Keys no configuradas o incorrectas
**Solución:**
```
1. Verifica appsettings.json tiene VapidKeys
2. Verifica que PublicKey y PrivateKey no estén vacíos
3. Regenera keys si es necesario:
   powershell Tools/Generate-VapidKeys.ps1
```

### **Problema: "Endpoint already exists"**
**Causa:** Ya suscrito antes
**Solución:** Normal, el código debería manejar esto

### **Problema: Notificación no aparece en móvil**
**Causa:** App debe estar instalada como PWA
**Solución:**
```
Android: Menu → "Instalar app"
iOS: Share → "Añadir a pantalla de inicio"
```

---

## 🎉 PRUEBA AVANZADA:

### **Notificación con imagen:**
```
Título: "Nuevo contenido disponible"
Mensaje: "Lee nuestro último artículo sobre Crohn"
Icono: /img/icons/icon-192x192.png
URL: /Contenidos/recien-diagnosticado
```

### **Notificación programada:**
```
Selecciona: ⚪ Programar envío
Fecha: Mañana
Hora: 10:00 AM
```

### **Verificar en historial:**
```
1. Ve a: /Identity/Admin/Notifications/Index
2. Deberías ver estado "Programada"
3. A la hora programada, se enviará automáticamente
```

---

## 📱 TESTING EN DIFERENTES NAVEGADORES:

### **Chrome/Edge Desktop:**
✅ Soportado completamente

### **Firefox Desktop:**
✅ Soportado completamente

### **Chrome Android:**
✅ Soportado (requiere PWA instalada)

### **Safari iOS 16.4+:**
✅ Soportado (requiere "Añadir a pantalla de inicio")

### **Safari iOS < 16.4:**
❌ No soportado

---

## 🎯 CHECKLIST COMPLETO:

- [ ] Reinicié la aplicación
- [ ] Ejecuté el script SQL para crear tablas
- [ ] Los iconos PWA están en /img/icons/
- [ ] Inicié sesión como usuario normal
- [ ] Veo la card "Notificaciones Push" en Dashboard
- [ ] Hice clic en "Activar notificaciones"
- [ ] Permití notificaciones en el navegador
- [ ] Estado cambió a "✅ Estás suscrito..."
- [ ] En /Admin/Notifications/Debug veo mi suscripción
- [ ] Creé una notificación de prueba
- [ ] Dice "X suscriptores activos" (no 0)
- [ ] Envié la notificación
- [ ] Dice "Enviadas: X, Fallidas: 0"
- [ ] Recibí la notificación en el navegador
- [ ] Hice clic y me llevó a la URL correcta

---

¡LISTO PARA PROBAR! 🚀
