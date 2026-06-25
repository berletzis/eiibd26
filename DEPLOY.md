# DEPLOY.md — Guía de despliegue seguro · EIIBD

> App on-premise en IIS. Publicación desde **Visual Studio** (perfil de Publish, Folder/Web Deploy).
> Destino en producción: `C:\inetpub\wwwroot\eiibdcom`
>
> Este documento existe por un incidente real: un publish con la casilla
> **"Remove additional files at destination"** marcada borró `wwwroot/uploads/`
> (22 avatares de usuarios) y `DataProtectionKeys/`, rompiendo antiforgery y login.
> Seguir este checklist evita que se repita.

---

## ⛔ REGLA #1 (CRÍTICA) — Nunca "Remove additional files at destination"

En el perfil de Publish de Visual Studio:
**Settings → File Publish Options → `Remove additional files at destination` debe quedar DESMARCADO.**

**Por qué:** esa casilla borra del servidor todo lo que no esté en el output del
build. Pero hay carpetas que **viven solo en producción** y nunca están en el repo
ni en el build: los uploads de usuarios y las llaves de Data Protection. Marcarla
las elimina → se pierden avatares y se invalidan sesiones/antiforgery (login roto).

---

## 📁 Carpetas protegidas — viven SOLO en producción, JAMÁS borrar

Estas rutas son relativas al sitio publicado (`C:\inetpub\wwwroot\eiibdcom\`):

| Carpeta | Contiene | Ruta física en producción |
|---|---|---|
| `wwwroot/uploads/avatars/` | Avatares de pacientes (`{userId}/avatar-*.png`) | `C:\inetpub\wwwroot\eiibdcom\wwwroot\uploads\avatars\` |
| `wwwroot/uploads/medicos/` | Fotos de médicos (`medico-{guid}.jpg`) | `C:\inetpub\wwwroot\eiibdcom\wwwroot\uploads\medicos\` |
| `wwwroot/uploads/banners/` | Banners de inicio (`*.jpg`) | `C:\inetpub\wwwroot\eiibdcom\wwwroot\uploads\banners\` |
| `DataProtectionKeys/` | Llaves de Data Protection (antiforgery, tokens de reseteo, sesión) | `C:\inetpub\wwwroot\eiibdcom\DataProtectionKeys\` |

> Ninguna de estas carpetas está en el repositorio. Si un deploy las borra, los
> datos NO se recuperan salvo desde un backup (ver § Backups).

---

## ✅ Checklist PRE-deploy

1. [ ] Compilar en **Release** (Configuration = Release en Visual Studio).
2. [ ] **Verificar que "Remove additional files at destination" está DESMARCADO** (Regla #1).
3. [ ] Confirmar que `appsettings.Production.json` está presente y con los valores correctos.
4. [ ] Publicar al destino `C:\inetpub\wwwroot\eiibdcom`.
5. [ ] Reciclar el **App Pool** del sitio en IIS (o `iisreset` si aplica).
6. [ ] Verificar que el sitio responde (home carga, sin error 500).

---

## ✅ Checklist POST-deploy (verificación)

1. [ ] **Webhook SendGrid:** en SendGrid → Mail Settings → Event Webhook → **Test Integration**.
       Confirmar en el log de la app una línea `[Webhook] {N} guardados`. (Si da **405**,
       el binario desplegado es viejo — re-publicar este código.)
2. [ ] **Login funciona:** iniciar sesión con una cuenta de prueba (valida que las
       DataProtectionKeys y el antiforgery estén operativos).
3. [ ] **Avatares cargan:** abrir una página con avatares (top-menu, directorio,
       preguntas) y confirmar que las imágenes de `wwwroot/uploads/avatars/` se ven.

---

## 💾 Backups (on-premise, sin panel de hosting)

No hay panel de hosting que respalde automáticamente. El backup es **manual o por
tarea programada**. Respaldar periódicamente (idealmente antes de cada deploy):

- `C:\inetpub\wwwroot\eiibdcom\wwwroot\uploads\`  (todos los avatares, médicos, banners)
- `C:\inetpub\wwwroot\eiibdcom\DataProtectionKeys\`  (llaves de Data Protection)

Sugerencia: una tarea programada de Windows que copie ambas carpetas a otra unidad
o ubicación de red con fecha (`uploads-YYYYMMDD`, `keys-YYYYMMDD`).

> Nota: si las DataProtectionKeys se pierden, se regeneran solas al arrancar, pero
> se invalidan una vez las sesiones y tokens vigentes (los usuarios re-loguean).
> Los uploads NO se regeneran: su pérdida es permanente sin backup.
