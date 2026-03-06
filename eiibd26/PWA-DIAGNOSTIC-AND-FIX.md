# 🔍 Diagnóstico PWA - Instalación y Notificaciones

## 🚨 Problema Reportado
- ❌ No aparece la solicitud de instalar la aplicación
- ❌ Las push notifications no funcionan

---

## 📊 Estado Actual del Sistema

### ✅ Archivos Presentes
1. ✅ `/wwwroot/manifest.json` - Configurado correctamente
2. ✅ `/wwwroot/service-worker.js` - Service Worker funcional
3. ✅ `/wwwroot/js/pwa.js` - Script PWA con lógica de instalación
4. ✅ `_Layout.cshtml` - Configuración de meta tags PWA

### 🔧 Configuración Detectada
- Manifest enlazado: `<link rel="manifest" href="/manifest.json" />`
- Service Worker registrado en: `/service-worker.js`
- Script PWA cargado: `<script src="~/js/pwa.js" defer></script>`

---

## 🎯 Posibles Causas del Problema

### 1️⃣ **La App Ya Está Instalada** (Muy Probable)
El código actual tiene lógica para detectar si la app ya está instalada y **NO mostrar el banner** en ese caso.

```javascript
// Líneas 9-29 en pwa.js
if (isAppInstalled()) {
    console.log('ℹ️ PWA: App ya instalada, no mostrar banner');
    e.preventDefault();
    return;
}
```

**Verificación:**
- ¿El sitio está corriendo en modo standalone? (display-mode: standalone)
- ¿Existe el flag `pwa-installed` en localStorage?
- ¿El navegador detecta que ya fue instalada?

### 2️⃣ **Banner Fue Descartado Recientemente**
Si descartaste el banner hace menos de 7 días, no volverá a aparecer.

```javascript
// Líneas 64-72 en pwa.js
const dismissed = localStorage.getItem('pwa-install-dismissed');
if (dismissed) {
    const daysSince = (Date.now() - dismissTime) / (1000 * 60 * 60 * 24);
    if (daysSince < 7) {
        console.log('ℹ️ PWA: Banner descartado hace menos de 7 días');
        return;
    }
}
```

### 3️⃣ **Service Worker No Se Registró Correctamente**
Si hay errores en la consola del navegador, el SW podría no estar activo.

### 4️⃣ **HTTPS Requerido**
PWA solo funciona en HTTPS (o localhost). Si estás en HTTP en producción, no funcionará.

### 5️⃣ **Criterios de Instalabilidad No Cumplidos**
Chrome requiere:
- ✅ Manifest válido con nombre, iconos, start_url
- ✅ Service Worker registrado
- ✅ Servido sobre HTTPS
- ✅ Usuario ha interactuado con la página

---

## 🔧 Soluciones Paso a Paso

### **Solución 1: Limpiar Estado y Forzar Banner** ⚡

#### Opción A: Desde DevTools (Recomendado)
1. Presionar **F12** para abrir DevTools
2. Ir a **Console**
3. Ejecutar los siguientes comandos:

```javascript
// 1. Limpiar flags de instalación
localStorage.removeItem('pwa-installed');
localStorage.removeItem('pwa-install-dismissed');

// 2. Verificar estado
console.log('PWA installed flag:', localStorage.getItem('pwa-installed'));
console.log('PWA dismissed flag:', localStorage.getItem('pwa-install-dismissed'));

// 3. Recargar la página
location.reload();
```

#### Opción B: Desinstalar la App (Si Está Instalada)
**En Chrome:**
1. Ir a `chrome://apps`
2. Hacer clic derecho en "EIIBD"
3. Seleccionar "Desinstalar"
4. Recargar el sitio web

**En Edge:**
1. Menú → Apps → Manage apps
2. Encontrar "EIIBD"
3. Desinstalar
4. Recargar el sitio web

**En Android/iOS:**
- Desinstalar la app desde el launcher
- Abrir el sitio en el navegador

---

### **Solución 2: Verificar Service Worker** 🔍

1. Abrir **DevTools** (F12)
2. Ir a la pestaña **Application** (Chrome) o **Storage** (Firefox)
3. En el menú izquierdo: **Service Workers**
4. Verificar:
   - ✅ Debe aparecer: `https://tu-dominio.com/service-worker.js`
   - ✅ Estado: `activated and is running`
   - ✅ Scope: `/`

**Si NO aparece el Service Worker:**
- Click en "Unregister" (si hay alguno viejo)
- Recargar la página
- El SW debería registrarse automáticamente

---

### **Solución 3: Forzar Evento `beforeinstallprompt`** 🚀

Chrome solo dispara el evento si:
1. No ha sido instalada antes
2. El usuario ha interactuado con la página
3. Han pasado suficientes "visitas" (engagement)

**Para probar inmediatamente:**
1. Abrir **DevTools** → **Application** → **Manifest**
2. Verificar que el manifest se carga correctamente
3. Click en **"Add to home screen"** (botón en DevTools)

---

### **Solución 4: Modo Debug con Logs** 📝

El script `pwa.js` ya tiene logs extensivos. Abrir DevTools Console y buscar:

```
✅ Logs esperados:
- 🚀 PWA: Inicializando...
- ✅ Service Worker registrado correctamente
- 🎯 PWA: Evento beforeinstallprompt capturado
- 📱 PWA: Mostrando banner de instalación

❌ Logs problemáticos:
- ℹ️ PWA: App ya instalada, no mostrar banner
- ℹ️ PWA: Banner descartado hace menos de 7 días
- ⚠️ Service Workers no soportados
```

---

### **Solución 5: Probar en Modo Incógnito** 🕵️

1. Abrir el sitio en **modo incógnito** (Ctrl+Shift+N)
2. Esto limpia todo el estado local
3. El banner debería aparecer después de interactuar con la página

---

### **Solución 6: Verificar HTTPS** 🔒

PWA **requiere HTTPS** (excepto en localhost).

**Verificar:**
```
✅ https://eiibd.com
❌ http://eiibd.com
```

Si estás en HTTP, necesitas:
1. Configurar certificado SSL
2. Forzar redirección HTTP → HTTPS

---

### **Solución 7: Push Notifications** 🔔

Si la instalación funciona pero las notificaciones no:

#### A. Verificar Permisos
```javascript
// En DevTools Console:
console.log('Notification permission:', Notification.permission);
```

Valores posibles:
- `"granted"` ✅ Permitido
- `"denied"` ❌ Bloqueado
- `"default"` ⚠️ No se ha preguntado

#### B. Resetear Permisos
**Chrome:**
1. Click en el candado 🔒 (barra de direcciones)
2. Permisos del sitio → Notificaciones
3. Cambiar a "Permitir"

#### C. Suscribirse Manualmente
```javascript
// En DevTools Console:
window.subscribeToPush();
```

Esto debería:
1. Pedir permiso de notificaciones
2. Obtener VAPID key del servidor
3. Crear suscripción
4. Guardarla en el servidor

**Verificar logs:**
```
✅ Logs esperados:
- 🔔 [subscribeToPush] Iniciando...
- 🔔 [subscribeToPush] Permiso resultado: granted
- 🔑 [subscribeToPush] Obteniendo VAPID public key...
- ✅ [subscribeToPush] VAPID key obtenida
- 📝 [subscribeToPush] Suscribiendo a push manager...
- ✅ [subscribeToPush] Suscripción creada
- 💾 [subscribeToPush] Enviando al servidor...
- ✅ [subscribeToPush] ¡Proceso completado!
```

#### D. Verificar API Endpoints
```javascript
// Probar que los endpoints responden:
fetch('/api/push/vapid-public-key').then(r => r.text()).then(console.log);
```

Debe retornar una string base64 (la VAPID public key).

---

## 🎯 Script de Diagnóstico Automático

Copiar y pegar en **DevTools Console**:

```javascript
(async function diagnose() {
    console.log('🔍 === DIAGNÓSTICO PWA ===');
    
    // 1. Estado de instalación
    console.log('\n1️⃣ Estado de Instalación:');
    console.log('- localStorage pwa-installed:', localStorage.getItem('pwa-installed'));
    console.log('- localStorage pwa-dismissed:', localStorage.getItem('pwa-install-dismissed'));
    console.log('- Display mode:', window.matchMedia('(display-mode: standalone)').matches ? 'standalone' : 'browser');
    console.log('- iOS standalone:', window.navigator.standalone);
    
    // 2. Soporte de APIs
    console.log('\n2️⃣ Soporte de APIs:');
    console.log('- Service Worker:', 'serviceWorker' in navigator ? '✅' : '❌');
    console.log('- Notifications:', 'Notification' in window ? '✅' : '❌');
    console.log('- Push Manager:', 'PushManager' in window ? '✅' : '❌');
    
    // 3. Permisos
    console.log('\n3️⃣ Permisos:');
    if ('Notification' in window) {
        console.log('- Notification permission:', Notification.permission);
    }
    
    // 4. Service Worker
    console.log('\n4️⃣ Service Worker:');
    if ('serviceWorker' in navigator) {
        const reg = await navigator.serviceWorker.getRegistration();
        if (reg) {
            console.log('- Registrado: ✅');
            console.log('- Scope:', reg.scope);
            console.log('- Estado:', reg.active ? 'Activo ✅' : 'Inactivo ❌');
        } else {
            console.log('- Registrado: ❌');
        }
    }
    
    // 5. Manifest
    console.log('\n5️⃣ Manifest:');
    try {
        const response = await fetch('/manifest.json');
        const manifest = await response.json();
        console.log('- Cargado: ✅');
        console.log('- Nombre:', manifest.name);
        console.log('- Iconos:', manifest.icons.length, 'encontrados');
    } catch (e) {
        console.log('- Error cargando manifest:', e.message);
    }
    
    // 6. Suscripción Push
    console.log('\n6️⃣ Push Subscription:');
    if ('serviceWorker' in navigator) {
        const reg = await navigator.serviceWorker.getRegistration();
        if (reg && reg.pushManager) {
            const subscription = await reg.pushManager.getSubscription();
            console.log('- Suscrito:', subscription ? '✅' : '❌');
            if (subscription) {
                console.log('- Endpoint:', subscription.endpoint.substring(0, 50) + '...');
            }
        }
    }
    
    console.log('\n✅ Diagnóstico completado');
    console.log('\n💡 Para limpiar estado y reintentar:');
    console.log('localStorage.removeItem("pwa-installed");');
    console.log('localStorage.removeItem("pwa-install-dismissed");');
    console.log('location.reload();');
})();
```

---

## 🚀 Quick Fix (Reiniciar Todo)

**Ejecutar en DevTools Console:**

```javascript
// RESET COMPLETO DE PWA
(async function resetPWA() {
    console.log('🔄 Reseteando PWA...');
    
    // 1. Limpiar localStorage
    localStorage.removeItem('pwa-installed');
    localStorage.removeItem('pwa-install-dismissed');
    console.log('✅ LocalStorage limpiado');
    
    // 2. Desregistrar Service Worker
    if ('serviceWorker' in navigator) {
        const registrations = await navigator.serviceWorker.getRegistrations();
        for (let registration of registrations) {
            await registration.unregister();
            console.log('✅ Service Worker desregistrado');
        }
    }
    
    // 3. Limpiar caché
    if ('caches' in window) {
        const names = await caches.keys();
        for (let name of names) {
            await caches.delete(name);
            console.log('✅ Caché eliminado:', name);
        }
    }
    
    console.log('✅ PWA reseteado completamente');
    console.log('🔄 Recargando en 2 segundos...');
    
    setTimeout(() => {
        location.reload();
    }, 2000);
})();
```

---

## 📋 Checklist de Verificación

### Antes de reportar un bug:

- [ ] Verificar en DevTools Console si hay errores
- [ ] Confirmar que el sitio está en HTTPS
- [ ] Verificar que el Service Worker está activo
- [ ] Limpiar localStorage y probar de nuevo
- [ ] Probar en modo incógnito
- [ ] Probar en otro navegador (Chrome, Edge, Firefox)
- [ ] Verificar permisos de notificaciones
- [ ] Ejecutar script de diagnóstico
- [ ] Revisar la pestaña Application → Manifest en DevTools

---

## 🎯 Resultado Esperado

Después de aplicar las soluciones:

1. ✅ Al visitar el sitio (no instalado), después de interactuar, debe aparecer:
   - Banner inferior: "Instalar EIIBD como un APP"
   - Botón "Instalar" funcional
   
2. ✅ Al instalar:
   - La app se abre en modo standalone
   - Después de 2 segundos, solicita permisos de notificaciones
   
3. ✅ Push Notifications:
   - Permiso concedido
   - Suscripción guardada en servidor
   - Notificaciones llegan correctamente

---

## 📞 Si Nada Funciona

1. Compartir logs completos de DevTools Console
2. Verificar versión del navegador
3. Confirmar que `/api/push/vapid-public-key` responde correctamente
4. Revisar configuración del servidor (HTTPS, CORS, etc.)

---

**Última actualización:** 2024
**Autor:** Sistema PWA EIIBD
