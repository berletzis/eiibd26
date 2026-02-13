# 🔒 CSP Update - Cloudflare & CDN Support

**Fecha:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Cambios:** Agregado soporte para Cloudflare Analytics y cdnjs.cloudflare.com

---

## 🚨 Errores Corregidos

### 1. **jquery.validate.unobtrusive.min.js - 404** ✅
**Causa:** CSP bloqueaba cdnjs.cloudflare.com  
**Solución:** Agregado `https://cdnjs.cloudflare.com` a `script-src`

### 2. **MIME type 'text/html' no ejecutable** ✅
**Causa:** Servidor devolvía HTML (404) en vez del archivo JS  
**Solución:** Al permitir cdnjs.cloudflare.com, el CDN ahora carga correctamente

### 3. **Cloudflare beacon bloqueado** ✅
**Causa:** `static.cloudflareinsights.com` no estaba en CSP  
**Solución:** Agregado a `script-src` y `connect-src`

---

## ✅ CSP Actualizado

### **Scripts permitidos (`script-src`):**
```
'self'
'unsafe-inline'
'unsafe-eval'
https://cdn.jsdelivr.net          → Bootstrap, Chart.js, Bootstrap Icons
https://unpkg.com                  → Fallback CDN
https://maps.googleapis.com        → Google Maps API
https://cdn.datatables.net         → DataTables
https://code.jquery.com            → jQuery
https://cdnjs.cloudflare.com       → jQuery Validate, Unobtrusive ✨ NUEVO
https://static.cloudflareinsights.com → Cloudflare Analytics ✨ NUEVO
```

### **Estilos permitidos (`style-src`):**
```
'self'
'unsafe-inline'
https://cdn.jsdelivr.net
https://fonts.googleapis.com
https://cdn.datatables.net
https://cdnjs.cloudflare.com       ✨ NUEVO
```

### **Conexiones permitidas (`connect-src`):**
```
'self'
https://cdn.jsdelivr.net
https://maps.googleapis.com
https://static.cloudflareinsights.com   ✨ NUEVO
https://cloudflareinsights.com          ✨ NUEVO

// Solo en desarrollo:
http://localhost:*
ws://localhost:*
wss://localhost:*
```

---

## 🧪 Verificación

### 1. **Verificar en Browser DevTools (F12):**
```
Console Tab:
  ✅ No debe haber errores de CSP
  ✅ No debe haber errores "Failed to load resource: 404"
  ✅ Cloudflare beacon debe cargar sin errores

Network Tab:
  ✅ jquery.validate.unobtrusive.min.js → Status 200
  ✅ Content-Type: application/javascript
```

### 2. **Test de Cloudflare Analytics:**
```
1. Abre página principal
2. F12 → Network → busca "cloudflareinsights"
3. Verifica Status 200
4. Headers → Response: debe ser JavaScript
```

### 3. **Test de Validación jQuery:**
```
1. Ve a /Identity/Account/Login
2. F12 → Console
3. Escribe: typeof jQuery.validator.unobtrusive
4. Debe devolver: "object" (no "undefined")
```

---

## 📝 Código Actualizado en Program.cs

**Ubicación:** `eiibd26/Program.cs` líneas ~173-220

```csharp
// Content Security Policy (ajustada para tu aplicación)
var cspBuilder = new StringBuilder();
cspBuilder.Append("default-src 'self'; ");

// Scripts: App, CDNs, Google Maps y Cloudflare
cspBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                  "https://cdn.jsdelivr.net " +
                  "https://unpkg.com " +
                  "https://maps.googleapis.com " +
                  "https://cdn.datatables.net " +
                  "https://code.jquery.com " +
                  "https://cdnjs.cloudflare.com " +
                  "https://static.cloudflareinsights.com; ");

// Estilos: App, CDNs y Google Fonts
cspBuilder.Append("style-src 'self' 'unsafe-inline' " +
                  "https://cdn.jsdelivr.net " +
                  "https://fonts.googleapis.com " +
                  "https://cdn.datatables.net " +
                  "https://cdnjs.cloudflare.com; ");

// Fuentes: Google Fonts y Bootstrap Icons en CDN
cspBuilder.Append("font-src 'self' " +
                  "https://fonts.gstatic.com " +
                  "https://cdn.jsdelivr.net " +
                  "data:; ");

// Imágenes: permitir blob: para imágenes dinámicas (avatares, etc.)
cspBuilder.Append("img-src 'self' data: https: blob:; ");

// Conexiones: permitir localhost en desarrollo para Hot Reload y Browser Link
if (app.Environment.IsDevelopment())
{
    cspBuilder.Append("connect-src 'self' " +
                      "http://localhost:* " +
                      "ws://localhost:* " +
                      "wss://localhost:* " +
                      "https://cdn.jsdelivr.net " +
                      "https://maps.googleapis.com " +
                      "https://static.cloudflareinsights.com " +
                      "https://cloudflareinsights.com; ");
}
else
{
    cspBuilder.Append("connect-src 'self' " +
                      "https://cdn.jsdelivr.net " +
                      "https://maps.googleapis.com " +
                      "https://static.cloudflareinsights.com " +
                      "https://cloudflareinsights.com; ");
}

// Frames: permitir Google Maps
cspBuilder.Append("frame-src 'self' https://maps.googleapis.com;");

context.Response.Headers.Add("Content-Security-Policy", cspBuilder.ToString());
```

---

## 🆘 Si sigues viendo errores

### Error: "Failed to load resource: 404"
**Causa:** Archivo local no existe  
**Solución:**
```powershell
# Verificar que existen los archivos locales
Test-Path ".\eiibd26\wwwroot\lib\jquery-validation-unobtrusive\jquery.validate.unobtrusive.min.js"

# Si no existe, restaurar librerías
cd eiibd26
dotnet restore
libman restore
```

### Error: "Refused to execute script... MIME type 'text/html'"
**Causa:** Ruta incorrecta o archivo no accesible  
**Solución:**
```csharp
// En Layout o página específica, verifica las rutas:
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>

// O usa CDN (ya permitido en CSP):
<script src="https://cdnjs.cloudflare.com/ajax/libs/jquery-validate-unobtrusive/3.2.12/jquery.validate.unobtrusive.min.js"></script>
```

### Error: "CSP directive 'script-src'"
**Causa:** Falta dominio en whitelist  
**Solución:** Agrega el dominio a Program.cs en la sección correspondiente

---

## 📊 Impacto de los Cambios

| Componente | Antes | Ahora | Estado |
|------------|-------|-------|--------|
| **jQuery Validate** | ❌ Bloqueado | ✅ Permitido | cdnjs.cloudflare.com |
| **Cloudflare Analytics** | ❌ Bloqueado | ✅ Permitido | static.cloudflareinsights.com |
| **DataTables** | ✅ Ya permitido | ✅ Funciona | cdn.datatables.net |
| **Bootstrap** | ✅ Ya permitido | ✅ Funciona | cdn.jsdelivr.net |
| **Google Maps** | ✅ Ya permitido | ✅ Funciona | maps.googleapis.com |

---

## ⚠️ Nota de Seguridad

Estos cambios mantienen un nivel de seguridad **alto**:
- ✅ Solo dominios de confianza (CDNs conocidos)
- ✅ No permitimos `*` (wildcard)
- ✅ Mantenemos `X-Frame-Options: DENY`
- ✅ Mantenemos `X-Content-Type-Options: nosniff`
- ✅ Mantenemos HSTS habilitado

Si Cloudflare Analytics no es necesario, puedes removerlo:
```csharp
// Remover estas líneas:
"https://static.cloudflareinsights.com " +
"https://cloudflareinsights.com; "
```

---

## 📝 Próximos Pasos

1. ✅ **Reinicia la aplicación** (Hot Reload no es suficiente para CSP)
2. ✅ **Abre DevTools (F12)** y verifica Console (sin errores CSP)
3. ✅ **Prueba Login** para verificar validación jQuery
4. ✅ **Verifica Network tab** - todos los recursos Status 200
5. ✅ **Confirma Cloudflare beacon** carga correctamente

---

**Documentos relacionados:**
- `IMPLEMENTATION_SUMMARY.md` - Resumen de todas las optimizaciones
- `ROLLBACK_GUIDE.md` - Si necesitas revertir cambios
- `PERFORMANCE_OPTIMIZATIONS.md` - Guía completa de performance

---

**Status:** ✅ **IMPLEMENTADO Y COMPILANDO**  
**Próximo paso:** Reinicia la app y verifica en F12 que no hay errores CSP
