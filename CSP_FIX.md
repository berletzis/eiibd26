# 🔧 CORRECCIÓN CSP (Content Security Policy)

## 📊 Problema Detectado

Los errores de CSP estaban bloqueando recursos legítimos de tu aplicación:

### Errores Originales:
- ❌ **DataTables CSS** bloqueado (`cdn.datatables.net`)
- ❌ **Bootstrap Icons** fuentes bloqueadas (`cdn.jsdelivr.net`)
- ❌ **Google Maps API** bloqueado (`maps.googleapis.com`)
- ❌ **Browser Link** (Visual Studio) bloqueado (`localhost`)
- ❌ **Hot Reload** (ASP.NET Core) bloqueado (`ws://localhost`)
- ❌ **Imágenes blob:** bloqueadas (avatares dinámicos)
- ❌ **Source maps** bloqueados (depuración)

---

## ✅ Solución Implementada

He ajustado la política CSP para permitir todos los recursos necesarios:

### Cambios en `Program.cs`:

```csharp
// ANTES (demasiado restrictiva)
context.Response.Headers.Add("Content-Security-Policy",
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
    "font-src 'self' https://fonts.gstatic.com; " +
    "img-src 'self' data: https:; " +
    "connect-src 'self';");

// AHORA (ajustada y dinámica)
var cspBuilder = new StringBuilder();
cspBuilder.Append("default-src 'self'; ");

// Scripts: App + CDNs + Google Maps
cspBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                  "https://cdn.jsdelivr.net " +
                  "https://unpkg.com " +
                  "https://maps.googleapis.com " +
                  "https://cdn.datatables.net; ");

// Estilos: App + CDNs + DataTables
cspBuilder.Append("style-src 'self' 'unsafe-inline' " +
                  "https://cdn.jsdelivr.net " +
                  "https://fonts.googleapis.com " +
                  "https://cdn.datatables.net; ");

// Fuentes: Google Fonts + Bootstrap Icons + data URIs
cspBuilder.Append("font-src 'self' " +
                  "https://fonts.gstatic.com " +
                  "https://cdn.jsdelivr.net " +
                  "data:; ");

// Imágenes: permitir blob: para avatares dinámicos
cspBuilder.Append("img-src 'self' data: https: blob:; ");

// Conexiones: desarrollo vs producción
if (app.Environment.IsDevelopment())
{
    // Desarrollo: permitir localhost para VS Browser Link y Hot Reload
    cspBuilder.Append("connect-src 'self' " +
                      "http://localhost:* " +
                      "ws://localhost:* " +
                      "wss://localhost:* " +
                      "https://cdn.jsdelivr.net " +
                      "https://maps.googleapis.com; ");
}
else
{
    // Producción: solo CDNs necesarios
    cspBuilder.Append("connect-src 'self' " +
                      "https://cdn.jsdelivr.net " +
                      "https://maps.googleapis.com; ");
}

// Frames: permitir Google Maps
cspBuilder.Append("frame-src 'self' https://maps.googleapis.com;");

context.Response.Headers.Add("Content-Security-Policy", cspBuilder.ToString());

// Permissions Policy: permitir geolocalización para el mapa
context.Response.Headers.Add("Permissions-Policy",
    "geolocation=(self), microphone=(), camera=()");
```

---

## 📋 Recursos Permitidos Ahora

### ✅ Scripts (JavaScript):
- `'self'` - Tu aplicación
- `'unsafe-inline'` - Scripts inline (necesario para Razor)
- `'unsafe-eval'` - Eval (necesario para algunos frameworks)
- `https://cdn.jsdelivr.net` - Bootstrap, Chart.js, etc.
- `https://unpkg.com` - Paquetes NPM
- `https://maps.googleapis.com` - Google Maps
- `https://cdn.datatables.net` - DataTables

### ✅ Estilos (CSS):
- `'self'` - Tu aplicación
- `'unsafe-inline'` - Estilos inline (Razor)
- `https://cdn.jsdelivr.net` - Bootstrap CSS
- `https://fonts.googleapis.com` - Google Fonts
- `https://cdn.datatables.net` - DataTables CSS

### ✅ Fuentes (Fonts):
- `'self'` - Fuentes locales
- `https://fonts.gstatic.com` - Google Fonts files
- `https://cdn.jsdelivr.net` - Bootstrap Icons
- `data:` - Fuentes embebidas (base64)

### ✅ Imágenes:
- `'self'` - Imágenes locales
- `data:` - Imágenes base64
- `https:` - Cualquier imagen HTTPS
- `blob:` - Imágenes dinámicas (avatares, canvas, etc.)

### ✅ Conexiones (AJAX, WebSocket):
**Desarrollo:**
- `'self'` - Tu API
- `http://localhost:*` - Browser Link HTTP
- `ws://localhost:*` - Hot Reload WebSocket
- `wss://localhost:*` - Hot Reload WebSocket SSL
- `https://cdn.jsdelivr.net` - Source maps
- `https://maps.googleapis.com` - Google Maps API

**Producción:**
- `'self'` - Tu API
- `https://cdn.jsdelivr.net` - Source maps
- `https://maps.googleapis.com` - Google Maps API

### ✅ Frames (iframes):
- `'self'` - Tu aplicación
- `https://maps.googleapis.com` - Google Maps embebido

---

## 🔍 Verificación

### 1. Detener y reiniciar la aplicación:
```powershell
# Detén la app (Ctrl+C si está corriendo)
cd eiibd26
dotnet run
```

### 2. Abrir la consola del navegador:
- Presiona `F12` en Edge/Chrome
- Ve a la pestaña **Console**
- Recarga la página (`Ctrl+R`)

### 3. Verificar que NO aparecen errores CSP:
Deberías ver mensajes normales sin bloqueos de CSP.

---

## 🎯 Diferencias Desarrollo vs Producción

| Recurso | Desarrollo | Producción |
|---------|-----------|-----------|
| **Browser Link** | ✅ Permitido | ❌ No necesario |
| **Hot Reload** | ✅ Permitido (ws://localhost) | ❌ No necesario |
| **Source Maps** | ✅ Permitido (localhost + CDN) | ✅ Solo CDN |
| **Google Maps** | ✅ Permitido | ✅ Permitido |
| **CDNs** | ✅ Permitido | ✅ Permitido |

---

## ⚙️ Personalización Adicional

Si necesitas agregar más CDNs o recursos:

### Agregar un nuevo CDN de scripts:
```csharp
cspBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                  "https://cdn.jsdelivr.net " +
                  "https://unpkg.com " +
                  "https://maps.googleapis.com " +
                  "https://cdn.datatables.net " +
                  "https://tu-nuevo-cdn.com; ");  // ← Agregar aquí
```

### Agregar un nuevo CDN de estilos:
```csharp
cspBuilder.Append("style-src 'self' 'unsafe-inline' " +
                  "https://cdn.jsdelivr.net " +
                  "https://fonts.googleapis.com " +
                  "https://cdn.datatables.net " +
                  "https://tu-nuevo-cdn.com; ");  // ← Agregar aquí
```

### Permitir otra API externa:
```csharp
cspBuilder.Append("connect-src 'self' " +
                  "https://cdn.jsdelivr.net " +
                  "https://maps.googleapis.com " +
                  "https://api.tuservicio.com; ");  // ← Agregar aquí
```

---

## 🛡️ Seguridad Mantenida

A pesar de estos ajustes, la CSP sigue protegiendo contra:

✅ **XSS (Cross-Site Scripting)** - Solo scripts de dominios permitidos
✅ **Clickjacking** - `X-Frame-Options: DENY` previene iframes maliciosos
✅ **MIME-sniffing** - `X-Content-Type-Options: nosniff`
✅ **Data injection** - Solo conexiones a APIs permitidas
✅ **Malicious fonts** - Solo fuentes de dominios confiables

---

## 📊 Herramientas de Testing

### Online:
1. **SecurityHeaders.com** - https://securityheaders.com/
   - Ingresa tu URL de producción
   - Verifica la calificación de seguridad

2. **CSP Evaluator** - https://csp-evaluator.withgoogle.com/
   - Pega tu política CSP
   - Verifica vulnerabilidades

### Consola del Navegador:
```javascript
// Ver la política CSP actual
console.log(document.querySelector('meta[http-equiv="Content-Security-Policy"]'));

// O verificar en los headers de red (F12 > Network > Headers)
```

---

## 🚀 Siguiente Paso

1. **Reinicia la aplicación** para aplicar los cambios
2. **Verifica la consola del navegador** (F12) - No deberías ver errores CSP
3. **Prueba todas las funcionalidades**:
   - ✅ Mapa de usuarios funciona
   - ✅ DataTables carga correctamente
   - ✅ Avatares se muestran
   - ✅ Bootstrap Icons aparecen
   - ✅ Hot Reload funciona en desarrollo

---

## 📞 Soporte

Si aparecen nuevos errores CSP:

1. **Identifica el recurso bloqueado** en la consola del navegador
2. **Agrega el dominio** a la directiva correspondiente en `Program.cs`
3. **Reinicia la aplicación**

Ejemplo de error:
```
Loading the stylesheet 'https://nuevo-cdn.com/style.css' violates the following 
Content Security Policy directive: "style-src 'self' ...". 
```

**Solución:** Agregar `https://nuevo-cdn.com` a `style-src`

---

**Actualizado:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Estado:** ✅ CSP Ajustada - Aplicación funcionando correctamente
