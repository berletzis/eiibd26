# 🔐 ACCIÓN INMEDIATA REQUERIDA

## ⚠️ CREDENCIALES EXPUESTAS EN GITHUB

Tu archivo `appsettings.json` con credenciales reales fue commiteado públicamente.

---

## 🚀 INICIO RÁPIDO (3 pasos)

### PASO 1: Revocar Credenciales (5 min) 🔴 CRÍTICO

- **SQL Server:** Cambiar password de 'sa'
- **SendGrid:** Regenerar API Key → https://app.sendgrid.com/settings/api_keys
- **Twilio:** Regenerar Auth Token → https://console.twilio.com/

### PASO 2: Configurar User Secrets (2 min)

```powershell
# Método automático (recomendado)
.\setup-secrets.ps1

# O método manual
cd eiibd26
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=NUEVA_PASSWORD;..."
dotnet user-secrets set "SendGrid:ApiKey" "NUEVA_API_KEY"
dotnet user-secrets set "Twilio:AuthToken" "NUEVO_TOKEN"
```

### PASO 3: Limpiar Git (5 min)

```powershell
# Remover del staging
git rm --cached eiibd26/appsettings.json

# Usar plantilla (edítala con tus nuevas credenciales localmente)
cp eiibd26/appsettings.json.template eiibd26/appsettings.json

# Commit
git add .gitignore eiibd26/appsettings.json.template
git commit -m "security: Remove credentials from repo"
git push

# Limpiar historial (IMPORTANTE - lee SECURITY_GUIDE.md primero)
# Descarga BFG: https://rtyley.github.io/bfg-repo-cleaner/
java -jar bfg.jar --delete-files appsettings.json
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git push origin --force --all
```

---

## ✅ MEJORAS APLICADAS AUTOMÁTICAMENTE

- ✅ **Política de contraseñas fortalecida** (8+ chars, símbolos)
- ✅ **Lockout habilitado** (bloqueo tras 5 intentos fallidos)
- ✅ **Cookies seguras** (HTTPS-only, SameSite=Strict)
- ✅ **Headers de seguridad** (X-Frame-Options, CSP, etc.)
- ✅ **HSTS mejorado** (365 días, preload)
- ✅ **`.gitignore` actualizado** (bloquea credenciales)

---

## 📚 DOCUMENTACIÓN

- **`SECURITY_CHANGES.md`** - Resumen completo de cambios
- **`SECURITY_GUIDE.md`** - Guía detallada paso a paso
- **`setup-secrets.ps1`** - Script automatizado de configuración
- **`security-tools.ps1`** - Herramientas interactivas

---

## 🆘 AYUDA RÁPIDA

```powershell
# Ver secretos configurados
dotnet user-secrets list --project eiibd26

# Ejecutar app
cd eiibd26
dotnet run

# Herramientas interactivas
.\security-tools.ps1
```

---

## 🎯 VERIFICACIÓN

Tras completar los pasos:

1. ✅ Credenciales revocadas y regeneradas
2. ✅ User Secrets configurado
3. ✅ `appsettings.json` removido del repo
4. ✅ Historial de Git limpiado
5. ✅ App funciona con `dotnet run`

---

**Estado:** ⚠️ REQUIERE TU ACCIÓN  
**Prioridad:** 🔴 CRÍTICA  
**Tiempo estimado:** 15-20 minutos
