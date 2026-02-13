# 🚀 PLAN DE OPTIMIZACIÓN PAGESPEED INSIGHTS

## 📊 Problemas Identificados:
- **First Contentful Paint (FCP)**: 2.6s → Objetivo: < 1.8s
- **Largest Contentful Paint (LCP)**: 7.1s → **CRÍTICO** → Objetivo: < 2.5s  
- **Speed Index**: 4.3s → Objetivo: < 3.4s
- **Cumulative Layout Shift (CLS)**: 0.11 → Aceptable (objetivo < 0.1)
- **Total Blocking Time (TBT)**: 30ms → Excelente ✅

---

## ✅ OPTIMIZACIONES YA APLICADAS EN TU PROYECTO:

### 1. ✅ Response Compression (Gzip/Brotli)
Ya configurado en `Program.cs` líneas 115-133

### 2. ✅ Response Caching
Ya configurado en `Program.cs` líneas 111-113, 247-248

### 3. ✅ Static Files Caching (1 año)
Ya configurado en `Program.cs` líneas 250-260

### 4. ✅ Optimización de CSS en `_Layout.cshtml`
- DNS Prefetch y Preconnect para CDNs
- Critical CSS inline
- Preload para recursos críticos
- Carga diferida de CSS no crítico
- JavaScript con defer

---

## 🔧 OPTIMIZACIONES ADICIONALES NECESARIAS:

### **1. OPTIMIZAR IMÁGENES (Impacto ALTO en LCP)**

#### A. Usar formatos modernos (WebP)
```bash
# Instala herramientas de conversión
dotnet add package SixLabors.ImageSharp

# O usa herramientas online:
# - https://squoosh.app/
# - https://tinypng.com/
```

#### B. Implementar lazy loading en imágenes
En tus vistas Razor, cambia:
```html
<!-- ANTES -->
<img src="/uploads/contenidos/imagen.jpg" alt="Descripción">

<!-- DESPUÉS -->
<img src="/uploads/contenidos/imagen.jpg" 
     alt="Descripción" 
     loading="lazy" 
     width="800" 
     height="600">
```

#### C. Usar srcset para imágenes responsivas
```html
<img srcset="/uploads/contenidos/imagen-320w.webp 320w,
             /uploads/contenidos/imagen-640w.webp 640w,
             /uploads/contenidos/imagen-1024w.webp 1024w"
     sizes="(max-width: 640px) 320px,
            (max-width: 1024px) 640px,
            1024px"
     src="/uploads/contenidos/imagen.jpg"
     alt="Descripción"
     loading="lazy"
     width="1024"
     height="768">
```

#### D. Preload imagen LCP (la imagen más grande above-the-fold)
En el `<head>` de la página donde está la imagen principal:
```html
<link rel="preload" 
      as="image" 
      href="/uploads/contenidos/hero-image.webp" 
      imagesrcset="/uploads/contenidos/hero-320w.webp 320w,
                   /uploads/contenidos/hero-640w.webp 640w,
                   /uploads/contenidos/hero-1024w.webp 1024w"
      imagesizes="100vw">
```

---

### **2. MINIFICAR Y BUNDLAR CSS/JS**

#### Opción A: Usar WebOptimizer (Recomendado)
```bash
dotnet add package LigerShark.WebOptimizer.Core
```

En `Program.cs`, agrega después de línea 81:
```csharp
// Minificación y bundling
builder.Services.AddWebOptimizer(pipeline =>
{
    // Bundle y minifica CSS
    pipeline.AddCssBundle("/css/bundle.min.css", 
        "css/site.css",
        "css/miSalud.css",
        "css/account.css",
        "css/site-responsive.css");
    
    // Minifica JS
    pipeline.MinifyJsFiles("js/**/*.js");
});
```

Y antes de `app.UseStaticFiles()` (línea 251):
```csharp
app.UseWebOptimizer();
```

Luego actualiza `_Layout.cshtml`:
```html
<!-- Reemplaza múltiples links CSS por -->
<link rel="stylesheet" href="/css/bundle.min.css" asp-append-version="true">
```

---

### **3. REDUCIR CSS NO USADO**

#### Usar PurgeCSS
Instala como herramienta:
```bash
npm install -g purgecss
```

Ejecuta para limpiar Bootstrap:
```bash
purgecss --css wwwroot/css/bootstrap.min.css --content Pages/**/*.cshtml --output wwwroot/css/
```

O usa esta opción inline en build:
```bash
dotnet add package BuildBundlerMinifier
```

---

### **4. OPTIMIZAR FUENTES**

Si usas Google Fonts, agrega en el `<head>`:
```html
<!-- Preconnect a Google Fonts -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>

<!-- Fuente con display=swap para evitar FOIT -->
<link href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap" rel="stylesheet">
```

O mejor aún, usa **font-display: swap** en CSS:
```css
@font-face {
    font-family: 'MiFuente';
    src: url('/fonts/mifuente.woff2') format('woff2');
    font-display: swap;
}
```

---

### **5. IMPLEMENTAR SERVICE WORKER PARA CACHÉ**

Crea `wwwroot/sw.js`:
```javascript
const CACHE_NAME = 'eiibd-v1';
const urlsToCache = [
  '/',
  '/css/bundle.min.css',
  '/js/site.js',
  '/uploads/logo.webp'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => response || fetch(event.request))
  );
});
```

Y registra en `_Layout.cshtml` antes de `</body>`:
```html
<script>
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js');
  });
}
</script>
```

---

### **6. REDUCIR JAVASCRIPT NO USADO**

#### A. Cargar DataTables solo donde se usa
En lugar de cargarlo globalmente, cárgalo solo en páginas que lo necesiten:

En `Contenidos.cshtml` (ya lo tienes):
```html
@section Scripts {
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>
    <script src="https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js"></script>
}
```

#### B. Usar code splitting
Para scripts grandes, divídelos en chunks más pequeños.

---

### **7. IMPLEMENTAR HTTP/2 PUSH**

En `Program.cs`, después de `app.UseHttpsRedirection()`:
```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Headers.Add("Link", "</css/bundle.min.css>; rel=preload; as=style");
        context.Response.Headers.Add("Link", "</js/site.min.js>; rel=preload; as=script");
    }
    await next();
});
```

---

### **8. OPTIMIZAR CONTENIDO ABOVE-THE-FOLD**

#### Inline Critical CSS en el `<head>`
Ya aplicado en `_Layout.cshtml` pero amplía con:
```html
<style>
    /* Critical CSS - Above the fold */
    body{margin:0;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif}
    .hero{height:400px;background:#f8f9fa}
    .nav{display:flex;padding:1rem}
    /* Agrega aquí todos los estilos visibles sin scroll */
</style>
```

Herramienta para extraer Critical CSS: https://www.sitelocity.com/critical-path-css-generator

---

### **9. LAZY LOAD PARA IFRAMES Y EMBEDS**

Si tienes YouTube, Maps, etc.:
```html
<iframe src="https://www.youtube.com/embed/..." 
        loading="lazy" 
        width="560" 
        height="315"></iframe>
```

---

### **10. REDUCIR IMPACTO DE THIRD-PARTY SCRIPTS**

Para Google Analytics, Tag Manager, etc., usa:
```html
<script async src="https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'GA_MEASUREMENT_ID');
</script>
```

---

## 🎯 PRIORIDADES POR IMPACTO:

### **CRÍTICO (Hacer primero):**
1. ✅ **Optimizar imágenes** (WebP + lazy loading + srcset)
2. ✅ **Preload imagen LCP**
3. ✅ **Minificar CSS/JS** (WebOptimizer)

### **ALTO (Segunda fase):**
4. ✅ **Reducir CSS no usado** (PurgeCSS)
5. ✅ **Optimizar fuentes** (font-display: swap)
6. ✅ **Lazy load iframes**

### **MEDIO (Mejoras adicionales):**
7. Service Worker
8. HTTP/2 Push
9. Code splitting

---

## 📈 RESULTADOS ESPERADOS DESPUÉS DE OPTIMIZACIONES:

- **FCP**: 2.6s → **< 1.5s** (mejora 40%)
- **LCP**: 7.1s → **< 2.5s** (mejora 65%)
- **Speed Index**: 4.3s → **< 3.0s** (mejora 30%)
- **CLS**: 0.11 → **< 0.05** (mejora 55%)

---

## 🔍 HERRAMIENTAS PARA MONITOREAR:

1. **PageSpeed Insights**: https://pagespeed.web.dev/
2. **WebPageTest**: https://www.webpagetest.org/
3. **GTmetrix**: https://gtmetrix.com/
4. **Chrome DevTools Lighthouse**: F12 → Lighthouse tab

---

## 📝 CHECKLIST DE IMPLEMENTACIÓN:

- [x] Response Compression (Gzip/Brotli)
- [x] Response Caching
- [x] Static Files Caching
- [x] DNS Prefetch y Preconnect
- [x] Critical CSS inline
- [x] Preload recursos críticos
- [x] Defer JavaScript
- [ ] Convertir imágenes a WebP
- [ ] Implementar lazy loading de imágenes
- [ ] Usar srcset para imágenes responsivas
- [ ] Preload imagen LCP
- [ ] Instalar y configurar WebOptimizer
- [ ] Reducir CSS no usado con PurgeCSS
- [ ] Optimizar fuentes con font-display: swap
- [ ] Lazy load iframes
- [ ] Implementar Service Worker
- [ ] HTTP/2 Push para recursos críticos

---

## 💡 COMANDOS ÚTILES:

### Medir rendimiento local:
```bash
dotnet run --configuration Release
# Luego abrir Chrome DevTools → Lighthouse
```

### Compilar en Release mode:
```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### Verificar tamaño de archivos:
```bash
# PowerShell
Get-ChildItem wwwroot -Recurse | Where-Object {-not $_.PSIsContainer} | Measure-Object -Property Length -Sum
```

---

**¡Éxito con las optimizaciones! 🚀**
