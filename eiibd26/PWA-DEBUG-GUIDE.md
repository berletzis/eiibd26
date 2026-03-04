# 🔍 PWA DEBUG CHECKLIST

## ✅ PROBLEMAS RESUELTOS:

### 1. Error SQL "Invalid column name 'CreatorId'"
- ✅ **SOLUCIONADO**: Agregado `[ForeignKey]` a los modelos
- Los modelos ahora especifican explícitamente la FK

### 2. Banner PWA no aparece
- ✅ **MEJORADO**: Script con logs detallados
- ✅ **AGREGADO**: Verificación de estado PWA
- ✅ **AGREGADO**: Manejo de errores mejorado

---

## 🔍 CÓMO VERIFICAR QUE FUNCIONA:

### A. Verificar Service Worker:
1. Abre DevTools (F12)
2. Ve a la pestaña **Console**
3. Deberías ver:
   ```
   🚀 PWA: Inicializando...
   ✅ Service Worker registrado correctamente
   📊 PWA Status Check:
   - Service Worker support: true
   - Notification support: true
   - Push support: true
   - Notification permission: default
   ```

### B. Verificar manifest.json:
1. En DevTools → **Application**
2. En el sidebar → **Manifest**
3. Deberías ver:
   - Name: EIIBD - Comunidad EII
   - Short name: EIIBD
   - Start URL: /
   - Theme color: #764ba2
   - Icons (8 tamaños)

### C. Verificar PWA installable:
1. En DevTools → **Application**
2. Buscar el mensaje: **"App can be installed"**
3. Si NO aparece, verificar:
   - ❌ manifest.json no se carga
   - ❌ Service Worker no registrado
   - ❌ No estás en HTTPS (necesario excepto localhost)
   - ❌ Ya tienes la app instalada

### D. Forzar mostrar banner:
```javascript
// En la consola del navegador:
localStorage.removeItem('pwa-install-dismissed');
location.reload();
```

---

## 🔧 TROUBLESHOOTING:

### Problema: "beforeinstallprompt event no se dispara"

**Causas comunes:**
1. **Ya instalaste la PWA**: Desinstala primero
   - Chrome: Settings → Apps → EIIBD → Uninstall
   - Edge: Same
   - Mobile: Mantén presionado el ícono → Uninstall

2. **No cumple criterios PWA**:
   - ✅ Manifest.json válido
   - ✅ Service Worker registrado
   - ✅ HTTPS (o localhost)
   - ✅ Start URL es válida
   - ✅ Iconos 192x192 y 512x512 presentes

3. **Chrome/Edge Desktop**: A veces tarda en detectar
   - Solución: Reinicia el navegador
   - Solución: Limpia caché (Ctrl+Shift+Del)

### Problema: "Service Worker no se registra"

**Verificar:**
```javascript
// En Console:
navigator.serviceWorker.getRegistrations().then(regs => {
  console.log('Registrations:', regs.length);
  regs.forEach(r => console.log('SW:', r));
});
```

**Desregistrar todos:**
```javascript
navigator.serviceWorker.getRegistrations().then(regs => {
  regs.forEach(r => r.unregister());
  console.log('All SW unregistered');
  location.reload();
});
```

### Problema: "Iconos no cargan"

**Verificar rutas:**
- Ve a: https://localhost:7002/img/icons/icon-192x192.png
- Debe cargar la imagen
- Si 404 → Agrega los iconos (ver README.md en /img/icons/)

### Problema: "manifest.json no se encuentra"

**Verificar:**
- URL: https://localhost:7002/manifest.json
- Debe mostrar el JSON
- Si 404 → Verifica que está en `wwwroot/manifest.json`

---

## 🎯 TESTING PASO A PASO:

### 1. Reiniciar aplicación
```bash
# Detén la app (Ctrl+C)
dotnet clean
dotnet build
dotnet run
```

### 2. Limpiar caché del navegador
- Ctrl+Shift+Del
- Selecciona:
  - ✅ Cookies
  - ✅ Cached images
  - ✅ Site data
- Time: "Last hour"
- Clear

### 3. Abrir DevTools ANTES de cargar la página
- F12 → Console tab
- Luego navegar a https://localhost:7002

### 4. Verificar logs en Console:
```
✅ Debería ver:
🚀 PWA: Inicializando...
✅ Service Worker registrado correctamente
📊 PWA Status Check: ...

❌ Si ves errores:
❌ Error registrando SW: ... → Verifica service-worker.js
❌ Failed to load manifest → Verifica manifest.json
```

### 5. Esperar 2-3 segundos
- El evento `beforeinstallprompt` tarda un poco
- Deberías ver: `🎯 PWA: Evento beforeinstallprompt capturado`

### 6. Banner debe aparecer
- Si no aparece, ve a Console
- Verifica si hay logs de PWA

---

## 🚨 ERRORES COMUNES Y SOLUCIONES:

| Error | Causa | Solución |
|-------|-------|----------|
| `beforeinstallprompt no se dispara` | PWA ya instalada | Desinstalar app |
| `manifest.json 404` | Ruta incorrecta | Verificar en wwwroot |
| `Service Worker failed` | Error en SW | Verificar console |
| `Icons 404` | Iconos no existen | Agregar iconos PNG |
| `Invalid manifest` | JSON mal formado | Validar JSON |

---

## 📱 TESTING EN MÓVIL:

### Android Chrome:
1. Abre Chrome en Android
2. Ve a https://tu-sitio.com (necesita HTTPS real)
3. Espera banner "Add to Home Screen"
4. Si no aparece: Menu (⋮) → "Install app"

### iOS Safari:
1. Safari → Share button
2. "Add to Home Screen"
3. Push notifications requieren iOS 16.4+

---

## ✅ CHECKLIST FINAL:

- [ ] Reinicié la aplicación
- [ ] Limpié caché del navegador
- [ ] DevTools abierto en Console
- [ ] Veo logs "🚀 PWA: Inicializando..."
- [ ] Service Worker registrado (✅ en console)
- [ ] manifest.json accesible (/manifest.json)
- [ ] Iconos agregados en /img/icons/
- [ ] Esperé 3-5 segundos
- [ ] Banner aparece (o evento beforeinstallprompt en console)

---

Si después de todo esto NO funciona:
1. Copia TODOS los logs de Console
2. Toma screenshot de DevTools → Application → Manifest
3. Verifica URL en Address bar (debe ser localhost:7002)
